using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Collections;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules
{
    /// <summary>
    /// An implementation of adaptive client throttling mechanism described in https://landing.google.com/sre/book/chapters/handling-overload.html.
    /// </summary>
    internal class AdaptiveThrottlingModule : IRequestModule
    {
        internal const string RequestParametersStatisticsGranularityPropertyKey = "AdaptiveThrottlingModule.StatisticsGranularity";

        private static readonly RequestPriority DefaultPriority = RequestPriority.Ordinary;
        private static readonly ConcurrentDictionary<string, CountersPerPriority> Counters = new();
        private static readonly ConcurrentDictionary<GranularKey, CountersPerPriority> GranularCounters = new();
        private static readonly Stopwatch Watch = Stopwatch.StartNew();

        private readonly Func<string, CountersPerPriority> counterFactory;
        private readonly Func<GranularKey, CountersPerPriority> granularCounterFactory;

        [Obsolete("This constructor for adaptive throttling is obsolete. Instead use constructor with AdaptiveThrottlingOptionsPerRequest.", false)]
        public AdaptiveThrottlingModule(AdaptiveThrottlingOptions options)
            : this(
                AdaptiveThrottlingOptionsBuilder.Build(
                    setup => setup.WithDefaultOptions(options),
                    options.StorageKey
                )
            )
        {
        }

        public AdaptiveThrottlingModule(AdaptiveThrottlingOptionsPerPriority options)
        {
            StorageKey = options.StorageKey;
            counterFactory = _ => new CountersPerPriority(options.Parameters);
            granularCounterFactory = _ => new CountersPerPriority(options.Parameters);
        }

        public static void ClearCache()
        {
            Counters.Clear();
        }

        public int Requests(RequestPriority? priority) => GetCounter(priority).GetMetrics().Requests;

        public int Accepts(RequestPriority? priority) => GetCounter(priority).GetMetrics().Accepts;

        public double Ratio(RequestPriority? priority) => ComputeRatio(GetCounter(priority).GetMetrics());

        public double RejectionProbability(RequestPriority? priority) => ComputeRejectionProbability(GetCounter(priority).GetMetrics(), GetCounter(priority).Options);

        [Obsolete("This property for adaptive throttling is obsolete. Instead use PerPriorityOptions.", false)]
        public AdaptiveThrottlingOptions Options => GetCounter(DefaultPriority).Options;

        public AdaptiveThrottlingOptions PerPriorityOptions(RequestPriority? priority) => GetCounter(priority).Options;

        public string StorageKey { get; }

        public async Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            var granularity = GetGranularity(context);
            
            var counter = GetCounter(context.Parameters.Priority);
            var options = counter.Options;
            var granularCounter = options.TrackGranularStatistics ? GetCounter(context.Parameters.Priority, granularity) : null;

            var metrics = counter.GetMetrics();
            var granularMetrics = granularCounter?.GetMetrics();
            
            counter.BeginRequest();
            granularCounter?.BeginRequest();            

            ClusterResult result;

            try
            {
                granularCounter?.AddRequest();
                
                var random = ThreadSafeRandom.NextDouble();
                if (TryReject(metrics, options, random, out var globalRatio, out var globalRejectionProbability))
                {
                    LogThrottledRequest(context, globalRatio, globalRejectionProbability);
                    counter.AddRequest();

                    return ClusterResult.Throttled(context.Request);
                }

                double granularRatio = 0;
                if (granularMetrics.HasValue && TryReject(granularMetrics.Value, options, random, out granularRatio, out var granularRejectionProbability))
                {
                    LogThrottledRequest(context, granularRatio, granularRejectionProbability, granularity);
                    if (!RejectStatisticsInsertion(granularMetrics.Value.Requests, options, granularRatio, globalRatio))
                        counter.AddRequest();
                    return ClusterResult.Throttled(context.Request);
                }

                counter.AddRequest();

                result = await next(context).ConfigureAwait(false);

                if (granularMetrics.HasValue)
                {
                    UpdateCounter(granularCounter, result);
                    if (!RejectStatisticsInsertion(granularMetrics.Value.Requests, options, granularRatio, globalRatio))
                        UpdateCounter(counter, result);
                }
                else
                {
                    UpdateCounter(counter, result);
                }
            }
            catch (OperationCanceledException)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    counter.AddAccept();
                    granularCounter?.AddAccept();
                }
                throw;
            }
            finally
            {
                counter.EndRequest();
                granularCounter?.EndRequest();
            }

            return result;
        }

        private static ImmutableArrayDictionary<string, string> GetGranularity(IRequestContext context) =>
            (context.Parameters.Properties.TryGetValue(RequestParametersStatisticsGranularityPropertyKey, out var granularity)
                ? granularity
                : null
            ) as ImmutableArrayDictionary<string, string>;

        private static bool TryReject(CounterMetrics metrics, AdaptiveThrottlingOptions options, double random, out double ratio, out double computedRejectionProbability)
        {
            ratio = 1d; computedRejectionProbability = 0d;
            return metrics.Requests >= options.MinimumRequests &&
                   (ratio = ComputeRatio(metrics)) >= options.CriticalRatio &&
                   (computedRejectionProbability = ComputeRejectionProbability(metrics, options)) > random;
        }

        private static bool RejectStatisticsInsertion(int granularRequests, AdaptiveThrottlingOptions options, double granularRatio, double globalRatio)
        {
            Debug.Assert(globalRatio >= 1d && granularRatio >= 1d);
            return granularRequests >= options.MinimumRequests &&
                   granularRatio / globalRatio > options.AnomalousStatisticsThreshold &&
                   1 - globalRatio / granularRatio > ThreadSafeRandom.NextDouble();
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

        private Counter GetCounter(RequestPriority? priority, ImmutableArrayDictionary<string, string> granularity = null)
        {
            var counters = granularity == null
                ? Counters.GetOrAdd(StorageKey, counterFactory)
                : GranularCounters.GetOrAdd(new GranularKey(StorageKey, granularity), granularCounterFactory);
            return counters.GetCounterPerPriority(priority ?? DefaultPriority);
        }

        #region CountersPerPriority

        private class CountersPerPriority
        {
            private readonly Dictionary<RequestPriority, Counter> requestCounters;

            public CountersPerPriority(IReadOnlyDictionary<RequestPriority, AdaptiveThrottlingOptions> options)
            {
                requestCounters = new Dictionary<RequestPriority, Counter>();
                foreach (var parameters in options)
                {
                    requestCounters[parameters.Key] = new Counter(parameters.Value);
                }
            }

            public Counter GetCounterPerPriority(RequestPriority requestPriority)
            {
                return !requestCounters.TryGetValue(requestPriority, out var counter) ? null : counter;
            }
        }

        #endregion

        #region Logging

        private static void LogThrottledRequest(IRequestContext context, double ratio, double rejectionProbability)
        {
            context.Log.Warn(
                "Throttled {priority} request without sending it based on overall service statistics. " +
                "Request/accept ratio = {RequestAcceptsRatio:F3}. Rejection probability = {RejectionProbability:F3}",
                context.Parameters.Priority, ratio, rejectionProbability);
        }

        private static void LogThrottledRequest(IRequestContext context, double ratio, double rejectionProbability,
                                                ImmutableArrayDictionary<string, string> granularity)
        { 
            var builder = new StringBuilder();
            foreach (var pair in granularity)
                builder.Append(pair.Key).Append('=').Append(pair.Value).Append("; ");

            context.Log.Warn(
                "Throttled {priority} request without sending it based on granular service statistics for granularity \"{Granularity}\". " +
                "Request/accept ratio = {RequestAcceptsRatio:F3}. Rejection probability = {RejectionProbability:F3}.",
                context.Parameters.Priority, builder.ToString(), ratio, rejectionProbability);
        }

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

            public Counter(AdaptiveThrottlingOptions options)
            {
                Options = options;

                var bucketsNumber = options.MinutesToTrack;
                buckets = new CounterBucket[bucketsNumber];

                for (var i = 0; i < bucketsNumber; i++)
                    buckets[i] = new CounterBucket();
            }

            public AdaptiveThrottlingOptions Options { get; }

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

            private static int GetCurrentMinute() => (int)Math.Floor(Watch.Elapsed.TotalMinutes);

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

        #region GranularKey

        private record struct GranularKey
        {
            public string GlobalKey;
            public ImmutableArrayDictionary<string, string> Granularity;

            public GranularKey(string globalKey, ImmutableArrayDictionary<string, string> granularity)
            {
                GlobalKey = globalKey;
                Granularity = granularity;
            }

            public override int GetHashCode() =>
                unchecked(GlobalKey.GetHashCode() * 31 + Granularity.GetHashCode());

            public bool Equals(GranularKey? other) =>
                GlobalKey.Equals(other?.GlobalKey) && Granularity.Equals(other?.Granularity);
        }

        #endregion
    }
}