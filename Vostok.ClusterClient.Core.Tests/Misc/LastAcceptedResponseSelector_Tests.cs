using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Tests.Misc
{
    [TestFixture]
    internal class LastAcceptedResponseSelector_Tests
    {
        private LastAcceptedResponseSelector selector;
        private List<ReplicaResult> results;

        [SetUp]
        public void TestSetup()
        {
            selector = new LastAcceptedResponseSelector();
            results = new List<ReplicaResult>();
        }

        [Test]
        public void Should_return_null_when_there_are_no_results()
        {
            selector.Select(null, null, results).Should().BeNull();
        }

        [Test]
        public void Should_return_last_of_the_accepted_responses_if_there_are_any()
        {
            results.Add(CreateResult(ResponseVerdict.Accept));
            results.Add(CreateResult(ResponseVerdict.Reject));
            results.Add(CreateResult(ResponseVerdict.Reject));
            results.Add(CreateResult(ResponseVerdict.Accept));
            results.Add(CreateResult(ResponseVerdict.Reject));

            selector.Select(null, null, results).Should().BeSameAs(results[3].Response);
        }

        [Test]
        public void Should_prefer_accepted_responses_without_UnreliableResponse_header()
        {
            results.Add(CreateResult(ResponseVerdict.Accept, headers: Headers.Empty.Set(HeaderNames.UnreliableResponse, "true")));
            results.Add(CreateResult(ResponseVerdict.Accept));
            results.Add(CreateResult(ResponseVerdict.Accept, headers: Headers.Empty.Set(HeaderNames.UnreliableResponse, "true")));

            selector.Select(null, null, results).Should().BeSameAs(results[1].Response);
        }

        [Test]
        public void Should_return_last_of_the_accepted_responses_with_UnreliableResponse_if_all_have_such_header()
        {
            results.Add(CreateResult(ResponseVerdict.Accept, headers: Headers.Empty.Set(HeaderNames.UnreliableResponse, "true")));
            results.Add(CreateResult(ResponseVerdict.Accept, headers: Headers.Empty.Set(HeaderNames.UnreliableResponse, "true")));
            results.Add(CreateResult(ResponseVerdict.Accept, headers: Headers.Empty.Set(HeaderNames.UnreliableResponse, "true")));

            selector.Select(null, null, results).Should().BeSameAs(results[2].Response);
        }

        [Test]
        public void Should_return_last_of_the_known_responses_if_there_are_no_accepted_ones()
        {
            results.Add(CreateResult(ResponseVerdict.Reject, ResponseCode.Unknown));
            results.Add(CreateResult(ResponseVerdict.Reject, ResponseCode.Unknown));
            results.Add(CreateResult(ResponseVerdict.Reject));
            results.Add(CreateResult(ResponseVerdict.Reject));
            results.Add(CreateResult(ResponseVerdict.Reject, ResponseCode.Unknown));

            selector.Select(null, null, results).Should().BeSameAs(results[3].Response);
        }

        [Test]
        public void Should_return_just_the_last_response_if_there_are_no_accepted_or_known_ones()
        {
            results.Add(CreateResult(ResponseVerdict.Reject, ResponseCode.Unknown));
            results.Add(CreateResult(ResponseVerdict.Reject, ResponseCode.Unknown));
            results.Add(CreateResult(ResponseVerdict.Reject, ResponseCode.Unknown));

            selector.Select(null, null, results).Should().BeSameAs(results.Last().Response);
        }

        [TestCase(ResponseCode.StreamReuseFailure)]
        [TestCase(ResponseCode.ContentReuseFailure)]
        public void Should_avoid_choosing_reuse_failure_responses_when_there_are_others(ResponseCode failureCode)
        {
            results.Add(CreateResult(ResponseVerdict.Reject, failureCode));
            results.Add(CreateResult(ResponseVerdict.Reject));
            results.Add(CreateResult(ResponseVerdict.Reject, failureCode));

            selector.Select(null, null, results).Should().BeSameAs(results[1].Response);
        }

        private static ReplicaResult CreateResult(ResponseVerdict verdict, ResponseCode code = ResponseCode.Ok, Headers headers = null)
        {
            return new ReplicaResult(new Uri("http://host:123/"), new Response(code, headers: headers), verdict, TimeSpan.Zero);
        }
    }
}