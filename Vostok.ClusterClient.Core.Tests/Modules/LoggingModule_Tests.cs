using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Strategies;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.Logging.Formatting;

namespace Vostok.Clusterclient.Core.Tests.Modules
{
    [TestFixture]
    internal class LoggingModule_Tests
    {
        private Request request;
        private Response response;

        private IRequestContext context;
        private ILog logMock;
        private LoggingModule module;
        private LoggingOptions loggingOptions;

        private List<string> logMessages;

        [SetUp]
        public void TestSetup()
        {
            context = Substitute.For<IRequestContext>();
            logMock = Substitute.For<ILog>();
            var log = new CompositeLog(
                new ConsoleLog(),
                logMock
            );
            context.Log.Returns(log);
            context.Parameters.Returns(RequestParameters.Empty.WithStrategy(new SingleReplicaRequestStrategy()));
            context.Budget.Returns(RequestTimeBudget.Infinite);

            logMessages = new List<string>();

            logMock.IsEnabledFor(default).ReturnsForAnyArgs(true);
            logMock.WhenForAnyArgs(x => x.Log(default(LogEvent)))
                .Do(x =>
                    logMessages.Add(
                        LogEventFormatter.Format(x.Arg<LogEvent>(), OutputTemplate.Default))
                );

            loggingOptions = new LoggingOptions();
            module = new LoggingModule(loggingOptions, "target_service");

            request = CreateRequest("?q1=qv1&q2=qv2", Headers.Empty.Set("h1", "hv1").Set("h2", "hv2"));
            response = CreateResponse(headers: Headers.Empty.Set("h3", "hv3").Set("h4", "hv4"));
        }

