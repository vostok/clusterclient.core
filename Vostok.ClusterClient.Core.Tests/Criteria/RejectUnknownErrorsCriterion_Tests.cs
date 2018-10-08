﻿using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Criteria;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Tests.Criteria
{
    [TestFixture]
    internal class RejectUnknownErrorsCriterion_Tests
    {
        private RejectUnknownErrorsCriterion criterion;

        [SetUp]
        public void TestSetup()
        {
            criterion = new RejectUnknownErrorsCriterion();
        }

        [TestCase(ResponseCode.Unknown)]
        [TestCase(ResponseCode.UnknownFailure)]
        public void Should_reject_given_response_code(ResponseCode code)
        {
            criterion.Decide(new Response(code)).Should().Be(ResponseVerdict.Reject);
        }

        [Test]
        public void Should_know_nothing_about_codes_which_are_not_unknown_errors()
        {
            var codes = Enum
                .GetValues(typeof(ResponseCode))
                .Cast<ResponseCode>()
                .Where(
                    code =>
                        code != ResponseCode.Unknown &&
                        code != ResponseCode.UnknownFailure);

            foreach (var code in codes)
            {
                criterion.Decide(new Response(code)).Should().Be(ResponseVerdict.DontKnow);
            }
        }
    }
}