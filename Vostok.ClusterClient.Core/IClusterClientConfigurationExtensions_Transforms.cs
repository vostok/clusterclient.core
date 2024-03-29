﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Core.Transforms;

namespace Vostok.Clusterclient.Core
{
    public static partial class IClusterClientConfigurationExtensions
    {
        /// <summary>
        /// Adds given <paramref name="transform"/> to configuration's <see cref="IClusterClientConfiguration.RequestTransforms"/> list.
        /// </summary>
        public static void AddRequestTransform(this IClusterClientConfiguration configuration, IRequestTransform transform)
            => AddRequestTransform(configuration, transform as IRequestTransformMetadata);

        /// <summary>
        /// Adds given <paramref name="transform"/> to configuration's <see cref="IClusterClientConfiguration.RequestTransforms"/> list.
        /// </summary>
        public static void AddRequestTransform(this IClusterClientConfiguration configuration, IAsyncRequestTransform transform)
            => AddRequestTransform(configuration, transform as IRequestTransformMetadata);

        /// <summary>
        /// Adds an <see cref="AdHocRequestTransform"/> with given <paramref name="transform"/> function to configuration's <see cref="IClusterClientConfiguration.RequestTransforms"/> list.
        /// </summary>
        public static void AddRequestTransform(this IClusterClientConfiguration configuration, Func<Request, Request> transform) =>
            AddRequestTransform(configuration, new AdHocRequestTransform(transform));

        /// <summary>
        /// Adds an <see cref="AdHocAsyncRequestTransform"/> with given <paramref name="transform"/> function to configuration's <see cref="IClusterClientConfiguration.RequestTransforms"/> list.
        /// </summary>
        public static void AddRequestTransform(this IClusterClientConfiguration configuration, Func<Request, Task<Request>> transform) =>
            AddRequestTransform(configuration, new AdHocAsyncRequestTransform(transform));

        /// <summary>
        /// Adds given <paramref name="transform"/> to configuration's <see cref="IClusterClientConfiguration.ResponseTransforms"/> list.
        /// </summary>
        public static void AddResponseTransform(this IClusterClientConfiguration configuration, IResponseTransform transform)
        {
            (configuration.ResponseTransforms ?? (configuration.ResponseTransforms = new List<IResponseTransform>())).Add(transform);
        }

        /// <summary>
        /// Adds an <see cref="AdHocResponseTransform"/> with given <paramref name="transform"/> function to configuration's <see cref="IClusterClientConfiguration.ResponseTransforms"/> list.
        /// </summary>
        public static void AddResponseTransform(this IClusterClientConfiguration configuration, Func<Response, Response> transform) =>
            AddResponseTransform(configuration, new AdHocResponseTransform(transform));

        internal static void ApplyReplicaTransform(this IClusterClientConfiguration configuration)
        {
            if (configuration.ReplicaTransform == null)
                return;

            if (configuration.ClusterProvider != null)
                configuration.ClusterProvider = new TransformingClusterProvider(configuration.ClusterProvider, configuration.ReplicaTransform);

            if (configuration.AsyncClusterProvider != null)
                configuration.AsyncClusterProvider = new TransformingAsyncClusterProvider(configuration.AsyncClusterProvider, configuration.ReplicaTransform);
        }

        private static void AddRequestTransform(this IClusterClientConfiguration configuration, IRequestTransformMetadata transform)
            => (configuration.RequestTransforms ?? (configuration.RequestTransforms = new List<IRequestTransformMetadata>())).Add(transform);
    }
}