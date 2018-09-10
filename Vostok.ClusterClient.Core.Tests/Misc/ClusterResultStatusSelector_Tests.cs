using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterClient.Abstractions.Model;
using Vostok.ClusterClient.Core.Misc;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Core.Tests.Helpers;

namespace Vostok.ClusterClient.Core.Tests.Misc
{
    [TestFixture]
    internal class ClusterResultStatusSelector_Tests
    {
        private ClusterResultStatusSelector selector;

        [SetUp]
        public void TestSetup()
        {
            selector = new ClusterResultStatusSelector();
        }

        [Test]
        public void Should_return_success_status_if_there_is_at_least_one_accepted_result()
        {
            var status = selector.Select(Group(Result(ResponseVerdict.Reject), Result(ResponseVerdict.Accept)), Budget.Infinite);

            AssertionExtensions.Should((object)status).Be(ClusterResultStatus.Success);
        }

        [Test]
        public void Should_return_replicas_exhausted_status_if_there_are_no_accepted_results()
        {
            var status = selector.Select(Group(Result(ResponseVerdict.Reject), Result(ResponseVerdict.Reject)), Budget.Infinite);

            AssertionExtensions.Should((object)status).Be(ClusterResultStatus.ReplicasExhausted);
        }

        [Test]
        public void Should_return_replicas_exhausted_status_if_there_are_no_accepted_results_and_time_budget_has_expired()
        {
            var status = selector.Select(Group(Result(ResponseVerdict.Reject), Result(ResponseVerdict.Reject)), Budget.Expired);

            AssertionExtensions.Should((object)status).Be(ClusterResultStatus.TimeExpired);
        }

        private IList<ReplicaResult> Group(params ReplicaResult[] results)
        {
            return results;
        }

        private ReplicaResult Result(ResponseVerdict verdict)
        {
            return new ReplicaResult(new Uri("http://replica"), new Response(ResponseCode.NotFound), verdict, TimeSpan.Zero);
        }
    }
}
