using System;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;

namespace Vostok.Clusterclient.Core.Sending
{
    internal class ContextualRequestSender : IRequestSender
    {
        private readonly IRequestSenderInternal sender;
        private readonly RequestContext context;

        public ContextualRequestSender(IRequestSenderInternal sender, RequestContext context)
        {
            this.sender = sender;
            this.context = context;
        }

        public async Task<ReplicaResult> SendToReplicaAsync(Uri replica, Request request, TimeSpan? connectionTimeout, TimeSpan timeout, CancellationToken cancellationToken)
        {
            context.SetUnknownResult(replica);

            ReplicaResult result;

            try
            {
                result = await sender.SendToReplicaAsync(
                        context.Transport,
                        context.ReplicaOrdering,
                        replica,
                        request,
                        context.ConnectionAttempts,
                        connectionTimeout,
                        timeout,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                result = CreateCanceledResult(replica);
            }

            context.SetReplicaResult(result);

            return result;
        }

        private static ReplicaResult CreateCanceledResult(Uri replica) =>
            new ReplicaResult(replica, Responses.Canceled, ResponseVerdict.DontKnow, TimeSpan.Zero);
    }
}