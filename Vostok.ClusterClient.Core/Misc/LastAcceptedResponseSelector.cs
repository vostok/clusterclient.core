using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Misc
{
    /// <summary>
    /// Represents a response selector which works using following priority system:
    /// <list type="number">
    /// <item><description>If there are no results at all, it returns <c>null</c>.</description></item>
    /// <item><description>If there are any results with <see cref="ResponseVerdict.Accept"/> verdict, it returns the last of them.
    /// Results without <see cref="HeaderNames.UnreliableResponse"/> header will be more preferable than with it.</description></item>
    /// <item><description>If there are any results with response code other than <see cref="ResponseCode.Unknown"/>, it returns the last of them.</description></item>
    /// <item><description>As a last resort, it just returns response of last result in the list.</description></item>
    /// </list>
    /// </summary>
    [PublicAPI]
    public class LastAcceptedResponseSelector : IResponseSelector
    {
        /// <inheritdoc />
        public Response Select(Request request, RequestParameters parameters, IList<ReplicaResult> results) =>
            GetLastAcceptedResponse(results) ?? GetLastKnownResponse(results) ?? GetLastResponse(results);

        private static Response GetLastAcceptedResponse(IList<ReplicaResult> results) =>
            results.LastOrDefault(result => result.Verdict == ResponseVerdict.Accept && result.Response.Headers[HeaderNames.UnreliableResponse] == null)?.Response ??
            results.LastOrDefault(result => result.Verdict == ResponseVerdict.Accept)?.Response;

        private static Response GetLastKnownResponse(IList<ReplicaResult> results) =>
            results.LastOrDefault(
                    result => result.Response.Code != ResponseCode.Unknown &&
                              result.Response.Code != ResponseCode.StreamReuseFailure &&
                              result.Response.Code != ResponseCode.ContentReuseFailure)
                ?.Response;

        private static Response GetLastResponse(IList<ReplicaResult> results) =>
            results.LastOrDefault()?.Response;
    }
}