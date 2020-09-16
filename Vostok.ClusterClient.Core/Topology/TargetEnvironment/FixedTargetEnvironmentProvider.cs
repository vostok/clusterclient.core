using System;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Topology.TargetEnvironment
{
    /// <summary>
    /// Represents a target environment provider which always returns a fixed target environment
    /// </summary>
    public class FixedTargetEnvironmentProvider : ITargetEnvironmentProvider
    {
        [NotNull]
        private readonly string environment;

        /// <param name="environment">The environment name this provider should return.</param>
        public FixedTargetEnvironmentProvider([NotNull] string environment)
        {
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <inheritdoc />
        public string Find() => environment;
    }
}