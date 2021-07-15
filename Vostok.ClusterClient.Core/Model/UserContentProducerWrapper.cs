using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Clusterclient.Core.Model
{
    internal class UserContentProducerWrapper : IContentProducer
    {
        internal IContentProducer producer;

        public UserContentProducerWrapper(IContentProducer producer)
        {
            this.producer = producer;
            IsReusable = producer.IsReusable;
        }

        public bool WasUsed => producer == null;

        public bool IsReusable { get; }

        public long? Length => producer?.Length;

        public Task ProduceAsync(Stream requestStream, CancellationToken cancellationToken)
        {
            var producerToReturn = producer;

            if (!IsReusable)
            {
                producerToReturn = Interlocked.Exchange(ref producer, null);
                if (producerToReturn == null)
                    throw new ContentAlreadyUsedException($"Detected an attempt to use request body content producer twice, which is not allowed because {nameof(IContentProducer.IsReusable)} set to false.");
            }

            return producerToReturn.ProduceAsync(requestStream, cancellationToken);
        }
    }
}