using System;
using System.Collections.Generic;
using Vostok.Commons.Collections;

namespace Vostok.Clusterclient.Core.Misc
{
    internal class ImmutableArrayDictionaryByValueEqualityComparer<TKey, TValue> : IEqualityComparer<ImmutableArrayDictionary<TKey, TValue>>
    {
        public static readonly ImmutableArrayDictionaryByValueEqualityComparer<TKey, TValue> Instance = new();

        public bool Equals(ImmutableArrayDictionary<TKey, TValue> x, ImmutableArrayDictionary<TKey, TValue> y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            if (x.GetType() != y.GetType())
                return false;
            if (x.Count != y.Count)
                return false;

            foreach (var pair in x)
            {
                if (!y.TryGetValue(pair.Key, out var yValue)) return false;
                if (!EqualityComparer<TValue>.Default.Equals(pair.Value, yValue)) return false;
            }

            return true;
        }

        public int GetHashCode(ImmutableArrayDictionary<TKey, TValue> obj)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (obj == null) return 0;
            
            var hash = 0;
            foreach (var pair in obj)
            {
                var keyHash = EqualityComparer<TKey>.Default.GetHashCode(pair.Key);
                var valueHash = EqualityComparer<TValue>.Default.GetHashCode(pair.Value);
#if NETSTANDARD2_0
                hash = unchecked(hash + (keyHash * 397 ^ valueHash * 23));
#else
                hash = unchecked(hash + HashCode.Combine(keyHash, valueHash));
#endif
            }

            return hash;
        }

        private ImmutableArrayDictionaryByValueEqualityComparer()
        {
        }
    }
}