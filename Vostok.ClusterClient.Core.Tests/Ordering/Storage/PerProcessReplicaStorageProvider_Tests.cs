using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Ordering.Storage;

namespace Vostok.Clusterclient.Core.Tests.Ordering.Storage
{
    [TestFixture]
    internal class PerProcessReplicaStorageProvider_Tests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("key")]
        public void Two_different_instances_should_return_same_storage_for_same_storage_key(string storageKey)
        {
            var provider1 = new PerProcessReplicaStorageProvider();
            var provider2 = new PerProcessReplicaStorageProvider();

            var storage1 = provider1.Obtain<int>();
            var storage2 = provider2.Obtain<int>();

            storage2.Should().BeSameAs(storage1);
        }
    }
}