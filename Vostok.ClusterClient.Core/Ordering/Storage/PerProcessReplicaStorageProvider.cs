using System;
using System.Collections.Concurrent;

namespace Vostok.Clusterclient.Core.Ordering.Storage
{
    internal class PerProcessReplicaStorageProvider : IReplicaStorageProvider
    {
        public ConcurrentDictionary<Uri, TValue> Obtain<TValue>(string storageKey = null) =>
            ReplicaStorageContainer<TValue>.Shared.Obtain(storageKey);

        public TValue ObtainGlobalValue<TValue>(string storageKey, Func<TValue> factory) =>
            GlobalStorageContainer<TValue>.Shared.Obtain(storageKey, factory);
    }
}