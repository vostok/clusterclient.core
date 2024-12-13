#if NET6_0_OR_GREATER
using System.Diagnostics;

namespace Vostok.Clusterclient.Core.Tracing;

internal static class Instrumentation
{
    public const string ActivitySourceName = "Vostok.ClusterClient";
    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName, typeof(Instrumentation).Assembly.GetName().Version?.ToString());

    public const string ClusterSpanInitialName = "Vostok.ClusterClient.ClusterRequestOut";
    public const string ClientSpanInitialName = "Vostok.ClusterClient.ClientRequestOut";
}
#endif