#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using OpenTelemetry;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Strategies;
using Vostok.Clusterclient.Core.Tracing;
using Vostok.Commons.Helpers.Url;
using Vostok.Tracing.Abstractions;

namespace Vostok.Clusterclient.Core.Tests.Modules;

internal class TracingModule_Tests
{
    private IRequestContext context;

    private string targetService;
    private string targetEnvironment;
    private Request request;
    private Response response;

    private TracingModule module;

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

        module = new TracingModule(new TracingOptions())
        {
            TargetServiceProvider = () => targetService,
            TargetEnvironmentProvider = () => targetEnvironment
        };

        targetService = Guid.NewGuid().ToString();
        targetEnvironment = Guid.NewGuid().ToString();

        request = Request.Get("foo/bar");
        response = Responses.Ok;

        context = Substitute.For<IRequestContext>();
        context.Request.Returns(_ => request);
        context.Parameters.Returns(RequestParameters.Empty.WithStrategy(new ParallelRequestStrategy(2)));
    }

    [Test]
    public void Should_fill_basic_span_data()
    {
        Run();

        recordedActivity.Should().NotBeNull();

        recordedActivity.Kind.Should().Be(ActivityKind.Client);
        recordedActivity.Source.Name.Should().Be(Instrumentation.ActivitySourceName);
        recordedActivity.OperationName.Should().Be(Instrumentation.ClusterSpanInitialName);
        recordedActivity.Context.IsValid().Should().BeTrue();
        recordedActivity.IsStopped.Should().BeTrue();
        recordedActivity.Status.Should().Be(ActivityStatusCode.Unset);
        recordedActivity.StatusDescription.Should().BeNull();
    }

    [TestCase(RequestMethods.Get, "bar/baz", "GET bar/baz")]
    [TestCase(RequestMethods.Post, "foo/pupa?query=value", "POST foo/pupa")]
    [TestCase(RequestMethods.Put, "baz/lupa", "PUT baz/lupa")]
    public void Should_fill_span_name(string method, string url, string expectedName)
    {
        request = new Request(method, new Uri(url, UriKind.Relative));

        Run();

        recordedActivity.DisplayName.Should().Be(expectedName);
    }

    [Test]
    public void Should_fill_cluster_request_attributes()
    {
        var result = Run();

        recordedActivity.GetTagItem(SemanticConventions.AttributeClusterRequest).Should().Be(true);
        recordedActivity.GetTagItem(SemanticConventions.AttributeRequestStrategy).Should().Be(context.Parameters.Strategy!.ToString());
        recordedActivity.GetTagItem(SemanticConventions.AttributeClusterStatus).Should().Be(result.Status.ToString());

        recordedActivity.GetTagItem(WellKnownAnnotations.Http.Request.TargetService).Should().Be(targetService);
        recordedActivity.GetTagItem(WellKnownAnnotations.Http.Request.TargetEnvironment).Should().Be(targetEnvironment);

        recordedActivity.GetTagItem(SemanticConventions.AttributeHttpRequestMethod).Should().Be(request.Method);
        recordedActivity.GetTagItem(SemanticConventions.AttributeUrlFull).Should().Be(request.Url.ToStringWithoutQuery());

        recordedActivity.GetTagItem(SemanticConventions.AttributeServerAddress).Should().BeNull();
        recordedActivity.GetTagItem(SemanticConventions.AttributeServerPort).Should().BeNull();
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
    [TestCase(ResponseCode.InternalServerError)]
    [TestCase(ResponseCode.RequestTimeout)]
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
        var result = Run();
        result.Response.Stream.CopyTo(new MemoryStream());
        result.Dispose();

        recordedActivity.GetTagItem(SemanticConventions.AttributeStreaming).Should().Be(true);
        recordedActivity.IsStopped.Should().BeTrue();
        // TODO(kungurtsev): handle case when result.Response is not ProxyStream.
        // CheckBodySizeAttribute(bodySize);

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
    public void Should_enrich_data_from_settings()
    {
        const string header1 = "header1";
        const string tag = "Biba";
        const string requestTag = "request_tag";
        const string resultTag = "response_tag";

        request = request.WithHeader(header1, tag);
        response = response.WithHeader(header1, tag);

        var options = new TracingOptions
        {
            EnrichWithRequest = (activity, req) => activity.SetTag(requestTag, req.Headers![header1]),
            EnrichWithClusterResult = (activity, res) => activity.SetTag(resultTag, res.ReplicaResults.Count)
        };
        module = new TracingModule(options);

        var result = Run();

        recordedActivity.GetTagItem(requestTag).Should().Be(tag);
        recordedActivity.GetTagItem(resultTag).Should().Be(result.ReplicaResults.Count);
    }

    private ClusterResult Run() => module
        .ExecuteAsync(
            context,
            _ => Task.FromResult(
                new ClusterResult(
                    ClusterResultStatus.Success,
                    new List<ReplicaResult> {new(new Uri("http://google.com"), response, ResponseVerdict.Accept, 1.Seconds())},
                    response,
                    request)))
        .GetAwaiter()
        .GetResult();
}
#endif