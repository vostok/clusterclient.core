using System;

namespace Vostok.Clusterclient.Core.Misc;

internal static class ClusterClientConstants
{
    //(deniaa): We can't use "null" as connection time budget because of a bug in Net6.
    // Also we can't use TimeBudget.Remaining as connection time budget for the last attempt in strategies
    // because of HttpMessageHandler cache in Vostok.Clusterclient.Transport.Sockets.SocketsHandlerProvider.
    // Connection timeout is a key in this cache and we don't want to create a new HttpMessageHandler for each request.
    // So we want to have only one constant "Infinite" for the whole ClusterClient.
    public static TimeSpan LastAttemptConnectionTimeBudget = TimeSpan.FromSeconds(7);
}