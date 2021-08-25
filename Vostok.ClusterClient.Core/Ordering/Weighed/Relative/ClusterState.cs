using System;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Ordering.Weighed.Relative.Interfaces;
using Vostok.Commons.Threading;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Relative
{
    internal class ClusterState
    {
        public AtomicBoolean IsUpdatingNow { get; }
        public DateTime LastUpdateTimestamp { get;  set; }
        public ITimeProvider TimeProvider { get; }
        public IRelativeWeightCalculator RelativeWeightCalculator { get; }
        public IRawClusterStatistic CurrentStatistic { get; private set; }
        public IStatisticHistory StatisticHistory { get; private set; }
        public IWeightsNormalizer WeightsNormalizer { get; }
        public IWeights Weights { get; }
        
        public ClusterState(
            IRelativeWeightCalculator relativeWeightCalculator = null,
            IRawClusterStatistic rawClusterStatistic = null,
            ITimeProvider timeProvider = null,
            IStatisticHistory statisticHistory = null, 
            IWeightsNormalizer weightsNormalizer = null,
            IWeights weights = null)
        {
            IsUpdatingNow = new AtomicBoolean(false);
            TimeProvider = timeProvider ?? new TimeProvider();
            RelativeWeightCalculator = relativeWeightCalculator ?? new RelativeWeightCalculator();
            Weights = weights ?? new Weights();
            WeightsNormalizer = weightsNormalizer ?? new WeightsNormalizer();
            CurrentStatistic = rawClusterStatistic ?? new RawClusterStatistic();
            StatisticHistory = statisticHistory ?? new StatisticsHistory();

            LastUpdateTimestamp = TimeProvider.GetCurrentTime();
        }

        public IRawClusterStatistic SwapToNewRawStatistic()
        {
            var previousRawStatistic = CurrentStatistic;

            CurrentStatistic = new RawClusterStatistic();

            return previousRawStatistic;
        }
    }
}