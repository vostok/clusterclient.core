﻿using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Vostok.ClusterClient.Core.Helpers
{
    internal static class ConcurrentDictionaryExtensions
    {
        public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) =>
            dictionary.TryRemove(key, out _);

        public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value) =>
            dictionary.Remove(new KeyValuePair<TKey, TValue>(key, value));

        // (iloktionov): Explicit implementation in ConcurrentDictionary is atomic: http://blogs.msdn.com/b/pfxteam/archive/2011/04/02/10149222.aspx
        public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> pair)
        {
            var collection = (ICollection<KeyValuePair<TKey, TValue>>)dictionary;
            return collection.Remove(pair);
        }
    }
}