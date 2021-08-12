﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Clusterclient.Core.Ordering.Weighed.Relative.Interfaces;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Relative
{
    /// <summary>
    /// <para>Represents a weight modifier that periodically calculates the weight of a replica, which characterizes its quality relative to others in the cluster.</para>
    /// <para>Replica weight is the probability that a replica will respond in a time less than or equal to the average response time across the cluster.</para>
    /// <para>For replicas whose responses are <see cref="ResponseVerdict.Reject"/> or <see cref="ResponseVerdict.DontKnow"/> a <see cref="RelativeWeightSettings.PenaltyMultiplier"/> will be applied.</para>
    /// </summary>
    [PublicAPI]
    public class RelativeWeightModifier : IReplicaWeightModifier
    {
        private readonly ILog log;
        private readonly IGlobalStorageProvider globalStorageProvider;
        private readonly RelativeWeightSettings settings;
        private readonly double minWeight;
        private readonly double maxWeight;
        private readonly string storageKey;

        public RelativeWeightModifier(
            RelativeWeightSettings settings,
            string service,
            string environment,
            double minWeight = ClusterClientDefaults.MinimumReplicaWeight,
            double maxWeight = ClusterClientDefaults.MaximumReplicaWeight,
            IGlobalStorageProvider globalStorageProvider = null,
            ILog log = null)
        {
            this.settings = settings;
            this.minWeight = minWeight;
            this.maxWeight = maxWeight;
            this.log = (log ?? new SilentLog()).ForContext<RelativeWeightModifier>();
            this.globalStorageProvider = globalStorageProvider ?? new PerProcessGlobalStorageProvider();

            storageKey = CreateStorageKey(service, environment);
        }

        /// <inheritdoc />
        public void Modify(Uri replica, IList<Uri> allReplicas, IReplicaStorageProvider storageProvider, Request request, RequestParameters parameters, ref double weight)
        {
            var clusterState = globalStorageProvider.ObtainGlobalValue(storageKey, CreateClusterState);
            
            ModifyClusterWeightsIfNeed(clusterState);

            weight = ModifyAndApplyLimits(weight, clusterState.Weights.Get(replica, settings.WeightsTTL));
        }

        /// <inheritdoc />
        public void Learn(ReplicaResult result, IReplicaStorageProvider storageProvider) =>
            globalStorageProvider.ObtainGlobalValue(storageKey, CreateClusterState).CurrentStatistic.Report(result);

        private void ModifyClusterWeightsIfNeed(ClusterState clusterState)
        {
            var needUpdateWeights = NeedUpdateWeights(clusterState.TimeProvider.GetCurrentTime(), clusterState.LastUpdateTimestamp);
            var updatingFlagChanged = false;
            try
            {
                if (!needUpdateWeights || !(updatingFlagChanged = clusterState.IsUpdatingNow.TrySetTrue()))
                    return;

                var currentTime = clusterState.TimeProvider.GetCurrentTime();
                clusterState.LastUpdateTimestamp = currentTime;

                var aggregatedClusterStatistic = clusterState
                    .SwapToNewRawStatistic()
                    .GetPenalizedAndSmoothedStatistic(currentTime, clusterState.StatisticHistory.Get(),
                        settings.PenaltyMultiplier, 
                        settings.StatisticSmoothingConstant);
                
                ModifyWeights(aggregatedClusterStatistic, 
                    clusterState.RelativeWeightCalculator,
                    clusterState.WeightsNormalizer,
                    clusterState.Weights);

                clusterState.StatisticHistory.Update(aggregatedClusterStatistic, settings.StatisticTTL);
            }
            finally
            {
                if (updatingFlagChanged)
                    clusterState.IsUpdatingNow.Value = false;
            }
        }

        private void ModifyWeights(
            AggregatedClusterStatistic aggregatedClusterStatistic,
            IRelativeWeightCalculator relativeWeightCalculator,
            IWeightsNormalizer weightsNormalizer,
            IWeights weights)
        {
            var newWeights = new Dictionary<Uri, Weight>(aggregatedClusterStatistic.Replicas.Count);
            var statisticCollectedTimestamp = aggregatedClusterStatistic.Cluster.Timestamp;
            var relativeMaxWeight = 0d;
            foreach (var (replica, replicaStatistic) in aggregatedClusterStatistic.Replicas)
            {
                var previousWeight = weights.Get(replica, settings.WeightsTTL) ??
                                     new Weight(settings.InitialWeight, statisticCollectedTimestamp - settings.WeightUpdatePeriod);
                
                var newReplicaWeight = relativeWeightCalculator
                    .Calculate(aggregatedClusterStatistic.Cluster, replicaStatistic, previousWeight, settings);
                
                newWeights.Add(replica, newReplicaWeight);

                if (relativeMaxWeight < newReplicaWeight.Value)
                    relativeMaxWeight = newReplicaWeight.Value;
            }
            
            weightsNormalizer.Normalize(newWeights, relativeMaxWeight);

            LogWeights(weights, newWeights);

            weights.Update(newWeights, settings);
        }

        private double ModifyAndApplyLimits(double externalWeight, Weight? relativeWeight)
        {
            // ExternalWeight - [minWeight; maxWeight]
            // RelativeWeight - [0; 1]
            // ModifiedWeight - [0; maxWeight]
            var relative = relativeWeight?.Value ?? settings.InitialWeight;
            var modified = externalWeight * relative;
            
            if (double.IsPositiveInfinity(maxWeight))
                return minWeight + modified;

            return minWeight + modified / maxWeight * (maxWeight - minWeight);
        }

        private bool NeedUpdateWeights(in DateTime currentTimestamp, in DateTime previousTimestamp) =>
            currentTimestamp - previousTimestamp > settings.WeightUpdatePeriod;

        private string CreateStorageKey(string service, string environment) =>
            $"{nameof(RelativeWeightModifier)}_{environment}_{service}";

        private ClusterState CreateClusterState() =>
            new ClusterState();
        
        private void LogWeights(IWeights oldWeights, IReadOnlyDictionary<Uri, Weight> newWeights)
        {
            const double significantWeightChange = 0.1;
            const double degradedWeightBorder = 0.7;
            foreach (var (replica, newWeight) in newWeights)
            {
                var previousWeight = oldWeights.Get(replica, settings.WeightsTTL)?.Value ?? settings.InitialWeight;

                if (Math.Abs(previousWeight - newWeight.Value) > significantWeightChange ||
                    previousWeight > degradedWeightBorder && newWeight.Value < degradedWeightBorder)
                    log.Debug("Replica {ReplicaUrl} weight has changed from {PreviousWeight} to {NewWeight}",
                        replica, previousWeight.ToString("F4"), newWeight.Value.ToString("F4"));
            }
        }
    }
}