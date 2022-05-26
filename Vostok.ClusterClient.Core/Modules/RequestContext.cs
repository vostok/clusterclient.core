using System;
using System.Collections.Generic;
using System.Threading;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules
{
    internal class RequestContext : IRequestContext
    {
        private readonly object resultsLock = new object();
        private List<ReplicaResult> results;
        private IClusterProvider clusterProvider;
        private IAsyncClusterProvider asyncClusterProvider;

        public RequestContext(
            Request request,
            RequestParameters parameters,
            IRequestTimeBudget budget,
            ILog log,
            IClusterProvider clusterProvider,
            IAsyncClusterProvider asyncClusterProvider,
            IReplicaOrdering replicaOrdering,
            ITransport transport,
            int maximumReplicasToUse,
            int connectionAttempts,
            string clientApplicationName = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            Budget = budget;
            Log = log;
            ClusterProvider = clusterProvider;
            AsyncClusterProvider = asyncClusterProvider;
            ReplicaOrdering = replicaOrdering;
            Transport = transport;
            Parameters = parameters;
            CancellationToken = cancellationToken;
            MaximumReplicasToUse = maximumReplicasToUse;
            ConnectionAttempts = connectionAttempts;
            ClientApplicationName = clientApplicationName;

            ResetReplicaResults();
        }

        public Request Request { get; set; }

        public IRequestTimeBudget Budget { get; }

        public ILog Log { get; }

        public IClusterProvider ClusterProvider
        {
            // note (kungurtsev, 26.05.2022): should be not null
            get =>
                clusterProvider ?? new SyncClusterProviderAdapter(asyncClusterProvider);
            set
            {
                clusterProvider = value;
                asyncClusterProvider = null;
            }
        }
        public IAsyncClusterProvider AsyncClusterProvider
        {
            // note (kungurtsev, 26.05.2022): may be null, then ClusterProvider not null
            get => asyncClusterProvider;
            set
            {
                asyncClusterProvider = value;
                clusterProvider = null;
            }
        }

        public IReplicaOrdering ReplicaOrdering { get; set; }

        public ITransport Transport { get; set; }

        public CancellationToken CancellationToken { get; }

        public int MaximumReplicasToUse { get; set; }

        public int ConnectionAttempts { get; set; }

        public string ClientApplicationName { get; }

        public RequestParameters Parameters { get; set; }

        public void SetUnknownResult(Uri replica)
        {
            lock (resultsLock)
            {
                if (results == null)
                    return;

                results.Add(CreateUnknownResult(replica));
            }
        }

        public void SetReplicaResult(ReplicaResult result)
        {
            lock (resultsLock)
            {
                if (results == null)
                    return;

                for (var i = 0; i < results.Count; i++)
                    if (results[i].Replica.Equals(result.Replica) && IsUnknownResult(results[i]))
                    {
                        results[i] = result;
                        return;
                    }

                results.Add(result);
            }
        }

        public List<ReplicaResult> FreezeReplicaResults()
        {
            lock (resultsLock)
            {
                var currentResults = results;
                results = null;
                return currentResults;
            }
        }

        public void ResetReplicaResults()
        {
            lock (resultsLock)
                results = new List<ReplicaResult>(2);
        }

        private static bool IsUnknownResult(ReplicaResult result)
            => ReferenceEquals(result.Response, Responses.Unknown) && result.Verdict == ResponseVerdict.DontKnow;

        private static ReplicaResult CreateUnknownResult(Uri replica) =>
            new ReplicaResult(replica, Responses.Unknown, ResponseVerdict.DontKnow, TimeSpan.Zero);
    }
}