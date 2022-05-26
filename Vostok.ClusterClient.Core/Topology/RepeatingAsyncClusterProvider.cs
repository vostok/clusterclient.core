using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Commons.Collections;

namespace Vostok.Clusterclient.Core.Topology;

internal class RepeatingAsyncClusterProvider : IAsyncClusterProvider
{
    private readonly IAsyncClusterProvider provider;
    private readonly CachingTransform<IList<Uri>, IList<Uri>> cache;

    public RepeatingAsyncClusterProvider(IAsyncClusterProvider provider, int repeatCount)
    {
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));

        if (repeatCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "Repeat count must be positive.");

        cache = new CachingTransform<IList<Uri>, IList<Uri>>(x => Repeat(x, repeatCount));
    }

    public async Task<IList<Uri>> GetClusterAsync() 
        => cache.Get(await provider.GetClusterAsync());

    private static IList<Uri> Repeat(IList<Uri> currentReplicas, int repeatCount)
    {
        if (currentReplicas == null)
            return null;

        var repeatedReplicas = new List<Uri>(currentReplicas.Count * repeatCount);

        for (var i = 0; i < repeatCount; i++)
            repeatedReplicas.AddRange(currentReplicas);

        return repeatedReplicas;
    }
}