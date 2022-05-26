using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Transforms;
using Vostok.Commons.Collections;

namespace Vostok.Clusterclient.Core.Topology
{
    internal class TransformingAsyncClusterProvider : IAsyncClusterProvider
    {
        private readonly IAsyncClusterProvider provider;
        private readonly IReplicaTransform transform;

        private readonly CachingTransform<IList<Uri>, IList<Uri>> cache;

        public TransformingAsyncClusterProvider(IAsyncClusterProvider provider, IReplicaTransform transform)
        {
            this.provider = provider;
            this.transform = transform;
            cache = new CachingTransform<IList<Uri>, IList<Uri>>(ApplyTransform);
        }

        public async Task<IList<Uri>> GetClusterAsync() 
            => cache.Get(await provider.GetClusterAsync());
        
        private IList<Uri> ApplyTransform(IList<Uri> replicas)
            => replicas == null
                ? null
                : transform.Transform(replicas);
    }
}