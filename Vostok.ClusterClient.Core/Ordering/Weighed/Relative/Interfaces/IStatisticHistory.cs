using System;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Relative.Interfaces
{
    internal interface IStatisticHistory
    {
        AggregatedClusterStatistic Get();

        void Update(AggregatedClusterStatistic snapshot, TimeSpan statisticTTL);
    }
}