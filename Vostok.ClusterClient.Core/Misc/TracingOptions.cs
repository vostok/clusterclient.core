#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Misc;

/// <summary>
/// Tracing configuration options.
/// </summary>
[PublicAPI]
public sealed class TracingOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// If set to a non-null value, an additional request transformation will be applied to given <see cref="Request"/> with current <see cref="ActivityContext"/>.
    /// </summary>
    [CanBeNull]
    public Func<Request, ActivityContext, Request> AdditionalRequestTransformation { get; set; }

    /// <summary>
    /// If set to a non-null value, an additional <see cref="Request"/> transformation will be applied to current <see cref="Activity"/>.
    /// </summary>
    [CanBeNull]
    public Action<Activity, Request> EnrichWithRequest { get; set; }

    /// <summary>
    /// If set to a non-null value, an additional <see cref="Response"/> transformation will be applied to current <see cref="Activity"/>.
    /// </summary>
    [CanBeNull]
    public Action<Activity, Response> EnrichWithResponse { get; set; }

    /// <summary>
    /// If set to a non-null value, an additional <see cref="ClusterResult"/> transformation will be applied to current <see cref="Activity"/>.
    /// </summary>
    [CanBeNull]
    public Action<Activity, ClusterResult> EnrichWithClusterResult { get; set; }
}
#endif