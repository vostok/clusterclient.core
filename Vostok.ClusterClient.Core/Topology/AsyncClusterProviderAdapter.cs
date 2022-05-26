using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vostok.Clusterclient.Core.Topology
{
    internal class AsyncClusterProviderAdapter : IAsyncClusterProvider
    {
        private readonly IClusterProvider provider;

        public AsyncClusterProviderAdapter(IClusterProvider provider) =>
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));

        public Task<IList<Uri>> GetClusterAsync() =>
            Task.FromResult(provider.GetCluster());
    }
}