using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Model
{
    /// <summary>
    /// Represents an error thrown when <see cref="SingleUseStreamContent"/> is attempted to be used more than once.
    /// </summary>
    [PublicAPI]
    public class StreamAlreadyUsedException : ClusterClientException
    {
        /// <inheritdoc />
        public StreamAlreadyUsedException(string message)
            : base(message)
        {
        }
    }
}