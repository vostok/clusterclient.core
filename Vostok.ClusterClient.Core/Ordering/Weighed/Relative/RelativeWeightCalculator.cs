using System;
using Vostok.Clusterclient.Core.Ordering.Weighed.Relative.Interfaces;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Relative
{
    internal class RelativeWeightCalculator : IRelativeWeightCalculator
    {
        private readonly RelativeWeightSettings settings;
        private readonly ILog log;

        public RelativeWeightCalculator(RelativeWeightSettings settings, ILog log = null)
        {
            this.settings = settings;
            this.log = log ?? new SilentLog();
        }

        public Weight Calculate(in AggregatedStatistic clusterAggregatedStatistic, in AggregatedStatistic replicaAggregatedStatistic, in Weight previousWeight)
        {
            var weightByStatuses = clusterAggregatedStatistic.TotalCount / settings.WeightUpdatePeriod.TotalSeconds <= settings.WeightByStatusesRpsThreshold;
            
            var rawWeight = weightByStatuses
                ? WeighingHelper.ComputeWeightByStatuses(replicaAggregatedStatistic.TotalCount, replicaAggregatedStatistic.ErrorFraction, clusterAggregatedStatistic.TotalCount, clusterAggregatedStatistic.ErrorFraction, settings.Sensitivity)
                : WeighingHelper.ComputeWeightByLatency(replicaAggregatedStatistic.Mean, replicaAggregatedStatistic.StdDev, clusterAggregatedStatistic.Mean, clusterAggregatedStatistic.StdDev, settings.Sensitivity);
            
            log.Info($"WeightsByStatuses: {weightByStatuses}, {rawWeight}");
            
            var newWeight = EnforceRelativeLimits(rawWeight);
            var smoothingConstant = newWeight > previousWeight.Value
                ? settings.WeightsRaiseSmoothingConstant
                : settings.WeightsDownSmoothingConstant;
            
            var smoothedWeight = SmoothingHelper
                .SmoothValue(newWeight, previousWeight.Value, clusterAggregatedStatistic.Timestamp, previousWeight.Timestamp, smoothingConstant);
            return new Weight(smoothedWeight, replicaAggregatedStatistic.Timestamp);

            double EnforceRelativeLimits(double rowWeight) =>
                Math.Min(1.0, Math.Max(settings.MinWeight, rowWeight));
        }
    }
}