        [Test]
        public void Should_not_log_request_details_query_and_headers_by_default_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("Sending request 'POST http://foo/bar' to 'target_service'. Timeout = infinite. Strategy = 'SingleReplica'."));
        }

        [Test]
        public void Should_log_request_details_query_and_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogRequestHeaders = true;

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("Sending request 'POST http://foo/bar?q1=qv1&q2=qv2 Headers: (h1=hv1, h2=hv2)' to 'target_service'. Timeout = infinite. Strategy = 'SingleReplica'."));
        }

        [Test]
        public void Should_log_request_details_query_and_empty_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogRequestHeaders = true;
            request = CreateRequest("?a=b", Headers.Empty);

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("Sending request 'POST http://foo/bar?a=b' to 'target_service'. Timeout = infinite. Strategy = 'SingleReplica'."));
        }

        [Test]
        public void Should_log_request_details_query_and_null_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogRequestHeaders = true;
            request = CreateRequest("?a=b", null);

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("Sending request 'POST http://foo/bar?a=b' to 'target_service'. Timeout = infinite. Strategy = 'SingleReplica'."));
        }

        [Test]
        public void Should_log_request_details_empty_query_and_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogRequestHeaders = true;
            request = CreateRequest(null, Headers.Empty.Set("h1", "v1"));

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("Sending request 'POST http://foo/bar Headers: (h1=v1)' to 'target_service'. Timeout = infinite. Strategy = 'SingleReplica'."));
        }

        [Test]
        public void Should_not_log_successful_replica_response_headers_by_default_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("Success. Response code = 200 ('Ok'). Time = 00:00:00."));
        }

        [Test]
        public void Should_log_successful_replica_response_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogResponseHeaders = true;

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("Success. Response code = 200 ('Ok') Headers: (h3=hv3, h4=hv4). Time = 00:00:00."));
        }

        [Test]
        public void Should_log_successful_replica_response_empty_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogResponseHeaders = true;
            response = CreateResponse(headers: Headers.Empty);

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("Success. Response code = 200 ('Ok'). Time = 00:00:00."));
        }

        [Test]
        public void Should_not_log_failed_cluster_response_headers_by_default_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            response = CreateResponse(ResponseCode.Forbidden, Headers.Empty.Set("h3", "hv3").Set("h4", "hv4"));

            Execute(ClusterResultStatus.Throttled);

            logMessages.Should().ContainSingle(x => x.Contains("Request 'POST http://foo/bar' to 'target_service' has failed with status 'Throttled'. Response code = 403 ('Forbidden'). Time = 00:00:00."));
        }

        [Test]
        public void Should_log_failed_cluster_response_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogResponseHeaders = true;
            response = CreateResponse(ResponseCode.Forbidden, Headers.Empty.Set("h3", "hv3").Set("h4", "hv4"));

            Execute(ClusterResultStatus.Throttled);

            logMessages.Should().ContainSingle(x => x.Contains("Request 'POST http://foo/bar' to 'target_service' has failed with status 'Throttled'. Response code = 403 ('Forbidden') Headers: (h3=hv3, h4=hv4). Time = 00:00:00."));
        }

        [Test]
        public void Should_log_failed_cluster_response_empty_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogResponseHeaders = true;
            response = CreateResponse(ResponseCode.Forbidden, Headers.Empty);

            Execute(ClusterResultStatus.Throttled);

            logMessages.Should().ContainSingle(x => x.Contains("Request 'POST http://foo/bar' to 'target_service' has failed with status 'Throttled'. Response code = 403 ('Forbidden'). Time = 00:00:00."));
        }

        [Test]
        public void Should_not_log_failed_cluster_request_query_and_headers_by_default_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            response = CreateResponse(ResponseCode.Forbidden, Headers.Empty);

            Execute(ClusterResultStatus.Throttled);

            logMessages.Should().ContainSingle(x => x.Contains("Request 'POST http://foo/bar' to 'target_service' has failed with status 'Throttled'. Response code = 403 ('Forbidden'). Time = 00:00:00."));
        }

        [Test]
        public void Should_log_failed_request_query_and_headers_for_detailed_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.Detailed;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogResponseHeaders = true;
            loggingOptions.LogRequestHeaders = true;
            response = CreateResponse(ResponseCode.Forbidden, Headers.Empty.Set("h3", "hv3").Set("h4", "hv4"));

            Execute(ClusterResultStatus.Throttled);

            logMessages.Should().ContainSingle(x => x.Contains("Request 'POST http://foo/bar?q1=qv1&q2=qv2 Headers: (h1=hv1, h2=hv2)' to 'target_service' has failed with status 'Throttled'. Response code = 403 ('Forbidden') Headers: (h3=hv3, h4=hv4). Time = 00:00:00."));
        }

        [Test]
        public void Should_not_log_query_and_headers_for_short_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleShortMessage;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogResponseHeaders = true;
            loggingOptions.LogRequestHeaders = true;

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar' to 'target_service'. Code = 200. Time = 00:00:00."));
        }

        [Test]
        public void Should_not_log_query_and_headers_by_default_for_verbose_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\"}]"));
        }

        [Test]
        public void Should_log_query_and_headers_for_verbose_mode()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogResponseHeaders = true;
            loggingOptions.LogRequestHeaders = true;

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar?q1=qv1&q2=qv2 Headers: (h1=hv1, h2=hv2)' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\", \"ResponseHeaders\": \"h3=hv3, h4=hv4\"}]"));
        }

        [Test]
        public void Should_apply_request_query_whitelist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogQueryString.Whitelist = new[] {"q2"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar?q2=qv2' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\"}]"));
        }

        [Test]
        public void Should_apply_request_query_blacklist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogQueryString.Blacklist = new[] {"q1"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar?q2=qv2' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\"}]"));
        }

        [Test]
        public void Should_prefer_request_query_whitelist_before_blacklist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogQueryString = true;
            loggingOptions.LogQueryString.Whitelist = new[] {"q1", "q2"};
            loggingOptions.LogQueryString.Blacklist = new[] {"q1"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar?q1=qv1&q2=qv2' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\"}]"));
        }

        [Test]
        public void Should_apply_request_headers_whitelist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogRequestHeaders = true;
            loggingOptions.LogRequestHeaders.Whitelist = new[] {"h1"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar Headers: (h1=hv1)' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\"}]"));
        }

        [Test]
        public void Should_apply_request_headers_blacklist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogRequestHeaders = true;
            loggingOptions.LogRequestHeaders.Blacklist = new[] {"h2"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar Headers: (h1=hv1)' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\"}]"));
        }

        [Test]
        public void Should_prefer_request_headers_whitelist_before_blacklist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogRequestHeaders = true;
            loggingOptions.LogRequestHeaders.Whitelist = new[] {"h2", "h1"};
            loggingOptions.LogRequestHeaders.Blacklist = new[] {"h2"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar Headers: (h1=hv1, h2=hv2)' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\"}]"));
        }

        [Test]
        public void Should_apply_response_headers_whitelist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogResponseHeaders = true;
            loggingOptions.LogResponseHeaders.Whitelist = new[] {"h4"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\", \"ResponseHeaders\": \"h4=hv4\"}]"));
        }

        [Test]
        public void Should_apply_response_headers_blacklist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogResponseHeaders = true;
            loggingOptions.LogResponseHeaders.Blacklist = new[] {"h4"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\", \"ResponseHeaders\": \"h3=hv3\"}]"));
        }

        [Test]
        public void Should_prefer_response_headers_whitelist_before_blacklist()
        {
            loggingOptions.LoggingMode = LoggingMode.SingleVerboseMessage;
            loggingOptions.LogResponseHeaders = true;
            loggingOptions.LogResponseHeaders.Whitelist = new[] {"h4"};
            loggingOptions.LogResponseHeaders.Blacklist = new[] {"h4"};

            Execute();

            logMessages.Should().ContainSingle(x => x.Contains("'POST http://foo/bar' to 'target_service', Timeout = infinite, Strategy = 'SingleReplica'. Success in 00:00:00, Code = 200. Replicas result = [{\"Replica\": \"http://replica/\", \"ResponseCode\": \"200\", \"Verdict\": \"Accept\", \"ElapsedTime\": \"00:00:00\", \"ResponseHeaders\": \"h4=hv4\"}]"));
        }

        private static Response CreateResponse(ResponseCode responseCode = ResponseCode.Ok, Headers headers = null) =>
            new Response(responseCode, headers: headers);

        private Request CreateRequest(string query = null, Headers headers = null)
        {
            request = new Request(RequestMethods.Post, new Uri($"http://foo/bar{query}"), Content.Empty, headers);
            context.Request.Returns(request);
            return request;
        }

        private ClusterResult Execute(ClusterResultStatus status = ClusterResultStatus.Success, ReplicaResult replicaResult = null)
        {
            var results = replicaResult == null
                ? new List<ReplicaResult>() {new ReplicaResult(new Uri("http://replica/"), response, ResponseVerdict.Accept, 0.Seconds())}
                : new List<ReplicaResult>() {replicaResult};
            return module.ExecuteAsync(context, _ => Task.FromResult(new ClusterResult(status, results, response, request))).Result;
        }
    }
}