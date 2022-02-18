using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Clusterclient.Core.Ordering.Weighed;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Vostok.Clusterclient.Core.Tests.Ordering.Weighed;

[Explicit]
public class WeighedReplicaOrdering_Benchmarks
{
    [Test]
    public void RunBenchmarks()
    {
        BenchmarkRunner.Run<WeighedReplicaOrdering_Benchmarks>(
            DefaultConfig.Instance
                .AddDiagnoser(MemoryDiagnoser.Default)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true));
    }

    [Params(1, 3, 5, 10, 20, 30, 100)]
    public int TotalReplicas { get; set; }

    [Params(1, 2, 3, 10, -1)]
    public int SelectReplicas { get; set; }
    
    private Uri[] replicas;
    private IReplicaOrdering ordering;
    private IReplicaStorageProvider storageProvider;
    private Request request;
    private RequestParameters parameters;
    
    [GlobalSetup]
    public void SetUp()
    {
        if (SelectReplicas == -1)
            SelectReplicas = TotalReplicas;
        
        ordering = new WeighedReplicaOrdering(new List<IReplicaWeightModifier>());
        
        replicas = Enumerable.Range(0, TotalReplicas).Select(j => new Uri($"http://foo/bar/{j}")).ToArray();
        storageProvider = new PerInstanceReplicaStorageProvider();
        request = Request.Get("http://foo/bar");
        parameters = new RequestParameters();
    }

    [Benchmark]
    public void Select()
    {
        if (SelectReplicas > TotalReplicas)
            return;
        
        var ordered = ordering.Order(replicas, storageProvider, request, parameters);
        using var replicasEnumerator = ordered.GetEnumerator();
        for (var i = 0; i < SelectReplicas; i++)
            replicasEnumerator.MoveNext();
    }
}