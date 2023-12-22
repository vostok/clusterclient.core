using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Modules
{
    /// <summary>
    /// Build configuration for <see cref="AdaptiveThrottlingModule"/>, create new instance of <see cref="AdaptiveThrottlingOptionsPerPriority"/>. 
    /// </summary>
    [PublicAPI]
    public class AdaptiveThrottlingOptionsBuilder
    {
        private static readonly Array PriorityList = Enum.GetValues(typeof(RequestPriority));
        private readonly Dictionary<RequestPriority, AdaptiveThrottlingOptions> options;
        private string storageKey;

        /// <param name="storageKey">A key used to decouple statistics for different services. This parameter is REQUIRED</param>
        /// <exception cref="ArgumentNullException"><paramref name="storageKey"/> is null.</exception>
        public AdaptiveThrottlingOptionsBuilder([NotNull] string storageKey)
        {
            this.storageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));
            options = new Dictionary<RequestPriority, AdaptiveThrottlingOptions>();
        }

        public AdaptiveThrottlingOptionsBuilder WithDefaultOptions(AdaptiveThrottlingOptions defaultThrottlingOptions)
        {
            foreach (RequestPriority priority in PriorityList)
            {
                options[priority] = defaultThrottlingOptions;
            }

            return this;
        }

        /// <summary>
        /// <para>Produces a new <see cref="AdaptiveThrottlingOptions"/> instance where adaptive throttling parameters by priority will have given value.</para>
        /// <para>See <see cref="AdaptiveThrottlingOptions"/> class documentation for details.</para>
        /// </summary>
        /// <param name="priority">Priority name <see cref="RequestPriority" /> for details</param>
        /// <param name="adaptiveThrottlingOptions">Adaptive throttling parameters by priority.</param>
        /// <returns>A new <see cref="AdaptiveThrottlingOptions"/> object with updated throttling parameters for given priority.</returns>
        public AdaptiveThrottlingOptionsBuilder WithPriorityParameters(RequestPriority priority, AdaptiveThrottlingOptions adaptiveThrottlingOptions)
        {
            options[priority] = adaptiveThrottlingOptions;
            return this;
        }

        internal AdaptiveThrottlingOptionsPerPriority Build()
        {
            return new AdaptiveThrottlingOptionsPerPriority(storageKey, options);
        }
    }
}