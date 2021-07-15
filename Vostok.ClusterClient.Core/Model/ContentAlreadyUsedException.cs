using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Model
{
    /// <summary>
    /// Represents an error thrown when <see cref="ReusableContentProducer"/> used twice.
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