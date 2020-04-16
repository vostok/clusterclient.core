using System.Collections.Generic;
using System.Linq;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Strategies.WaitAnotherReplicaResultProvider
{
    public class CompositeWaitAnotherResultProvider : IWaitAnotherResultProvider
    {
        private readonly ICollection<IWaitAnotherResultProvider> providers; 

        public CompositeWaitAnotherResultProvider(params IWaitAnotherResultProvider[] providers)
        {
            this.providers = providers;
        }

        public bool NeedWaitAnotherResult(ReplicaResult result)
        {
            return providers.Any(p => p.NeedWaitAnotherResult(result));
        }
    }
}