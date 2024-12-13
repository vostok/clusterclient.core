#if NET6_0_OR_GREATER
namespace Vostok.Clusterclient.Core.Tracing;

internal static class SemanticConventions
{
    // note (ponomaryovigor, 15.10.2024): Attributes copied manually from OTel registry: https://opentelemetry.io/docs/specs/semconv/attributes-registry/.
    // Replace with constants from SemanticConventions package when it will be released.
    public const string AttributeHttpRequestMethod = "http.request.method";
    public const string AttributeHttpResponseStatusCode = "http.response.status_code";
    public const string AttributeUrlFull = "url.full";
    public const string AttributeServerAddress = "server.address";
    public const string AttributeServerPort = "server.port";

    // ClusterClient related attributes.
    private const string ClusterClientPrefix = "vostok.clusterclient.";

    public const string AttributeClusterRequest = ClusterClientPrefix + "request.is_cluster";
    public const string AttributeRequestStrategy = ClusterClientPrefix + "request.strategy";
    public const string AttributeStreaming = ClusterClientPrefix + "response.is_streaming";
    public const string AttributeClusterStatus = ClusterClientPrefix + "response.cluster_status";

    // todo (ponomaryovigor, 21.10.2024):
    // Replace Vostok WellKnownAnnotations.Http.Request.Size and WellKnownAnnotations.Http.Response.Size
    // with OTel `http.request.body.size` and `http.response.body.size` when it will be stabilized
    //
    // Replace Vostok WellKnownAnnotations.Http.Request.TargetService and WellKnownAnnotations.Http.Request.TargetEnvironment
    // with something without OTel "http" namespace
}

#endif