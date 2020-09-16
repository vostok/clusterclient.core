using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Topology.TargetEnvironment
{
    /// <summary>
    /// Represents a storage of target environment.
    /// </summary>
    [PublicAPI]
    public interface ITargetEnvironmentProvider
    {
        /// <summary>
        /// <para>Returns target environment to use for cluster communication.</para>
        /// <para>May return null when target environment is unknown.</para>
        /// <para>Implementations of this method MUST BE thread-safe.</para>
        /// </summary>
        [CanBeNull]
        string Find();
    }
}