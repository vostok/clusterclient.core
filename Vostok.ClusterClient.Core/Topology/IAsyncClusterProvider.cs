using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Topology
{
    /// <inheritdoc cref="IClusterProvider"/>
    [PublicAPI]
    public interface IAsyncClusterProvider
    {
        /// <inheritdoc cref="IClusterProvider.GetCluster"/>
        [Pure]
        [NotNull]
        [ItemCanBeNull]
        Task<IList<Uri>> GetClusterAsync();
    }
}