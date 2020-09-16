using System;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Topology.TargetEnvironment;

namespace Vostok.Clusterclient.Core.Tests.Topology.TargetEnvironment
{
    [TestFixture]
    internal class FixedTargetEnvironmentProvider_Tests
    {
        [Test]
        public void Should_ensure_that_provided_environment_is_not_null()
        {
            Assert.Throws<ArgumentNullException>(() => new FixedTargetEnvironmentProvider(null));
        }

        [TestCase("environment1", ExpectedResult = "environment1")]
        [TestCase("environment2", ExpectedResult = "environment2")]
        [TestCase("default", ExpectedResult = "default")]
        public string Should_return_fixed_environment_value(string environment)
        {
            return new FixedTargetEnvironmentProvider(environment).Find();
        }
    }
}