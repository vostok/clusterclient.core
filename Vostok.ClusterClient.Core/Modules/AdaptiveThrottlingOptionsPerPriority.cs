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
    public class AdaptiveThrottlingOptionsPerPriority
    {
        private static AdaptiveThrottlingOptions defaultThrottlingOptions = new AdaptiveThrottlingOptions(string.Empty);
        private static Array priorityList = Enum.GetValues(typeof(RequestPriority));
        
        /// <param name="storageKey">A key used to decouple statistics for different services. This parameter is REQUIRED</param>
        /// <param name="options">A Dictionary in which provide adaptive throttling parameters by priority</param>
        /// <exception cref="ArgumentNullException"><paramref name="storageKey"/> is null.</exception>
        public AdaptiveThrottlingOptionsPerPriority(
            [NotNull] string storageKey,
            Dictionary<RequestPriority, AdaptiveThrottlingOptions> options = null)
        {
            StorageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));

            Parameters = options == null
                ? new Dictionary<RequestPriority, AdaptiveThrottlingOptions>()
                : new Dictionary<RequestPriority, AdaptiveThrottlingOptions>(options);
                    
            foreach (RequestPriority priority in priorityList)
            {
                if (!Parameters.ContainsKey(priority))
                {
                    Parameters[priority] = defaultThrottlingOptions;
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
        public Dictionary<RequestPriority, AdaptiveThrottlingOptions> Parameters { get; }
        
    }
    
    /// <summary>
    /// Build configuration for <see cref="AdaptiveThrottlingModule"/>, create new instance of <see cref="AdaptiveThrottlingOptionsPerPriority"/>. 
    /// </summary>
    [PublicAPI]
    public class AdaptiveThrottlingOptionsBuilder
    {
        private static Array priorityList = Enum.GetValues(typeof(RequestPriority));
        private Dictionary<RequestPriority, AdaptiveThrottlingOptions> options;
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
            foreach (RequestPriority priority in priorityList)
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

        public AdaptiveThrottlingOptionsPerPriority Build()
        {
            return new AdaptiveThrottlingOptionsPerPriority(storageKey, options);
        }
        
    }
}