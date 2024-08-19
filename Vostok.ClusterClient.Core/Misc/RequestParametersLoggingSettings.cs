using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Misc
{
    [PublicAPI]
    public class RequestParametersLoggingSettings
    {
        public RequestParametersLoggingSettings(bool enabled)
        {
            Enabled = enabled;
        }

        /// <summary>
        /// Flag that decides whether to log request or response parameters.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// <para>Case-insensitive whitelist of parameter keys to be logged.</para>
        /// <para><c>null</c> value allows all keys.</para>
        /// <para>Takes precedence over <see cref="Blacklist"/>.</para>
        /// </summary>
        [CanBeNull]
        public IReadOnlyCollection<string> Whitelist { get; set; }

        /// <summary>
        /// <para>Case-insensitive blacklist of parameter keys to be logged.</para>
        /// <para><c>null</c> value allows all keys.</para>
        /// </summary>
        [CanBeNull]
        public IReadOnlyCollection<string> Blacklist { get; set; }

        public static implicit operator RequestParametersLoggingSettings(bool enabled) =>
            new RequestParametersLoggingSettings(enabled);
    }
}