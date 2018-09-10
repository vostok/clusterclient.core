﻿using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterClient.Abstractions.Model;
using Vostok.ClusterClient.Core.Criteria;
using Vostok.ClusterClient.Core.Model;

namespace Vostok.ClusterClient.Core.Tests.Criteria
{
    [TestFixture]
    internal class AcceptNonRetriableCriterion_Tests
    {
        private AcceptNonRetriableCriterion criterion;

        [SetUp]
        public void TestSetup()
        {
            criterion = new AcceptNonRetriableCriterion();
        }

        [Test]
        public void Should_accept_an_error_response_with_dont_retry_header()
        {
            var response = new Response(ResponseCode.ServiceUnavailable, headers: Headers.Empty.Set(HeaderNames.XVostokDontRetry, ""));

            criterion.Decide(response).Should().Be(ResponseVerdict.Accept);
        }

        [Test]
        public void Should_know_nothing_about_an_error_response_without_dont_retry_header()
        {
            var response = new Response(ResponseCode.ServiceUnavailable, headers: Headers.Empty);

            criterion.Decide(response).Should().Be(ResponseVerdict.DontKnow);
        }
    }
}
