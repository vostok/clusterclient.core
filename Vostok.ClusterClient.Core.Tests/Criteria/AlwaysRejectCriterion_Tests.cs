﻿using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Criteria;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Tests.Criteria
{
    [TestFixture]
    internal class AlwaysRejectCriterion_Tests
    {
        [Test]
        public void Should_reject_all_response_codes()
        {
            var criterion = new AlwaysRejectCriterion();

            var codes = Enum.GetValues(typeof(ResponseCode)).Cast<ResponseCode>();

            foreach (var code in codes)
            {
                criterion.Decide(new Response(code)).Should().Be(ResponseVerdict.Reject);
            }
        }
    }
}