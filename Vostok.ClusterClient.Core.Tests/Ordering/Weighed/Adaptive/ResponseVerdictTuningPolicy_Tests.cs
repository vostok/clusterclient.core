using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering.Weighed.Adaptive;

namespace Vostok.Clusterclient.Core.Tests.Ordering.Weighed.Adaptive
{
    [TestFixture]
    internal class ResponseVerdictTuningPolicy_Tests
    {
        private ResponseVerdictTuningPolicy policy;

        [SetUp]
        public void TestSetup()
        {
            policy = new ResponseVerdictTuningPolicy();
        }

        [TestCase(ResponseVerdict.Accept, AdaptiveHealthAction.Increase)]
        [TestCase(ResponseVerdict.Reject, AdaptiveHealthAction.Decrease)]
        [TestCase(ResponseVerdict.DontKnow, AdaptiveHealthAction.DontTouch)]
        public void Should_return_correct_action_for_given_response_verdict(ResponseVerdict verdict, AdaptiveHealthAction action)
        {
            var result = new ReplicaResult(new Uri("http://replica"), Responses.Timeout, verdict, TimeSpan.Zero);

            policy.SelectAction(result).Should().Be(action);
        }

        [TestCase(ResponseVerdict.Accept)]
        [TestCase(ResponseVerdict.Reject)]
        [TestCase(ResponseVerdict.DontKnow)]
        public void Should_choose_to_not_touch_health_when_replica_response_code_indicates_stream_reuse_failure(ResponseVerdict verdict)
        {
            Should_choose_to_not_touch_health_when_replica_response_code_indicates_response(Responses.StreamReuseFailure, verdict);
        }

        [TestCase(ResponseVerdict.Accept)]
        [TestCase(ResponseVerdict.Reject)]
        [TestCase(ResponseVerdict.DontKnow)]
        public void Should_choose_to_not_touch_health_when_replica_response_code_indicates_stream_input_failure(ResponseVerdict verdict)
        {
            Should_choose_to_not_touch_health_when_replica_response_code_indicates_response(Responses.StreamInputFailure, verdict);
        }

        [TestCase(ResponseVerdict.Accept)]
        [TestCase(ResponseVerdict.Reject)]
        [TestCase(ResponseVerdict.DontKnow)]
        public void Should_choose_to_not_touch_health_when_replica_response_code_indicates_content_reuse_failure(ResponseVerdict verdict)
        {
            Should_choose_to_not_touch_health_when_replica_response_code_indicates_response(Responses.ContentReuseFailure, verdict);
        }

        [TestCase(ResponseVerdict.Accept)]
        [TestCase(ResponseVerdict.Reject)]
        [TestCase(ResponseVerdict.DontKnow)]
        public void Should_choose_to_not_touch_health_when_replica_response_code_indicates_content_input_failure(ResponseVerdict verdict)
        {
            Should_choose_to_not_touch_health_when_replica_response_code_indicates_response(Responses.ContentInputFailure, verdict);
        }

        private void Should_choose_to_not_touch_health_when_replica_response_code_indicates_response(Response response, ResponseVerdict verdict)
        {
            var result = new ReplicaResult(new Uri("http://replica"), response, verdict, TimeSpan.Zero);

            policy.SelectAction(result).Should().Be(AdaptiveHealthAction.DontTouch);
        }
    }
}