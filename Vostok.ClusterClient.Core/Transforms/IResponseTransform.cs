﻿using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Transforms
{
    /// <summary>
    /// <para>Represents a transform used to modify selected response before returning it to client in <see cref="ClusterResult"/>.</para>
    /// <para>Response transforms form a chain where each transform works with a result of previous one.</para>
    /// </summary>
    [PublicAPI]
    public interface IResponseTransform
    {
        /// <summary>
        /// Implementations of this method MUST BE thread-safe.
        /// </summary>
        [Pure]
        [NotNull]
        Response Transform([NotNull] Response response);
    }
}