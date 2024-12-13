#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Tracing;

namespace Vostok.Clusterclient.Core.Transport;

internal sealed class TracingTransport(ITransport transport, TracingOptions options) : ITransport
{
    private readonly ITransport transport = transport ?? throw new ArgumentNullException(nameof(transport));
    private readonly TracingOptions options = options ?? throw new ArgumentNullException(nameof(options));

    public TransportCapabilities Capabilities => transport.Capabilities;

    [CanBeNull]
    public Func<string> TargetServiceProvider { get; set; }

    [CanBeNull]
    public Func<string> TargetEnvironmentProvider { get; set; }

    public async Task<Response> SendAsync(
        Request request,
        TimeSpan? connectionTimeout,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var activity = Instrumentation.ActivitySource.StartActivity(Instrumentation.ClientSpanInitialName, ActivityKind.Client);

        if (activity?.IsAllDataRequested is true)
            request = FillRequestAttributes(activity, request);

        request = PropagateContext(activity, request);

        Response response;

        // note: Disable activity creation for underlying transports.
        using (SuppressInstrumentationScope.Begin())
        {
            response = await transport.SendAsync(request, connectionTimeout, timeout, cancellationToken)
                                      .ConfigureAwait(false);
        }

        if (activity?.IsAllDataRequested is true)
            response = FillResponseAttributes(activity, response);
        else
            activity?.Dispose();

        return response;
    }

    private Request FillRequestAttributes(Activity activity, Request request)
    {
        activity.FillRequestAttributes(request, TargetServiceProvider, TargetEnvironmentProvider);

        if (options.AdditionalRequestTransformation is not null)
            request = options.AdditionalRequestTransformation(request, activity.Context);

        options.EnrichWithRequest?.Invoke(activity, request);

        return request;
    }

    private Response FillResponseAttributes(Activity activity, Response response)
    {
        options.EnrichWithResponse?.Invoke(activity, response);

        return activity.FillResponseAttributes(response);
    }

    private static Request PropagateContext(Activity activity, Request request)
    {
        var contextToPropagate = activity?.Context ?? Activity.Current?.Context;
        if (!contextToPropagate.HasValue)
            return request;

        var propagator = Propagators.DefaultTextMapPropagator;

        propagator.Inject(new PropagationContext(contextToPropagate.Value, Baggage.Current),
            string.Empty,
            (_, key, value) =>
                request = request.WithHeader(key, value));

        return request;
    }
}
#endif