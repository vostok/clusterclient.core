using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Strategies.WaitAnotherReplicaResultProvider
{
    internal class FixedWaitAnotherResultProvider : IWaitAnotherResultProvider
    {
        private readonly bool wait;

        public FixedWaitAnotherResultProvider(bool wait)
        {
            this.wait = wait;
        }

        public bool NeedWaitAnotherResult(ReplicaResult result) => wait;
    }
}