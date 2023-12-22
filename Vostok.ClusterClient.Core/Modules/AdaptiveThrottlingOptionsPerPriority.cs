using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Modules
{
    /// <summary>
    /// Represents a configuration of <see cref="AdaptiveThrottlingModule"/> instance. 
    /// </summary>
    internal class AdaptiveThrottlingOptionsPerPriority
    {
        private static readonly AdaptiveThrottlingOptions DefaultThrottlingOptions = new();
        private static readonly Array PriorityList = Enum.GetValues(typeof(RequestPriority));

        private readonly Dictionary<RequestPriority, AdaptiveThrottlingOptions> parameters;

        /// <param name="storageKey">A key used to decouple statistics for different services. This parameter is REQUIRED</param>
        /// <param name="options">A Dictionary in which provide adaptive throttling parameters by priority</param>
        /// <exception cref="ArgumentNullException"><paramref name="storageKey"/> is null.</exception>
        public AdaptiveThrottlingOptionsPerPriority(
            [NotNull] string storageKey,
            Dictionary<RequestPriority, AdaptiveThrottlingOptions> options = null)
        {
            StorageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));

            parameters = options == null
                ? new Dictionary<RequestPriority, AdaptiveThrottlingOptions>()
                : new Dictionary<RequestPriority, AdaptiveThrottlingOptions>(options);

            foreach (RequestPriority priority in PriorityList)
            {
                if (!parameters.ContainsKey(priority))
                {
                    parameters[priority] = DefaultThrottlingOptions;
                }
            }
        }

        /// <summary>
        /// A key used to decouple statistics for different services.
        /// </summary>
        [NotNull]
        public string StorageKey { get; }

        /// <summary>
        /// Dictionary in which stored adaptive throttling parameters by priority.
        /// </summary>
        public IReadOnlyDictionary<RequestPriority, AdaptiveThrottlingOptions> Parameters => parameters;
    }
}