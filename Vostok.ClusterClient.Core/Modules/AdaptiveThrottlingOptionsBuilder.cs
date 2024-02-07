using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Modules
{
    internal class AdaptiveThrottlingOptionsBuilder : IAdaptiveThrottlingOptionsBuilder
    {
        private static readonly Array PriorityList = Enum.GetValues(typeof(RequestPriority));
        private readonly Dictionary<RequestPriority, AdaptiveThrottlingOptions> options;
        private readonly string storageKey;

        /// <param name="storageKey">A key used to decouple statistics for different services. This parameter is REQUIRED</param>
        /// <exception cref="ArgumentNullException"><paramref name="storageKey"/> is null.</exception>
        public AdaptiveThrottlingOptionsBuilder([NotNull] string storageKey)
        {
            this.storageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));
            options = new Dictionary<RequestPriority, AdaptiveThrottlingOptions>();
        }

        public IAdaptiveThrottlingOptionsBuilder WithDefaultOptions(AdaptiveThrottlingOptions adaptiveThrottlingOptions)
        {
            foreach (RequestPriority priority in PriorityList)
            {
                options[priority] = adaptiveThrottlingOptions;
            }

            return this;
        }
        
        public IAdaptiveThrottlingOptionsBuilder WithPriorityParameters(RequestPriority priority, AdaptiveThrottlingOptions adaptiveThrottlingOptions)
        {
            options[priority] = adaptiveThrottlingOptions;
            return this;
        }

        public static AdaptiveThrottlingOptionsPerPriority Build(Action<IAdaptiveThrottlingOptionsBuilder> setup, string storageKey)
        {
            var builder = new AdaptiveThrottlingOptionsBuilder(storageKey);
            setup?.Invoke(builder);
            return new AdaptiveThrottlingOptionsPerPriority(builder.storageKey, builder.options);
        }
    }
}