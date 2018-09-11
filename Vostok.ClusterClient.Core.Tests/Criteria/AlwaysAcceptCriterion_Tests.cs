﻿using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Core.Criteria;

namespace Vostok.ClusterClient.Core.Tests.Criteria
{
    [TestFixture]
    internal class AlwaysAcceptCriterion_Tests
    {
        [Test]
        public void Should_accept_all_response_codes()
        {
            var criterion = new AlwaysAcceptCriterion();

            var codes = Enum.GetValues(typeof (ResponseCode)).Cast<ResponseCode>();

            foreach (var code in codes)
            {
                criterion.Decide(new Response(code)).Should().Be(ResponseVerdict.Accept);
            }
        }
    }
}
