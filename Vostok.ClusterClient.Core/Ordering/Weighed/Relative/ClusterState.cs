﻿using System;
using Vostok.Clusterclient.Core.Ordering.Weighed.Relative.Interfaces;
using Vostok.Commons.Threading;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Relative
{
    internal class ClusterState
    {
        private readonly Func<IActiveStatistic> activeStatisticFactory;
        private readonly IStatisticHistory statisticHistory;

        public AtomicBoolean IsUpdatingNow { get; }
        public DateTime LastUpdateTimestamp { get; private set; }
        public IActiveStatistic CurrentStatistic { get; private set; }
        public IWeights Weights { get; }

        public ClusterState(
            RelativeWeightSettings settings, 
            Func<IActiveStatistic> activeStatisticFactory = null, 
            IStatisticHistory statisticHistory = null, 
            IWeights weights = null)
        {
            this.activeStatisticFactory = activeStatisticFactory ?? CreateDefault;
            this.statisticHistory = statisticHistory ?? new StatisticsHistory(settings.StatisticTTL);

            IsUpdatingNow = new AtomicBoolean(false);
            LastUpdateTimestamp = DateTime.UtcNow;
            Weights = weights ?? new Weights(settings);
            CurrentStatistic = this.activeStatisticFactory();

            IActiveStatistic CreateDefault() =>
                new ActiveStatistic(settings.StatisticSmoothingConstant, settings.PenaltyMultiplier);
        }

        public ClusterStatistic FlushCurrentStatisticToHistory(DateTime currentTimestamp)
        {
            LastUpdateTimestamp = currentTimestamp;

            var previousActiveStatistic = CurrentStatistic;
            CurrentStatistic = activeStatisticFactory();

            var penalty = previousActiveStatistic
                .CalculatePenalty();
            var clusterStatistic = previousActiveStatistic
                .CalculateClusterStatistic(currentTimestamp, penalty, statisticHistory.Get());
            
            statisticHistory.Update(clusterStatistic);
            
            return clusterStatistic;
        }
    }
}