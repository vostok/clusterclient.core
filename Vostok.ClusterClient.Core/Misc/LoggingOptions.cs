using System;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Misc
{
    /// <summary>
    /// A set of ClusterClient logging settings.
    /// </summary>
    [PublicAPI]
    public class LoggingOptions
    {
        private RequestParametersLoggingSettings logQueryString = false;
        private RequestParametersLoggingSettings logRequestHeaders = false;
        private RequestParametersLoggingSettings logResponseHeaders = false;

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

        /// <summary>
        /// <para>Request query parameters logging options.</para>
        /// <para>By default, query parameters are not logged at all.</para>
        /// </summary>
        [NotNull]
        public RequestParametersLoggingSettings LogQueryString
        {
            get =>
                logQueryString;
            set =>
                logQueryString = value.ToCaseInsensitive() ?? throw new ArgumentNullException(nameof(LogQueryString));
        }

        /// <summary>
        /// <para>Request headers logging options.</para>
        /// <para>By default, request headers are not logged at all.</para>
        /// </summary>
        [NotNull]
        public RequestParametersLoggingSettings LogRequestHeaders
        {
            get =>
                logRequestHeaders;
            set =>
                logRequestHeaders = value.ToCaseInsensitive() ?? throw new ArgumentNullException(nameof(LogRequestHeaders));
        }

        /// <summary>
        /// <para>Response headers logging options.</para>
        /// <para>By default, response headers are not logged at all.</para>
        /// </summary>
        [NotNull]
        public RequestParametersLoggingSettings LogResponseHeaders
        {
            get =>
                logResponseHeaders;
            set =>
                logResponseHeaders = value.ToCaseInsensitive() ?? throw new ArgumentNullException(nameof(LogResponseHeaders));
        }
    }
}