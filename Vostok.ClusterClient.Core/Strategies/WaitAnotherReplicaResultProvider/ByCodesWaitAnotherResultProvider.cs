using System.Collections.Generic;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Strategies.WaitAnotherReplicaResultProvider
{
    public class ByCodesWaitAnotherResultProvider : IWaitAnotherResultProvider
    {
        private readonly IList<ResponseCode> codes;

        public ByCodesWaitAnotherResultProvider(params ResponseCode[] codes)
        {
            this.codes = codes;
        }

        public bool NeedWaitAnotherResult(ReplicaResult result)
        {
            return codes.Contains(result.Response.Code);
        }
    }
}