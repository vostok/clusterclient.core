using System;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Topology.TargetEnvironment
{
    /// <summary>
    /// Represents a cluster provider which uses an external delegate to provide target environment.
    /// </summary>
    public class AdHocTargetEnvironmentProvider : ITargetEnvironmentProvider
    {
        [NotNull]
        private readonly Func<string> environmentProvider;

        /// <param name="environmentProvider">An external delegate which provides target environment.</param>
        public AdHocTargetEnvironmentProvider([NotNull] Func<string> environmentProvider)
        {
            this.environmentProvider = environmentProvider ?? throw new ArgumentNullException(nameof(environmentProvider));
        }

        /// <inheritdoc />
        public string Find() => environmentProvider();
    }
}