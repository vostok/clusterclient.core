using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Model
{
    public static class RequestMiscExtensions
    {
        public static bool ContainsAlreadyUsedStream(this Request request)
        {
            return request.StreamContent is SingleUseStreamContent streamContent && streamContent.WasUsed;
        }

        public static bool ContainsAlreadyUsedContent(this Request request)
        {
            return request.ContentProducer is UserContentProducerWrapper contentProducer && contentProducer.WasUsed;
        }

        internal static void LogRequestDataIsUsed()
        {
            LogProvider.Get().Info("Request data is already used and can not be reused");
        }
    }
}