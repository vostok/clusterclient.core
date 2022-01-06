using System;
using System.Collections.Concurrent;

namespace Vostok.Clusterclient.Core.Ordering.Storage
{
    internal class GlobalStorageContainer<TValue>
    {
        public static readonly GlobalStorageContainer<TValue> Shared = new GlobalStorageContainer<TValue>();

        private static readonly Func<string, Func<TValue>, TValue> ValueFactory = (_, func) => func();

        private readonly ConcurrentDictionary<string, TValue> values = new ConcurrentDictionary<string, TValue>();

        public TValue Obtain(string storageKey, Func<TValue> factory) =>
            values.GetOrAdd(storageKey, ValueFactory, factory);
    }
}