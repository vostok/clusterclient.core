using System;
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
        /// <param name="minutesToTrack">See <see cref="AdaptiveThrottlingOptions.MinutesToTrack"/>.</param>
        /// <param name="minimumRequests">See <see cref="AdaptiveThrottlingOptions.MinimumRequests"/>.</param>
        /// <param name="criticalRatio">See <see cref="AdaptiveThrottlingOptions.CriticalRatio"/>.</param>
        /// <param name="maximumRejectProbability">See <see cref="AdaptiveThrottlingOptions.MaximumRejectProbability"/>.</param>
        public static void SetupAdaptiveThrottling(
            this IClusterClientConfiguration configuration,
            string storageKey,
            int minutesToTrack = ClusterClientDefaults.AdaptiveThrottlingMinutesToTrack,
            int minimumRequests = ClusterClientDefaults.AdaptiveThrottlingMinimumRequests,
            double criticalRatio = ClusterClientDefaults.AdaptiveThrottlingCriticalRatio,
            double maximumRejectProbability = ClusterClientDefaults.AdaptiveThrottlingRejectProbabilityCap)
        {
            var options = AdaptiveThrottlingOptionsBuilder.Build(
                setup =>
                {
                    setup.WithDefaultOptions(
                        new AdaptiveThrottlingOptions(
                            minutesToTrack,
                            minimumRequests,
                            criticalRatio,
                            maximumRejectProbability
                        )
                    );
                },
                storageKey
            );

            configuration.AddRequestModule(new AdaptiveThrottlingModule(options), typeof(AbsoluteUrlSenderModule));
        }

        /// <summary>
        /// <para>Sets up an adaptive client throttling mechanism with given parameters using <see cref="IClusterClientConfiguration.TargetServiceName"/> and <see cref="IClusterClientConfiguration.TargetEnvironment"/> as a storage key.</para>
        /// <para> <b>N.B.</b> Ensure that <see cref="IClusterClientConfiguration.TargetServiceName"/> and <see cref="IClusterClientConfiguration.TargetEnvironment"/> is set before calling this method.</para>
        /// </summary>
        /// <param name="configuration">A configuration to be modified.</param>
        /// <param name="minutesToTrack">See <see cref="AdaptiveThrottlingOptions.MinutesToTrack"/>.</param>
        /// <param name="minimumRequests">See <see cref="AdaptiveThrottlingOptions.MinimumRequests"/>.</param>
        /// <param name="criticalRatio">See <see cref="AdaptiveThrottlingOptions.CriticalRatio"/>.</param>
        /// <param name="maximumRejectProbability">See <see cref="AdaptiveThrottlingOptions.MaximumRejectProbability"/>.</param>
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
        /// Sets up an adaptive client throttling mechanism with given options.
        /// </summary>
        /// <param name="configuration">A configuration to be modified.</param>
        /// <param name="optionsBuilder">See <see cref="AdaptiveThrottlingOptionsBuilder"/> </param>
        public static void SetupAdaptiveThrottling(
            this IClusterClientConfiguration configuration,
            Action<IAdaptiveThrottlingOptionsBuilder> optionsBuilder)
        {
            var storageKey = GenerateStorageKey(configuration);

            SetupAdaptiveThrottling(configuration, storageKey, optionsBuilder);
        }

        /// <summary>
        /// Sets up an adaptive client throttling mechanism with given options.
        /// </summary>
        /// <param name="configuration">A configuration to be modified.</param>
        /// <param name="storageKey">A key used to decouple statistics for different services.</param>
        /// <param name="optionsBuilder">See <see cref="AdaptiveThrottlingOptionsBuilder"/> </param>
        public static void SetupAdaptiveThrottling(
            this IClusterClientConfiguration configuration,
            string storageKey,
            Action<IAdaptiveThrottlingOptionsBuilder> optionsBuilder)
        {
            var builder = AdaptiveThrottlingOptionsBuilder.Build(optionsBuilder, storageKey);
            configuration.AddRequestModule(new AdaptiveThrottlingModule(builder), typeof(AbsoluteUrlSenderModule));
        }
    }
}