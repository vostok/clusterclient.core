using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules
{
    /// <summary>
    /// An implementation of adaptive client throttling mechanism described in https://landing.google.com/sre/book/chapters/handling-overload.html.
    /// </summary>
    internal class AdaptiveThrottlingModule : IRequestModule
    {
        private static readonly ConcurrentDictionary<string, CountersByPriority> Counters = new();
        private static readonly Stopwatch Watch = Stopwatch.StartNew();

        private readonly Func<string, CountersByPriority> counterFactory;

        public AdaptiveThrottlingModule(AdaptiveThrottlingOptions options)
        {
            this.Options = options;
            counterFactory = _ => new CountersByPriority(options.MinutesToTrack);
        }

        public static void ClearCache()
        {
            Counters.Clear();
        }

        public AdaptiveThrottlingOptions Options { get; }

        public int Requests(RequestPriority? priority) => GetCounter(priority).GetMetrics().Requests;

        public int Accepts(RequestPriority? priority) => GetCounter(priority).GetMetrics().Accepts;

        public double Ratio(RequestPriority? priority) => ComputeRatio(GetCounter(priority).GetMetrics());

        public double RejectionProbability(RequestPriority? priority) => ComputeRejectionProbability(GetCounter(priority).GetMetrics(), Options);

        public async Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            var counter = GetCounter(context.Parameters.Priority);

            counter.BeginRequest();

            ClusterResult result;

            try
            {
                counter.AddRequest();

                double ratio;
                double rejectionProbability;

                var metrics = counter.GetMetrics();
                if (metrics.Requests >= Options.MinimumRequests &&
                    (ratio = ComputeRatio(metrics)) >= Options.CriticalRatio &&
                    (rejectionProbability = ComputeRejectionProbability(metrics, Options)) > ThreadSafeRandom.NextDouble())
                {
                    LogThrottledRequest(context, ratio, rejectionProbability);

                    return ClusterResult.Throttled(context.Request);
                }

                result = await next(context).ConfigureAwait(false);

                UpdateCounter(counter, result);
            }
            catch (OperationCanceledException)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    counter.AddAccept();
                throw;
            }
            finally
            {
                counter.EndRequest();
            }

            return result;
        }

        private static double ComputeRatio(CounterMetrics metrics) =>
            1.0 * metrics.Requests / Math.Max(1.0, metrics.Accepts);

        private static double ComputeRejectionProbability(CounterMetrics metrics, AdaptiveThrottlingOptions options)
        {
            var probability = 1.0 * (metrics.Requests - options.CriticalRatio * metrics.Accepts) / (metrics.Requests + 1);

            probability = Math.Max(probability, 0.0);
            probability = Math.Min(probability, options.MaximumRejectProbability);

            return probability;
        }

        private static void UpdateCounter(Counter counter, ClusterResult result)
        {
            if (result.ReplicaResults.Any(r => r.Verdict == ResponseVerdict.Accept))
                counter.AddAccept();
        }

        private Counter GetCounter(RequestPriority? priority)
        {
            priority ??= RequestPriority.Sheddable;
            var counters = Counters.GetOrAdd(Options.StorageKey, counterFactory);
            return priority switch
            {
                RequestPriority.Critical => counters.CriticalRequestCounter,
                RequestPriority.Ordinary => counters.OrdinaryRequestCounter,
                RequestPriority.Sheddable => counters.SheddableRequestCounter,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
            };
        }

        #region CountersByPriority

        private class CountersByPriority
        {
            private readonly Counter criticalRequestCounter;
            private readonly Counter ordinaryRequestCounter;
            private readonly Counter sheddableRequestCounter;

            public CountersByPriority(int buckets)
            {
                criticalRequestCounter = new Counter(buckets);
                ordinaryRequestCounter = new Counter(buckets);
                sheddableRequestCounter = new Counter(buckets);
            }

            public Counter CriticalRequestCounter => criticalRequestCounter;
            public Counter OrdinaryRequestCounter => ordinaryRequestCounter;
            public Counter SheddableRequestCounter => sheddableRequestCounter;
        }

        #endregion
        
        #region Logging

        private void LogThrottledRequest(IRequestContext context, double ratio, double rejectionProbability) =>
            context.Log.Warn("Throttled request without sending it. Request/accept ratio = {RequestAcceptsRatio:F3}. Rejection probability = {RejectionProbability:F3}", ratio, rejectionProbability);

        #endregion

        #region CounterBucket

        private class CounterBucket
        {
            public volatile int Minute;
            public int Requests;
            public int Accepts;
        }

        #endregion

        #region Counter

        private class Counter
        {
            private readonly CounterBucket[] buckets;
            private int pendingRequests;

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
                    metrics.Accepts += bucket.Accepts;
                }

                metrics.Requests -= pendingRequests;

                return metrics;
            }

            public void BeginRequest() => Interlocked.Increment(ref pendingRequests);

            public void EndRequest() => Interlocked.Decrement(ref pendingRequests);

            public void AddRequest() => Interlocked.Increment(ref ObtainBucket().Requests);

            public void AddAccept() => Interlocked.Increment(ref ObtainBucket().Accepts);

            private static int GetCurrentMinute() => (int) Math.Floor(Watch.Elapsed.TotalMinutes);

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
            public int Accepts;
        }

        #endregion
    }
}