using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Topology.TargetEnvironment;

namespace Vostok.Clusterclient.Core.Tests.Topology.TargetEnvironment
{
    [TestFixture]
    internal class CompositeTargetEnvironmentProvider_Tests
    {
        [Test]
        public void Should_ensure_that_inner_providers_is_not_null()
        {
            ITargetEnvironmentProvider[] providers = null;
            Assert.Throws<ArgumentNullException>(() => new CompositeTargetEnvironmentProvider(providers));
        }

        [Test]
        public void Should_ensure_that_every_inner_provider_is_not_null()
        {
            var providers = new ITargetEnvironmentProvider[] {null};
            Assert.Throws<ArgumentException>(() => new CompositeTargetEnvironmentProvider(providers));
        }

        [Test]
        public void Should_return_first_non_null_value()
        {
            var provider = new CompositeTargetEnvironmentProvider(
                new AdHocTargetEnvironmentProvider(() => null),
                new AdHocTargetEnvironmentProvider(() => "expected_environment"),
                new AdHocTargetEnvironmentProvider(() => throw new Exception("CompositeTargetEnvironmentProvider should be lazy, this delegate should not be called"))
            );

            var actual = provider.Find();

            actual.Should().Be("expected_environment");
        }

        [Test]
        public void Should_not_swallow_inner_provider_exception()
        {
            var exception = new Exception("Inner provider exception");

            var provider = new CompositeTargetEnvironmentProvider(
                new AdHocTargetEnvironmentProvider(() => throw exception)
            );

            Action act = () => provider.Find();
            act.Should().Throw<Exception>().WithMessage(exception.Message);
        }
    }
}