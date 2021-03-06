﻿using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Strategies;

namespace Vostok.Clusterclient.Core.Sending
{
    /// <summary>
    /// A request sending abstraction used by implementations of <see cref="IRequestStrategy"/> interface.
    /// </summary>
    [PublicAPI]
    public interface IRequestSender
    {
        /// <summary>
        /// <para>Sends given <paramref name="request"/> to given <paramref name="replica"/> with provided <paramref name="connectionTimeout"/>, <paramref name="timeout"/> and <paramref name="cancellationToken"/>.</para>
        /// <para>Returns a <see cref="ResponseVerdict"/> with computed <see cref="ReplicaResult"/> and response time.</para>
        /// </summary>
        [ItemNotNull]
        Task<ReplicaResult> SendToReplicaAsync([NotNull] Uri replica, [NotNull] Request request, TimeSpan? connectionTimeout, TimeSpan timeout, CancellationToken cancellationToken);
    }
}