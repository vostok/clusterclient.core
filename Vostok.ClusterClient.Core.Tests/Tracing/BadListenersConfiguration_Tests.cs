#if NET6_0_OR_GREATER
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Strategies;
using Vostok.Clusterclient.Core.Tracing;
using Vostok.Clusterclient.Core.Transport;

namespace Vostok.Clusterclient.Core.Tests.Tracing;

internal sealed class BadListenersConfiguration_Tests
{
    private TracingModule module;
    private TracingTransport transport;

    private IRequestContext context;
    private Request request;

    [SetUp]
    public void TestSetup()
    {
        module = new TracingModule(new TracingOptions());

        request = Request.Get("foo/bar");
        context = Substitute.For<IRequestContext>();
        context.Request.Returns(_ => request);
        context.Parameters.Returns(RequestParameters.Empty.WithStrategy(new ParallelRequestStrategy(2)));

        transport = new TracingTransport(Substitute.For<ITransport>(), new TracingOptions());
    }

    [Test]
    public void Should_not_create_activity_when_no_listeners()
    {
        Activity recordedActivity = null;
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name == "NotExistingSource",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => recordedActivity = activity
        });

        RunModule();
        recordedActivity.Should().BeNull();

        RunTransport();
        recordedActivity.Should().BeNull();
    }

    [Test]
    public void Should_not_create_activity_when_not_sampled()
    {
        Activity recordedActivity = null;
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name == Instrumentation.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.None,
            ActivityStopped = activity => recordedActivity = activity
        });

        RunModule();
        recordedActivity.Should().BeNull();

        RunTransport();
        recordedActivity.Should().BeNull();
    }

    private void RunModule() => module
        .ExecuteAsync(
            context,
            _ => Task.FromResult(
                new ClusterResult(
                    ClusterResultStatus.Success,
                    new List<ReplicaResult>(),
                    Responses.Ok,
                    Request.Get(""))))
        .GetAwaiter()
        .GetResult();

    private void RunTransport() => transport
        .SendAsync(request, null, 5.Seconds(), CancellationToken.None)
        .GetAwaiter()
        .GetResult();
}
#endif