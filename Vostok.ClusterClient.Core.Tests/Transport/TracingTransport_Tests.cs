#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Tracing;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Commons.Helpers.Url;
using Vostok.Tracing.Abstractions;

namespace Vostok.Clusterclient.Core.Tests.Transport;

internal class TracingTransport_Tests
{
    private ITransport baseTransport;

    private string targetService;
    private string targetEnvironment;
    private Request request;
    private Response response;

    private TracingTransport transport;

    private Activity recordedActivity;

    [SetUp]
    public void TestSetup()
    {
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name == Instrumentation.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => recordedActivity = activity
        });

        baseTransport = Substitute.For<ITransport>();
        baseTransport.SendAsync(default, default, default, default).ReturnsForAnyArgs(_ => response);

        transport = new TracingTransport(baseTransport, new TracingOptions())
        {
            TargetServiceProvider = () => targetService,
            TargetEnvironmentProvider = () => targetEnvironment
        };

        targetService = Guid.NewGuid().ToString();
        targetEnvironment = Guid.NewGuid().ToString();

        request = Request.Get("http://my-host:3535/foo/bar");
        response = Responses.Ok;
    }

    [Test]
    public void Should_fill_basic_span_data()
    {
        Run();

        recordedActivity.Should().NotBeNull();

        recordedActivity.Kind.Should().Be(ActivityKind.Client);
        recordedActivity.Source.Name.Should().Be(Instrumentation.ActivitySourceName);
        recordedActivity.OperationName.Should().Be(Instrumentation.ClientSpanInitialName);
        recordedActivity.Context.IsValid().Should().BeTrue();
        recordedActivity.IsStopped.Should().BeTrue();
        recordedActivity.Status.Should().Be(ActivityStatusCode.Unset);
        recordedActivity.StatusDescription.Should().BeNull();
    }

    [TestCase(RequestMethods.Get, "http://my-host/bar/baz", "GET bar/baz")]
    [TestCase(RequestMethods.Post, "http://my-host/foo/pupa?query=value", "POST foo/pupa")]
    [TestCase(RequestMethods.Put, "http://my-host/baz/lupa", "PUT baz/lupa")]
    public void Should_fill_span_name(string method, string url, string expectedName)
    {
        request = new Request(method, new Uri(url, UriKind.Absolute));

        Run();

        recordedActivity.DisplayName.Should().Be(expectedName);
    }

    [Test]
    public void Should_fill_client_request_attributes()
    {
        Run();

        recordedActivity.GetTagItem(SemanticConventions.AttributeClusterRequest).Should().BeNull();
        recordedActivity.GetTagItem(SemanticConventions.AttributeRequestStrategy).Should().BeNull();

        recordedActivity.GetTagItem(WellKnownAnnotations.Http.Request.TargetService).Should().Be(targetService);
        recordedActivity.GetTagItem(WellKnownAnnotations.Http.Request.TargetEnvironment).Should().Be(targetEnvironment);

        recordedActivity.GetTagItem(SemanticConventions.AttributeHttpRequestMethod).Should().Be(request.Method);
        recordedActivity.GetTagItem(SemanticConventions.AttributeUrlFull).Should().Be(request.Url.ToStringWithoutQuery());

        recordedActivity.GetTagItem(SemanticConventions.AttributeServerAddress).Should().Be("my-host");
        recordedActivity.GetTagItem(SemanticConventions.AttributeServerPort).Should().Be(3535);
    }

    [Test]
    public void Should_not_fill_target_attributes_if_absent()
    {
        targetService = targetEnvironment = null;

        Run();

        recordedActivity.GetTagItem(WellKnownAnnotations.Http.Request.TargetService).Should().BeNull();
        recordedActivity.GetTagItem(WellKnownAnnotations.Http.Request.TargetEnvironment).Should().BeNull();
    }

    [TestCase(ResponseCode.Ok)]
    [TestCase(ResponseCode.BadRequest)]
    [TestCase(ResponseCode.InternalServerError)]
    public void Should_fill_status_code_attribute(ResponseCode expectedResponseCode)
    {
        response = new Response(expectedResponseCode);

        Run();

        recordedActivity.GetTagItem(SemanticConventions.AttributeHttpResponseStatusCode).Should().Be((int)expectedResponseCode);
    }

    [TestCase(ResponseCode.Ok, ActivityStatusCode.Unset)]
    [TestCase(ResponseCode.BadRequest, ActivityStatusCode.Error)]
    [TestCase(ResponseCode.InternalServerError, ActivityStatusCode.Error)]
    public void Should_fill_activity_status_when_error_result(
        ResponseCode responseCode,
        ActivityStatusCode expectedActivityStatus)
    {
        response = new Response(responseCode);

        Run();

        recordedActivity.Status.Should().Be(expectedActivityStatus);
        recordedActivity.StatusDescription.Should().BeNull();
    }

    [Test]
    public void Should_record_request_body_size_attribute()
    {
        Run();
        CheckBodySizeAttribute(null);

        const long bodySize = 15;

        request = request.WithContent(new Content(new byte[bodySize]));
        Run();
        CheckBodySizeAttribute(bodySize);

        request = request.WithContent(new CompositeContent(new[] {new Content(new byte[5]), new Content(new byte[10])}));
        Run();
        CheckBodySizeAttribute(bodySize);

        request = request.WithContent(new StreamContent(new MemoryStream(new byte[bodySize]), bodySize));
        Run();
        CheckBodySizeAttribute(bodySize);

        void CheckBodySizeAttribute(long? size) =>
            recordedActivity.GetTagItem(WellKnownAnnotations.Http.Request.Size).Should().Be(size);
    }

    [Test]
    public void Should_record_response_body_size_attribute()
    {
        Run();
        CheckBodySizeAttribute(null);

        const long bodySize = 123L;
        var content = new byte[bodySize];

        response = response.WithContent(content);
        Run();

        CheckBodySizeAttribute(bodySize);

        response = Responses.Ok.WithStream(new MemoryStream(content));
        response = Run();
        response.Stream.CopyTo(new MemoryStream());
        response.Dispose();

        CheckBodySizeAttribute(bodySize);
        recordedActivity.GetTagItem(SemanticConventions.AttributeStreaming).Should().Be(true);
        recordedActivity.IsStopped.Should().BeTrue();

        void CheckBodySizeAttribute(long? size) =>
            recordedActivity.GetTagItem(WellKnownAnnotations.Http.Response.Size).Should().Be(size);
    }

    [Test]
    public void Should_dispose_underlying_stream()
    {
        var stream = Substitute.For<Stream>();
        response = response.WithStream(stream);

        var result = Run();

        result.Dispose();

        stream.Received().Dispose();
    }

    [Test]
    public void Should_propagate_context_and_baggage_when_propagator_configured()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        // note (ponomaryovigor, 17.10.2024): Set propagators via reflection in order not to install full OTel SDK. 
        var propagator = new CompositeTextMapPropagator(new TextMapPropagator[]
        {
            new TraceContextPropagator(),
            new BaggagePropagator()
        });
        typeof(Propagators).GetProperty(nameof(Propagators.DefaultTextMapPropagator))!
            .SetValue(Propagators.DefaultTextMapPropagator, propagator);

        Baggage.SetBaggage("TestProperty", "TestValue");

        Run();

        var requestArgument = Arg.Is<Request>(r =>
            r.Headers["traceparent"].Contains(recordedActivity.Context.TraceId.ToHexString()) &&
            r.Headers["baggage"].Contains("TestProperty"));

        baseTransport.Received(1)
            .SendAsync(requestArgument, Arg.Any<TimeSpan?>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .GetAwaiter()
            .GetResult();
    }

    [Test]
    public void Should_enrich_data_from_settings()
    {
        const string header1 = "header1";
        const string header2 = "header2";
        const string tag = "Biba";
        const string requestTag = "request_tag";
        const string responseTag = "response_tag";

        request = request.WithHeader(header1, tag);
        response = response.WithHeader(header1, tag);

        var configuration = new TracingOptions
        {
            AdditionalRequestTransformation = (req, context) => req.WithHeader(header2, context.TraceId),
            EnrichWithRequest = (activity, req) => activity.SetTag(requestTag, req.Headers![header1]),
            EnrichWithResponse = (activity, res) => activity.SetTag(responseTag, res.Headers[header1])
        };
        transport = new TracingTransport(baseTransport, configuration);

        Run();

        var requestArgument = Arg.Is<Request>(r => r.Headers[header2] == recordedActivity.TraceId.ToHexString());
        baseTransport.Received(1)
            .SendAsync(requestArgument, Arg.Any<TimeSpan?>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .GetAwaiter()
            .GetResult();

        recordedActivity.GetTagItem(requestTag).Should().Be(tag);
        recordedActivity.GetTagItem(responseTag).Should().Be(tag);
    }

    private Response Run() => transport
        .SendAsync(request, null, 5.Seconds(), CancellationToken.None)
        .GetAwaiter()
        .GetResult();
}
#endif