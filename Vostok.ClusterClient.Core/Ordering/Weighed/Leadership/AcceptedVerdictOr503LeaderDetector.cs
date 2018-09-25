﻿using Vostok.ClusterClient.Core.Model;

namespace Vostok.ClusterClient.Core.Ordering.Weighed.Leadership
{
    /// <summary>
    /// Represents a leader result detector which accepts any result with <see cref="ResponseVerdict.Accept"/> verdict or <see cref="ResponseCode.ServiceUnavailable"/> response code.
    /// </summary>
    public class AcceptedVerdictOr503LeaderDetector : ILeaderResultDetector
    {
        /// <inheritdoc />
        public bool IsLeaderResult(ReplicaResult result) =>
            result.Verdict == ResponseVerdict.Accept || result.Response.Code == ResponseCode.ServiceUnavailable;
    }
}