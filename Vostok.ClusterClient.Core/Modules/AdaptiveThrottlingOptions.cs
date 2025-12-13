using System;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Modules
{
    /// <summary>
    /// Represents a configuration of <see cref="AdaptiveThrottlingModule"/> instance. 
    /// </summary>
    [PublicAPI]
    public class AdaptiveThrottlingOptions
    {
        /// <param name="storageKey">A key used to decouple statistics for different services. This parameter is REQUIRED</param>
        /// <param name="minutesToTrack">How much minutes of statistics will be tracked. Must be >= 1.</param>
        /// <param name="minimumRequests">A minimum requests count in <see cref="MinutesToTrack"/> minutes to reject any request.</param>
        /// <param name="criticalRatio">A minimum ratio of requests to accepts eligible for rejection. Must be > 1.</param>
        /// <param name="maximumRejectProbability">A cap on the request rejection probability to prevent eternal rejection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="storageKey"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minutesToTrack"/>, <paramref name="criticalRatio"/> or <paramref name="maximumRejectProbability"/> does not lie in expected range.</exception>
        [Obsolete("This constructor is obsolete. Instead use constructor without storageKey parameter", false)]
        public AdaptiveThrottlingOptions(
            [NotNull] string storageKey,
            int minutesToTrack = ClusterClientDefaults.AdaptiveThrottlingMinutesToTrack,
            int minimumRequests = ClusterClientDefaults.AdaptiveThrottlingMinimumRequests,
            double criticalRatio = ClusterClientDefaults.AdaptiveThrottlingCriticalRatio,
            double maximumRejectProbability = ClusterClientDefaults.AdaptiveThrottlingRejectProbabilityCap)
            : this(minutesToTrack, minimumRequests, criticalRatio, maximumRejectProbability)
        {
            StorageKey = storageKey;
        }

        /// <param name="minutesToTrack">How much minutes of statistics will be tracked. Must be >= 1.</param>
        /// <param name="minimumRequests">A minimum requests count in <see cref="MinutesToTrack"/> minutes to reject any request.</param>
        /// <param name="criticalRatio">A minimum ratio of requests to accepts eligible for rejection. Must be > 1.</param>
        /// <param name="maximumRejectProbability">A cap on the request rejection probability to prevent eternal rejection.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minutesToTrack"/>, <paramref name="criticalRatio"/> or <paramref name="maximumRejectProbability"/> does not lie in expected range.</exception>
        // binary backwards compatibility overload
        public AdaptiveThrottlingOptions(int minutesToTrack, int minimumRequests, double criticalRatio, double maximumRejectProbability)
            : this(minutesToTrack, minimumRequests, criticalRatio, maximumRejectProbability,
                ClusterClientDefaults.AdaptiveThrottlingTrackGranularStatistics)
        {
        }

        /// <param name="minutesToTrack">How much minutes of statistics will be tracked. Must be >= 1.</param>
        /// <param name="minimumRequests">A minimum requests count in <see cref="MinutesToTrack"/> minutes to reject any request.</param>
        /// <param name="criticalRatio">A minimum ratio of requests to accepts eligible for rejection. Must be > 1.</param>
        /// <param name="maximumRejectProbability">A cap on the request rejection probability to prevent eternal rejection.</param>
        /// <param name="trackGranularStatistics">Whether to allow tracking granular statistics or not.</param>
        /// <param name="anomalousStatisticsThreshold">Minimum statistics ratio for granular statistics to be deemed anomalous.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minutesToTrack"/>, <paramref name="criticalRatio"/>,
        /// <paramref name="maximumRejectProbability"/> or <paramref name="anomalousStatisticsThreshold"/> does not lie in expected range.</exception>
        public AdaptiveThrottlingOptions(
            int minutesToTrack = ClusterClientDefaults.AdaptiveThrottlingMinutesToTrack,
            int minimumRequests = ClusterClientDefaults.AdaptiveThrottlingMinimumRequests,
            double criticalRatio = ClusterClientDefaults.AdaptiveThrottlingCriticalRatio,
            double maximumRejectProbability = ClusterClientDefaults.AdaptiveThrottlingRejectProbabilityCap,
            bool trackGranularStatistics = ClusterClientDefaults.AdaptiveThrottlingTrackGranularStatistics,
            double anomalousStatisticsThreshold = ClusterClientDefaults.AdaptiveThrottlingAnomalousStatisticsThreshold)
        {
            if (minutesToTrack < 1)
                throw new ArgumentOutOfRangeException(nameof(minutesToTrack), "Minutes to track parameter must be >= 1.");

            if (criticalRatio <= 1.0)
                throw new ArgumentOutOfRangeException(nameof(criticalRatio), "Critical ratio must be in (1; +inf) range.");
            
            if (anomalousStatisticsThreshold <= 1.0)
                throw new ArgumentOutOfRangeException(nameof(anomalousStatisticsThreshold), "Critical ratio must be in (1; +inf) range.");

            if (maximumRejectProbability < 0.0 || maximumRejectProbability > 1.0)
                throw new ArgumentOutOfRangeException(nameof(maximumRejectProbability), "Maximum rejection probability must be in [0; 1] range.");
            
            StorageKey = string.Empty;
            MinutesToTrack = minutesToTrack;
            MinimumRequests = minimumRequests;
            CriticalRatio = criticalRatio;
            MaximumRejectProbability = maximumRejectProbability;
        }
        
        /// <summary>
        /// A key used to decouple statistics for different services.
        /// </summary>
        [NotNull]
        [Obsolete("This property is obsolete. Since it was transferred to property in AdaptiveThrottlingModule.", false)]
        public string StorageKey { get; }

        /// <summary>
        /// How much minutes of statistics will be tracked. Must be >= 1.
        /// </summary>
        public int MinutesToTrack { get; }

        /// <summary>
        /// A minimum requests count in <see cref="MinutesToTrack"/> minutes to reject any request.
        /// </summary>
        public int MinimumRequests { get; }

        /// <summary>
        /// A minimum ratio of requests to accepts eligible for rejection. Must be > 1.
        /// </summary>
        public double CriticalRatio { get; }

        /// <summary>
        /// A cap on the request rejection probability to prevent eternal rejection.
        /// </summary>
        public double MaximumRejectProbability { get; }
        
        /// <summary>
        /// Whether to allow tracking granular statistics or not.
        /// </summary>
        public bool TrackGranularStatistics { get; }

        /// <summary>
        /// A minimum ratio of granular requests / accepts ratio to that of global statistics so that
        /// the granular statistics are deemed "anomalous" and have a probability of skipping
        /// request result insertion into global statistics.  
        /// </summary>
        public double AnomalousStatisticsThreshold { get; }
    }
}