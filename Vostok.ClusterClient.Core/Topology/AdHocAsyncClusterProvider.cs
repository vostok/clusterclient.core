using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Topology
{
    /// <inheritdoc cref="AdHocClusterProvider"/>
    [PublicAPI]
    public class AdHocAsyncClusterProvider : IAsyncClusterProvider
    {
        private readonly Func<Task<IList<Uri>>> replicasProvider;

        /// <param name="replicasProvider">An external delegate which will provides replica urls.</param>
        public AdHocAsyncClusterProvider(Func<Task<IList<Uri>>> replicasProvider) =>
            this.replicasProvider = replicasProvider;

        /// <inheritdoc />
        public Task<IList<Uri>> GetClusterAsync() =>
            replicasProvider();
    }
}