using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Criteria
{
    /// <summary>
    /// Represents a criterion which rejects responses with <see cref="ResponseCode.ContentInputFailure"/> and <see cref="ResponseCode.ContentReuseFailure"/> code.
    /// </summary>
    [PublicAPI]
    public class RejectContentErrorsCriterion : IResponseCriterion
    {
        /// <inheritdoc />
        public ResponseVerdict Decide(Response response)
        {
            switch (response.Code)
            {
                case ResponseCode.ContentReuseFailure:
                case ResponseCode.ContentInputFailure:
                    return ResponseVerdict.Reject;

                default:
                    return ResponseVerdict.DontKnow;
            }
        }
    }
}