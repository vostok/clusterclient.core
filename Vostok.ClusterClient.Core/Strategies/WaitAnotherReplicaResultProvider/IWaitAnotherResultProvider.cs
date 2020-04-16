using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Strategies.WaitAnotherReplicaResultProvider
{
    public interface IWaitAnotherResultProvider
    {
        bool NeedWaitAnotherResult(ReplicaResult result);
    }
}