﻿using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Strategies.DelayProviders;
using Vostok.Clusterclient.Core.Tests.Helpers;

namespace Vostok.Clusterclient.Core.Tests.Strategies.DelayProviders
{
    [TestFixture]
    internal class EqualDelaysProvider_Tests
    {
        private Request request;

        [SetUp]
        public void TestSetup()
        {
            request = Request.Get("/foo");
        }

        [Test]
        public void Should_throw_an_error_when_given_negative_division_factor()
        {
            Action action = () => new EqualDelaysProvider(-1);

            action.Should().Throw<ArgumentOutOfRangeException>().Which.ShouldBePrinted();
        }

        [Test]
        public void Should_throw_an_error_when_given_zero_division_factor()
        {
            Action action = () => new EqualDelaysProvider(0);

            action.Should().Throw<ArgumentOutOfRangeException>().Which.ShouldBePrinted();
        }

        [Test]
        public void Should_return_equal_portions_of_total_budget_divided_by_given_factor()
        {
            var provider = new EqualDelaysProvider(3);

            var budget = Budget.WithRemaining(12.Seconds());

            for (var i = 0; i < 10; i++)
            {
                provider.GetForkingDelay(request, budget, i, 10).Should().Be(4.Seconds());
            }
        }

        [Test]
        public void Should_adjust_division_factor_if_there_are_not_enough_replicas()
        {
            var provider = new EqualDelaysProvider(3);

            var budget = Budget.WithRemaining(12.Seconds());

            provider.GetForkingDelay(request, budget, 0, 2).Should().Be(6.Seconds());
            provider.GetForkingDelay(request, budget, 1, 2).Should().Be(6.Seconds());
        }
    }
}