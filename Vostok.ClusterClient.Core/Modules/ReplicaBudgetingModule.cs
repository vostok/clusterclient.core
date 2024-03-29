using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules
{
    /// <summary>
    /// A module which limits replicas used per request to maintain sliding 'used-replicas/requests' ratio below given threshold.
    /// </summary>
    internal class ReplicaBudgetingModule : IRequestModule
    {
        private static readonly ConcurrentDictionary<string, Counter> Counters = new();
        private static readonly Stopwatch Watch = Stopwatch.StartNew();

        private readonly Func<string, Counter> counterFactory;

        public ReplicaBudgetingModule(ReplicaBudgetingOptions options)
        {
            Options = options;
            counterFactory = _ => new Counter(options.MinutesToTrack);
        }

        public static void ClearCache() => Counters.Clear();

        public ReplicaBudgetingOptions Options { get; }

        public int Requests => GetCounter().GetMetrics().Requests;

        public int Replicas => GetCounter().GetMetrics().Replicas;

        public double Ratio => ComputeRatio(GetCounter().GetMetrics());

        public async Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            var counter = GetCounter();

            double ratio;

            var metrics = counter.GetMetrics();
            if (metrics.Requests >= Options.MinimumRequests &&
                (ratio = ComputeRatio(counter.GetMetrics())) >= Options.CriticalRatio)
            {
                LogLimitingReplicasToUse(context, ratio);
                context.MaximumReplicasToUse = 1;
            }

            var result = await next(context).ConfigureAwait(false);

            counter.AddResult(result.ReplicaResults.Count);

            return result;
        }

        private static double ComputeRatio(CounterMetrics metrics) =>
            1.0 * metrics.Replicas / Math.Max(1.0, metrics.Requests);

        private Counter GetCounter() => Counters.GetOrAdd(Options.StorageKey, counterFactory);

        #region Logging

        private void LogLimitingReplicasToUse(IRequestContext context, double ratio) =>
            context.Log.Warn("Limiting max used replicas for request to 1 due to current replicas/requests ratio = {ReplicasRequestsRatio:F3}", ratio);

        #endregion

        #region CounterBucket

        private class CounterBucket
        {
            public volatile int Minute;
            public int Requests;
            public int Replicas;
        }

        #endregion

        #region Counter

        private class Counter
        {
            private readonly CounterBucket[] buckets;

            public Counter(int buckets)
            {
                this.buckets = new CounterBucket[buckets];

                for (var i = 0; i < buckets; i++)
                    this.buckets[i] = new CounterBucket();
            }

            public CounterMetrics GetMetrics()
            {
                var metrics = new CounterMetrics();
                var minute = GetCurrentMinute();

                foreach (var bucket in buckets)
                {
                    if (bucket.Minute <= minute - buckets.Length)
                        continue;

                    metrics.Requests += bucket.Requests;
                    metrics.Replicas += bucket.Replicas;
                }

                return metrics;
            }

            public void AddResult(int replicasCount)
            {
                var bucket = ObtainBucket();
                Interlocked.Increment(ref bucket.Requests);
                Interlocked.Add(ref bucket.Replicas, replicasCount);
            }

            private static int GetCurrentMinute() =>
                (int) Math.Floor(Watch.Elapsed.TotalMinutes);

            private CounterBucket ObtainBucket()
            {
                var minute = GetCurrentMinute();
                var bucketIndex = minute % buckets.Length;

                while (true)
                {
                    var currentBucket = buckets[bucketIndex];
                    if (currentBucket.Minute >= minute)
                        return currentBucket;

                    Interlocked.CompareExchange(ref buckets[bucketIndex], new CounterBucket {Minute = minute}, currentBucket);
                }
            }
        }

        #endregion

        #region CounterMetrics

        private struct CounterMetrics
        {
            public int Requests;
            public int Replicas;
        }

        #endregion
    }
}