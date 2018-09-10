﻿using System;
using Vostok.ClusterClient.Abstractions.Transforms;

namespace Vostok.ClusterClient.Core.Transforms
{
    /// <summary>
    /// Represents a replica transform which uses external delegate to modify urls.
    /// </summary>
    public class AdHocReplicaTransform : IReplicaTransform
    {
        private readonly Func<Uri, Uri> transform;

        public AdHocReplicaTransform(Func<Uri, Uri> transform)
        {
            this.transform = transform;
        }

        public Uri Transform(Uri replica) => transform(replica);
    }
}