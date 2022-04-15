using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Misc
{
    [PublicAPI]
    public enum LoggingMode
    {
        Detailed,
        SingleShortMessage,
        SingleVerboseMessage
    }
}