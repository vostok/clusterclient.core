using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Topology.TargetEnvironment;

namespace Vostok.Clusterclient.Core.Tests.Topology.TargetEnvironment
{
    [TestFixture]
    internal class AdHocTargetEnvironmentProvider_Tests
    {
        [Test]
        public void Should_ensure_that_provided_delegate_is_not_null()
        {
            Assert.Throws<ArgumentNullException>(() => new AdHocTargetEnvironmentProvider(null));
        }

        [Test]
        public void Should_use_delegate_on_each_find_call()
        {
            var values = new[] {"quick", "brown", "fox", null};
            var index = 0;
            var provider = new AdHocTargetEnvironmentProvider(() => values[index++]);

            var actual = new[] {provider.Find(), provider.Find(), provider.Find(), provider.Find()};

            actual.Should().Equal(values);
        }
    }
}