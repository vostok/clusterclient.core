using System.Collections.Generic;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;

namespace Vostok.Clusterclient.Core
{
    public static partial class IClusterClientConfigurationExtensions
    {
        /// <summary>
        /// Sets up an adaptive client throttling mechanism with given parameters.
        /// </summary>
        /// <param name="configuration">A configuration to be modified.</param>
        /// <param name="storageKey">See <see cref="AdaptiveThrottlingOptions.StorageKey"/>.</param>
        /// <param name="minutesToTrack">See <see cref="AdaptiveThrottlingParameters.MinutesToTrack"/>.</param>
        /// <param name="minimumRequests">See <see cref="AdaptiveThrottlingParameters.MinimumRequests"/>.</param>
        /// <param name="criticalRatio">See <see cref="AdaptiveThrottlingParameters.CriticalRatio"/>.</param>
        /// <param name="maximumRejectProbability">See <see cref="AdaptiveThrottlingParameters.MaximumRejectProbability"/>.</param>
        public static void SetupAdaptiveThrottling(
            this IClusterClientConfiguration configuration,
            string storageKey,
            int minutesToTrack = ClusterClientDefaults.AdaptiveThrottlingMinutesToTrack,
            int minimumRequests = ClusterClientDefaults.AdaptiveThrottlingMinimumRequests,
            double criticalRatio = ClusterClientDefaults.AdaptiveThrottlingCriticalRatio,
            double maximumRejectProbability = ClusterClientDefaults.AdaptiveThrottlingRejectProbabilityCap)
        {
            var options = new AdaptiveThrottlingOptions(
                storageKey,
                minutesToTrack,
                minimumRequests,
                criticalRatio,
                maximumRejectProbability);

            configuration.AddRequestModule(new AdaptiveThrottlingModule(options), typeof(AbsoluteUrlSenderModule));
        }

        /// <summary>
        /// <para>Sets up an adaptive client throttling mechanism with given parameters using <see cref="IClusterClientConfiguration.TargetServiceName"/> and <see cref="IClusterClientConfiguration.TargetEnvironment"/> as a storage key.</para>
        /// <para> <b>N.B.</b> Ensure that <see cref="IClusterClientConfiguration.TargetServiceName"/> and <see cref="IClusterClientConfiguration.TargetEnvironment"/> is set before calling this method.</para>
        /// </summary>
        /// <param name="configuration">A configuration to be modified.</param>
        /// <param name="minutesToTrack">See <see cref="AdaptiveThrottlingParameters.MinutesToTrack"/>.</param>
        /// <param name="minimumRequests">See <see cref="AdaptiveThrottlingParameters.MinimumRequests"/>.</param>
        /// <param name="criticalRatio">See <see cref="AdaptiveThrottlingParameters.CriticalRatio"/>.</param>
        /// <param name="maximumRejectProbability">See <see cref="AdaptiveThrottlingParameters.MaximumRejectProbability"/>.</param>
        public static void SetupAdaptiveThrottling(
            this IClusterClientConfiguration configuration,
            int minutesToTrack = ClusterClientDefaults.AdaptiveThrottlingMinutesToTrack,
            int minimumRequests = ClusterClientDefaults.AdaptiveThrottlingMinimumRequests,
            double criticalRatio = ClusterClientDefaults.AdaptiveThrottlingCriticalRatio,
            double maximumRejectProbability = ClusterClientDefaults.AdaptiveThrottlingRejectProbabilityCap)
        {
            var storageKey = GenerateStorageKey(configuration);

            SetupAdaptiveThrottling(configuration, storageKey, minutesToTrack, minimumRequests, criticalRatio, maximumRejectProbability);
        }

        /// <summary>
        /// Configures default settings by request priority for adaptive client throttling mechanism.
        /// </summary>
        public static AdaptiveThrottlingOptions ConfigureAdaptiveThrottlingOptions(string storageKey, AdaptiveThrottlingParameters defaultParameters)
        {
            defaultParameters ??= new AdaptiveThrottlingParameters();
            
            var parameters = new Dictionary<RequestPriority, AdaptiveThrottlingParameters>
            {
                [RequestPriority.Critical] = defaultParameters,
                [RequestPriority.Ordinary] = defaultParameters,
                [RequestPriority.Sheddable] = defaultParameters
            };

            return new AdaptiveThrottlingOptions(storageKey, parameters);
        }

        /// <summary>
        /// Sets up an adaptive client throttling mechanism with given options.
        /// </summary>
        /// <param name="configuration">A configuration to be modified.</param>
        /// <param name="options">See <see cref="AdaptiveThrottlingOptions"/> </param>
        public static void SetupAdaptiveThrottling(
            this IClusterClientConfiguration configuration,
            AdaptiveThrottlingOptions options)
        {
            configuration.AddRequestModule(new AdaptiveThrottlingModule(options), typeof(AbsoluteUrlSenderModule));
        }
    }
}