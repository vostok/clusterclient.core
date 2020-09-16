using System;
using System.Linq;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Topology.TargetEnvironment
{
    /// <summary>
    /// Represents a composite storage of target environment providers.
    /// </summary>
    public class CompositeTargetEnvironmentProvider : ITargetEnvironmentProvider
    {
        [NotNull]
        private readonly ITargetEnvironmentProvider[] innerProviders;

        /// <param name="innerProviders">An array of target environment providers. First non-null value will be used.</param>
        public CompositeTargetEnvironmentProvider([ItemNotNull] [NotNull] params ITargetEnvironmentProvider[] innerProviders)
        {
            if (innerProviders == null)
                throw new ArgumentNullException(nameof(innerProviders));

            if (innerProviders.Any(p => p == null))
                throw new ArgumentException("Some of the innerProviders are null.", nameof(innerProviders));

            this.innerProviders = innerProviders;
        }

        /// <inheritdoc />
        public string Find()
        {
            return innerProviders
                .Select(inner => inner.Find())
                .FirstOrDefault(environment => !string.IsNullOrEmpty(environment));
        }
    }
}