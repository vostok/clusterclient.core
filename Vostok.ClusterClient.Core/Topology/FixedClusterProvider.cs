﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Topology
{
    /// <summary>
    /// Represents a cluster provider which always returns a fixed list of urls.
    /// </summary>
    [PublicAPI]
    public class FixedClusterProvider : IClusterProvider
    {
        private readonly IList<Uri> replicas;

        /// <param name="replicas">A list of replica <see cref="Uri"/> which this provider should return.</param>
        public FixedClusterProvider(IList<Uri> replicas)
        {
            this.replicas = replicas;
        }

        /// <param name="replicas">A list of replica <see cref="Uri"/> which this provider should return.</param>
        public FixedClusterProvider(params Uri[] replicas)
        {
            this.replicas = replicas;
        }

        /// <param name="replicas">A list of replica Uri <see cref="string"/> which this provider should return.</param>
        public FixedClusterProvider(params string[] replicas)
        {
            this.replicas = replicas.Select(x => new Uri(x, UriKind.Absolute)).ToArray();
        }

        /// <inheritdoc />
        public IList<Uri> GetCluster() => replicas;
    }
}