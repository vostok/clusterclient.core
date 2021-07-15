using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Model
{
    /// <summary>
    /// Represents an error thrown when <see cref="UserContentProducerWrapper"/> is attempted to be used more than once.
    /// </summary>
    [PublicAPI]
    public class ContentAlreadyUsedException : ClusterClientException
    {
        /// <inheritdoc />
        public ContentAlreadyUsedException(string message)
            : base(message)
        {
        }
    }
}