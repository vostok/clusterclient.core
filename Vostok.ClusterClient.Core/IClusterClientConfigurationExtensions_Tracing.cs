#if NET6_0_OR_GREATER
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Transport;

namespace Vostok.Clusterclient.Core;

public static partial class IClusterClientConfigurationExtensions
{
    internal static void SetupDistributedTracing(this IClusterClientConfiguration configuration)
    {
        if (!configuration.Tracing.Enabled)
            return;

        var tracingModule = new TracingModule(configuration.Tracing)
        {
            TargetServiceProvider = () => configuration.TargetServiceName,
            TargetEnvironmentProvider = () => configuration.TargetEnvironment
        };

        configuration.AddRequestModule(tracingModule, RequestModule.ResponseTransformation);

        configuration.Transport = new TracingTransport(configuration.Transport, configuration.Tracing)
        {
            TargetServiceProvider = () => configuration.TargetServiceName,
            TargetEnvironmentProvider = () => configuration.TargetEnvironment
        };
    }
}
#endif