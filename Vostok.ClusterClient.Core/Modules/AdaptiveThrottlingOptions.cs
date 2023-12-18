using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

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
        /// <param name="minimumRequests">A minimum requests count in <see cref="AdaptiveThrottlingParameters.MinutesToTrack"/> minutes to reject any request.</param>
        /// <param name="criticalRatio">A minimum ratio of requests to accepts eligible for rejection. Must be > 1.</param>
        /// <param name="maximumRejectProbability">A cap on the request rejection probability to prevent eternal rejection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="storageKey"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minutesToTrack"/>, <paramref name="criticalRatio"/> or <paramref name="maximumRejectProbability"/> does not lie in expected range.</exception>
        public AdaptiveThrottlingOptions(
            [NotNull] string storageKey,
            int minutesToTrack = ClusterClientDefaults.AdaptiveThrottlingMinutesToTrack,
            int minimumRequests = ClusterClientDefaults.AdaptiveThrottlingMinimumRequests,
            double criticalRatio = ClusterClientDefaults.AdaptiveThrottlingCriticalRatio,
            double maximumRejectProbability = ClusterClientDefaults.AdaptiveThrottlingRejectProbabilityCap)
        {
            StorageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));

            var defaultParameters = new AdaptiveThrottlingParameters(
                minutesToTrack,
                minimumRequests,
                criticalRatio,
                maximumRejectProbability
            );

            Parameters = new Dictionary<RequestPriority, AdaptiveThrottlingParameters>
            {
                [RequestPriority.Critical] = defaultParameters,
                [RequestPriority.Ordinary] = defaultParameters,
                [RequestPriority.Sheddable] = defaultParameters
            };
        }

        /// <param name="storageKey">A key used to decouple statistics for different services. This parameter is REQUIRED</param>
        /// <param name="parameters">A Dictionary in which provide adaptive throttling parameters by priority</param>
        /// <exception cref="ArgumentNullException"><paramref name="storageKey"/> is null.</exception>
        public AdaptiveThrottlingOptions(
            [NotNull] string storageKey,
            Dictionary<RequestPriority, AdaptiveThrottlingParameters> parameters)
        {
            StorageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));

            Parameters = parameters == null
                ? new Dictionary<RequestPriority, AdaptiveThrottlingParameters>()
                : new Dictionary<RequestPriority, AdaptiveThrottlingParameters>(parameters);

            var defaultParameters = new AdaptiveThrottlingParameters();
            if (!Parameters.ContainsKey(RequestPriority.Critical))
            {
                Parameters[RequestPriority.Critical] = defaultParameters;
            }

            if (!Parameters.ContainsKey(RequestPriority.Ordinary))
            {
                Parameters[RequestPriority.Ordinary] = defaultParameters;
            }

            if (!Parameters.ContainsKey(RequestPriority.Sheddable))
            {
                Parameters[RequestPriority.Sheddable] = defaultParameters;
            }

            StorageKey = storageKey;
        }

        /// <summary>
        /// A key used to decouple statistics for different services.
        /// </summary>
        [NotNull]
        public string StorageKey { get; }

        /// <summary>
        /// Dictionary in which stored adaptive throttling parameters by priority.
        /// </summary>
        public Dictionary<RequestPriority, AdaptiveThrottlingParameters> Parameters { get; }

        /// <summary>
        /// <para>Produces a new <see cref="AdaptiveThrottlingOptions"/> instance where adaptive throttling parameters by priority will have given value.</para>
        /// <para>See <see cref="AdaptiveThrottlingOptions"/> class documentation for details.</para>
        /// </summary>
        /// <param name="priority">Priority name <see cref="RequestPriority" /> for details</param>
        /// <param name="criticalRequestParameters">Throttling parameters by priority.</param>
        /// <returns>A new <see cref="AdaptiveThrottlingOptions"/> object with updated throttling parameters for given priority.</returns>
        public AdaptiveThrottlingOptions WithPriorityParameters(RequestPriority priority, AdaptiveThrottlingParameters criticalRequestParameters)
        {
            var parameters = new Dictionary<RequestPriority, AdaptiveThrottlingParameters>(Parameters)
            {
                [RequestPriority.Critical] = criticalRequestParameters
            };
            return new AdaptiveThrottlingOptions(StorageKey, parameters);
        }
    }

    /// <summary>
    /// Represents a parameters of <see cref="AdaptiveThrottlingModule"/> instance by request priority. 
    /// </summary>
    [PublicAPI]
    public class AdaptiveThrottlingParameters
    {
        /// <param name="minutesToTrack">How much minutes of statistics will be tracked. Must be >= 1.</param>
        /// <param name="minimumRequests">A minimum requests count in <see cref="MinutesToTrack"/> minutes to reject any request.</param>
        /// <param name="criticalRatio">A minimum ratio of requests to accepts eligible for rejection. Must be > 1.</param>
        /// <param name="maximumRejectProbability">A cap on the request rejection probability to prevent eternal rejection.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minutesToTrack"/>, <paramref name="criticalRatio"/> or <paramref name="maximumRejectProbability"/> does not lie in expected range.</exception>
        public AdaptiveThrottlingParameters(
            int minutesToTrack = ClusterClientDefaults.AdaptiveThrottlingMinutesToTrack,
            int minimumRequests = ClusterClientDefaults.AdaptiveThrottlingMinimumRequests,
            double criticalRatio = ClusterClientDefaults.AdaptiveThrottlingCriticalRatio,
            double maximumRejectProbability = ClusterClientDefaults.AdaptiveThrottlingRejectProbabilityCap)
        {
            if (minutesToTrack < 1)
                throw new ArgumentOutOfRangeException(nameof(minutesToTrack), "Minutes to track parameter must be >= 1.");

            if (criticalRatio <= 1.0)
                throw new ArgumentOutOfRangeException(nameof(criticalRatio), "Critical ratio must be in (1; +inf) range.");

            if (maximumRejectProbability < 0.0 || maximumRejectProbability > 1.0)
                throw new ArgumentOutOfRangeException(nameof(maximumRejectProbability), "Maximum rejection probability must be in [0; 1] range.");

            MinutesToTrack = minutesToTrack;
            MinimumRequests = minimumRequests;
            CriticalRatio = criticalRatio;
            MaximumRejectProbability = maximumRejectProbability;
        }

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
    }
}