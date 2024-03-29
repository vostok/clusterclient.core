﻿using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Adaptive
{
    /// <summary>
    /// <para>Represents a tuning policy which selects action based on replica's response verdict:</para>
    /// <list type="bullet">
    /// <item><description><see cref="ResponseVerdict.Accept"/> verdict leads to <see cref="AdaptiveHealthAction.Increase"/> of replica health.</description></item>
    /// <item><description><see cref="ResponseVerdict.Reject"/> verdict leads to <see cref="AdaptiveHealthAction.Decrease"/> of replica health.</description></item>
    /// </list>
    /// </summary>
    [PublicAPI]
    public class ResponseVerdictTuningPolicy : IAdaptiveHealthTuningPolicy
    {
        /// <inheritdoc />
        public AdaptiveHealthAction SelectAction(ReplicaResult result)
        {
            if (result.Response.Code == ResponseCode.StreamReuseFailure ||
                result.Response.Code == ResponseCode.StreamInputFailure ||
                result.Response.Code == ResponseCode.ContentInputFailure ||
                result.Response.Code == ResponseCode.ContentReuseFailure)
                return AdaptiveHealthAction.DontTouch;

            switch (result.Verdict)
            {
                case ResponseVerdict.Accept:
                    return AdaptiveHealthAction.Increase;

                case ResponseVerdict.Reject:
                    return AdaptiveHealthAction.Decrease;

                default:
                    return AdaptiveHealthAction.DontTouch;
            }
        }
    }
}