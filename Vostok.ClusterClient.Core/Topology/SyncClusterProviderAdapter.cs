using System;
using System.Collections.Generic;

namespace Vostok.Clusterclient.Core.Topology
{
    internal class SyncClusterProviderAdapter : IClusterProvider
    {
        private readonly IAsyncClusterProvider provider;

        public SyncClusterProviderAdapter(IAsyncClusterProvider provider) =>
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));

        public IList<Uri> GetCluster() =>
            provider.GetClusterAsync().GetAwaiter().GetResult();
    }
}