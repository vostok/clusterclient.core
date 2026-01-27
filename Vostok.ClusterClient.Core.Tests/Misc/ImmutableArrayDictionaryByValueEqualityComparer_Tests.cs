using System.Collections.Generic;
using NUnit.Framework;
using Vostok.Commons.Collections;
using Vostok.Clusterclient.Core.Misc;

namespace Vostok.Clusterclient.Core.Tests.Misc
{
    public class ImmutableArrayDictionaryByValueEqualityComparer_Tests
    {
        [TestCase(0, 0)]
        [TestCase("", "")]
        [TestCase("", 0)]
        [TestCase(0, "")]
        public void EmptyDictsShouldBeEqual<TKey, TValue>(TKey keyDummy, TValue valueDummy)
        {
            var comparer = GetComparerInstance<TKey, TValue>();
            var empty1 = ImmutableArrayDictionary<TKey, TValue>.Empty;
            var empty2 = new ImmutableArrayDictionary<TKey, TValue>(0);
            Assert.True(comparer.Equals(empty1, empty2));
            Assert.True(comparer.GetHashCode(empty1) == comparer.GetHashCode(empty2));
        }

        [TestCase(1, 1)]
        [TestCase("abc", "abc")]
        [TestCase("abc", 1)]
        [TestCase(1, "abc")]
        public void SingleValueDictsShouldBeEqual<TKey, TValue>(TKey key, TValue value)
        {
            var comparer = GetComparerInstance<TKey, TValue>();
            var dict1 = new ImmutableArrayDictionary<TKey, TValue>(new Dictionary<TKey, TValue> {{key, value}});
            var dict2 = new ImmutableArrayDictionary<TKey, TValue>(new Dictionary<TKey, TValue> {{key, value}});
            Assert.True(comparer.Equals(dict1, dict2));
            Assert.True(comparer.GetHashCode(dict1) == comparer.GetHashCode(dict2));
        }

        [TestCase(1, 1)]
        [TestCase("key", "value")]
        [TestCase("key1", "value1")]
        public void GetHashCodeShouldBeStable<TKey, TValue>(TKey key, TValue value)
        {
            var comparer = GetComparerInstance<TKey, TValue>();
            var dict1 = new ImmutableArrayDictionary<TKey, TValue>(new Dictionary<TKey, TValue> {{key, value}});
            var hash = comparer.GetHashCode(dict1);
            for (var i = 0; i < 10; i++)
                Assert.AreEqual(hash, comparer.GetHashCode(dict1));
            var dict2 = new ImmutableArrayDictionary<TKey, TValue>(new Dictionary<TKey, TValue> {{key, value}});
            Assert.AreEqual(hash, comparer.GetHashCode(dict2));
        }

        [Test]
        public void OperationsAreKeyOrderIndependent()
        {
            var comparer = GetComparerInstance<string, string>();
            var dict1 = new ImmutableArrayDictionary<string, string>(3);
            foreach (var (key, value) in new (string, string)[] {("key1", "value1"), ("key2", "value2"), ("key3", "value3")})
                dict1.AppendUnsafe(key, value);

            var dict2 = new ImmutableArrayDictionary<string, string>(3);
            foreach (var (key, value) in new (string, string)[] {("key2", "value2"), ("key3", "value3"), ("key1", "value1")})
                dict2.AppendUnsafe(key, value);

            Assert.True(comparer.Equals(dict1, dict2));
            Assert.True(comparer.GetHashCode(dict1) == comparer.GetHashCode(dict2));
        }

        [Test]
        public void Should_not_collide_on_key_or_value_cyclic_shift()
        {
            var comparer = GetComparerInstance<string, string>();
            var dict1 = new ImmutableArrayDictionary<string, string>(3);
            foreach (var (key, value) in new (string, string)[] {("key3", "value2"), ("key1", "value3"), ("key2", "value1")})
                dict1.AppendUnsafe(key, value);

            var dict2 = new ImmutableArrayDictionary<string, string>(3);
            foreach (var (key, value) in new (string, string)[] {("key2", "value2"), ("key3", "value3"), ("key1", "value1")})
                dict2.AppendUnsafe(key, value);

            Assert.False(comparer.Equals(dict1, dict2));
            Assert.False(comparer.GetHashCode(dict1) == comparer.GetHashCode(dict2));
        }

        private ImmutableArrayDictionaryByValueEqualityComparer<TKey, TValue> GetComparerInstance<TKey, TValue>() =>
            ImmutableArrayDictionaryByValueEqualityComparer<TKey, TValue>.Instance;
    }
}