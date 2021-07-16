using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Criteria;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Tests.Criteria
{
    [TestFixture]
    internal class RejectContentErrorsCriterion_Tests
    {
        private RejectContentErrorsCriterion criterion;

        [SetUp]
        public void TestSetup()
        {
            criterion = new RejectContentErrorsCriterion();
        }

        [TestCase(ResponseCode.ContentInputFailure)]
        [TestCase(ResponseCode.ContentReuseFailure)]
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
                        code != ResponseCode.ContentReuseFailure &&
                        code != ResponseCode.ContentInputFailure);

            foreach (var code in codes)
            {
                criterion.Decide(new Response(code)).Should().Be(ResponseVerdict.DontKnow);
            }
        }
    }
}