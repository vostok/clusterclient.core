﻿using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Retry;

namespace Vostok.Clusterclient.Core.Tests.Retry
{
    [TestFixture]
    internal class VerdictBasedRetryPolicy_Tests
    {
        private VerdictBasedRetryPolicy policy;

        [SetUp]
        public void TestSetup()
        {
            policy = new VerdictBasedRetryPolicy();
        }

        [Test]
        public void NeedToRetry_should_return_true_if_no_results()
        {
            policy.NeedToRetry(default, default, new List<ReplicaResult>()).Should().BeTrue();
        }

        [Test]
        public void NeedToRetry_should_return_true_if_all_rejected_and_no_DontFork_header()
        {
            var results = new List<ReplicaResult>() {BuildResult(ResponseVerdict.Reject), BuildResult(ResponseVerdict.Reject)};
            policy.NeedToRetry(default, default, results).Should().BeTrue();
        }

        [Test]
        public void NeedToRetry_should_return_false_if_any_accepted()
        {
            var results = new List<ReplicaResult>() {BuildResult(ResponseVerdict.Reject), BuildResult(ResponseVerdict.Accept)};
            policy.NeedToRetry(default, default, results).Should().BeFalse();
        }

        private static ReplicaResult BuildResult(ResponseVerdict verdict, Headers responseHeaders = null)
        {
            return new ReplicaResult(default, new Response(default, headers: responseHeaders), verdict, default);
        }
    }
}