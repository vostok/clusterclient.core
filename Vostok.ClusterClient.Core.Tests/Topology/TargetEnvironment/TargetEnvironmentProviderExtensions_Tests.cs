using System;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Topology.TargetEnvironment;

namespace Vostok.Clusterclient.Core.Tests.Topology.TargetEnvironment
{
    [TestFixture]
    internal class TargetEnvironmentProviderExtensions_Tests
    {
        [Test]
        public void Should_throw_exception_on_get_call_when_environment_is_null()
        {
            var provider = new AdHocTargetEnvironmentProvider(() => null);
            Assert.Throws<Exception>(() => provider.Get());
        }

        [Test]
        public void Should_not_throw_exception_on_get_call_when_environment_is_not_null()
        {
            var provider = new AdHocTargetEnvironmentProvider(() => "non-null-value");
            Assert.DoesNotThrow(() => provider.Get());
        }
    }
}