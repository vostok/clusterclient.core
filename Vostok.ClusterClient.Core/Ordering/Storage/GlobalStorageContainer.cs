using System;
using System.Collections.Concurrent;

namespace Vostok.Clusterclient.Core.Ordering.Storage
{
    internal class GlobalStorageContainer<TValue>
    {
        public static readonly GlobalStorageContainer<TValue> Shared = new GlobalStorageContainer<TValue>();

#if NET6_0_OR_GREATER
        private static readonly Func<string, Func<TValue>, TValue> ValueFactory = (_, func) => func();
#endif

        private readonly ConcurrentDictionary<string, TValue> values = new ConcurrentDictionary<string, TValue>();

        public TValue Obtain(string storageKey, Func<TValue> factory) =>
#if NET6_0_OR_GREATER
            values.GetOrAdd(storageKey, ValueFactory, factory);
#else
            values.GetOrAdd(storageKey, _ => factory());
#endif
    }
}