﻿using System;
using Vostok.ClusterClient.Core.Annotations;

namespace Vostok.ClusterClient.Core.Transforms
{
    /// <summary>
    /// Represents a transform used to modify replica urls provided by <see cref="Topology.IClusterProvider"/> implementation.
    /// </summary>
    public interface IReplicaTransform
    {
        /// <summary>
        /// Implementations of this method MUST BE thread-safe.
        /// </summary>
        [Pure]
        [NotNull]
        Uri Transform([NotNull] Uri replica);
    }
}