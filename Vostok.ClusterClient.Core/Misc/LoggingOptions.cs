using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Misc
{
    /// <summary>
    /// A set of ClusterClient logging settings.
    /// </summary>
    [PublicAPI]
    public class LoggingOptions
    {
        /// <summary>
        /// <para>Gets or sets whether to log request details before execution.</para>
        /// <para>This parameter is optional and has a default value (see <see cref="ClusterClientDefaults.LogRequestDetails"/>).</para>
        /// </summary>
        public bool LogRequestDetails { get; set; } = ClusterClientDefaults.LogRequestDetails;

        /// <summary>
        /// <para>Gets or sets whether to log result details after execution.</para>
        /// <para>This parameter is optional and has a default value (see <see cref="ClusterClientDefaults.LogResultDetails"/>).</para>
        /// </summary>
        public bool LogResultDetails { get; set; } = ClusterClientDefaults.LogResultDetails;

        /// <summary>
        /// <para>Gets or sets whether to log requests to each replica.</para>
        /// <para>This parameter is optional and has a default value (see <see cref="ClusterClientDefaults.LogReplicaRequests"/>).</para>
        /// </summary>
        public bool LogReplicaRequests { get; set; } = ClusterClientDefaults.LogReplicaRequests;

        /// <summary>
        /// <para>Gets or sets whether to log results from each replica.</para>
        /// <para>This parameter is optional and has a default value (see <see cref="ClusterClientDefaults.LogReplicaResults"/>).</para>
        /// </summary>
        public bool LogReplicaResults { get; set; } = ClusterClientDefaults.LogReplicaResults;

        /// <summary>
        /// If <see cref="Misc.LoggingMode.Detailed"/> is set, each message will be written individually according to the flags set.
        /// If <see cref="Misc.LoggingMode.SingleShortMessage"/> is set, only one short message about communication with the cluster will be logged.
        /// If <see cref="Misc.LoggingMode.SingleVerboseMessage"/> is set, only one detailed message about communication with the cluster will be logged with the results from each replica.
        /// </summary>
        public LoggingMode LoggingMode { get; set; } = ClusterClientDefaults.LoggingMode;
    }
}