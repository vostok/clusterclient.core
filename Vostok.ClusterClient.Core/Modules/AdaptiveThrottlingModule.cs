using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Misc;
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

        private readonly IReadOnlyDictionary<RequestPriority, AdaptiveThrottlingOptions> options;
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
            this.options = options.Parameters; 
            counterFactory = _ => new CountersPerPriority(this.options);
            granularCounterFactory = _ => new CountersPerPriority(this.options);
        }

        public static void ClearCache()
        {
            Counters.Clear();
            GranularCounters.Clear();
        }

        public int Requests(RequestPriority? priority, ImmutableArrayDictionary<string, string> granularity = null) => GetCounter(priority, granularity).GetMetrics().Requests;

        public int Accepts(RequestPriority? priority, ImmutableArrayDictionary<string, string> granularity = null) => GetCounter(priority, granularity).GetMetrics().Accepts;

        public double Ratio(RequestPriority? priority, ImmutableArrayDictionary<string, string> granularity = null) => ComputeRatio(GetCounter(priority, granularity).GetMetrics());

        public double RejectionProbability(RequestPriority? priority, ImmutableArrayDictionary<string, string> granularity = null)
            => ComputeRejectionProbability(GetCounter(priority, granularity).GetMetrics(), GetCounter(priority).Options);

        [Obsolete("This property for adaptive throttling is obsolete. Instead use PerPriorityOptions.", false)]
        public AdaptiveThrottlingOptions Options => GetCounter(DefaultPriority).Options;

        public AdaptiveThrottlingOptions PerPriorityOptions(RequestPriority? priority) => GetCounter(priority).Options;

        public string StorageKey { get; }

        public async Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            var granularity = ExtractGranularity(context);

            var currentPriorityOptions = options.TryGetValue(context.Parameters.Priority ?? DefaultPriority, out var tmp) ? tmp : AdaptiveThrottlingOptions.Default;
            var counter = currentPriorityOptions.TrackGlobalStatistics ? GetCounter(context.Parameters.Priority) : null;
            var granularCounter = currentPriorityOptions.TrackGranularStatistics ? GetCounter(context.Parameters.Priority, granularity) : null;

            var metrics = counter?.GetMetrics();
            var granularMetrics = granularCounter?.GetMetrics();

            ClusterResult result;

            try
            {
                var random = ThreadSafeRandom.NextDouble();
                double globalRatio = 0;
                if (metrics.HasValue && TryReject(metrics.Value, currentPriorityOptions, random, out globalRatio, out var globalRejectionProbability))
                {
                    LogThrottledRequest(context, globalRatio, globalRejectionProbability);
                    UpdateCounter(counter, false);
                    UpdateCounter(granularCounter, false);

                    return ClusterResult.Throttled(context.Request);
                }

                double granularRatio = 0;
                if (granularMetrics.HasValue && TryReject(granularMetrics.Value, currentPriorityOptions, random, out granularRatio, out var granularRejectionProbability))
                {
                    LogThrottledRequest(context, granularRatio, granularRejectionProbability, granularity);

                    UpdateCounter(granularCounter, false);
                    if (counter is not null && !RejectStatisticsInsertion(granularMetrics, currentPriorityOptions, granularRatio, globalRatio))
                        UpdateCounter(counter, false);

                    return ClusterResult.Throttled(context.Request);
                }

                result = await next(context).ConfigureAwait(false);

                if (result.Status is ClusterResultStatus.ReplicasNotFound)
                    return result;

                var isAccept = IsAccept(result);
                UpdateCounter(granularCounter, isAccept);
                if (counter is not null && (isAccept || !RejectStatisticsInsertion(granularMetrics, currentPriorityOptions, granularRatio, globalRatio)))
                    UpdateCounter(counter, isAccept);

                return result;
            }
            catch (OperationCanceledException)
            {
                UpdateCounter(counter, context.CancellationToken.IsCancellationRequested);
                UpdateCounter(granularCounter, context.CancellationToken.IsCancellationRequested);
                throw;
            }
        }

        private static ImmutableArrayDictionary<string, string> ExtractGranularity(IRequestContext context) =>
            (context.Parameters.Properties.TryGetValue(RequestParametersStatisticsGranularityPropertyKey, out var granularity)
                ? granularity
                : null
            ) as ImmutableArrayDictionary<string, string>;

        private static bool TryReject(CounterMetrics metrics, AdaptiveThrottlingOptions options, double random, out double ratio, out double computedRejectionProbability)
        {
            ratio = 1d;
            computedRejectionProbability = 0d;
            return metrics.Requests >= options.MinimumRequests &&
                   (ratio = ComputeRatio(metrics)) >= options.CriticalRatio &&
                   (computedRejectionProbability = ComputeRejectionProbability(metrics, options)) > random;
        }

        private static bool RejectStatisticsInsertion(CounterMetrics? granularMetrics, AdaptiveThrottlingOptions options, double granularRatio, double globalRatio)
        {
            return granularMetrics.HasValue && granularMetrics.Value.Requests >= options.MinimumRequests &&
                   granularRatio / globalRatio > options.GranularToGlobalStatisticsRatioAnomalyThreshold &&
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

        private bool IsAccept(ClusterResult result) => result.Status is ClusterResultStatus.Success;

        private static void UpdateCounter(Counter counter, bool isAccept)
        {
            if (counter is null) return;
            counter.AddRequest();
            if (isAccept)
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
            if (!context.Log.IsEnabledFor(LogLevel.Warn)) return;

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

                return metrics;
            }

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

        private struct GranularKey : IEquatable<GranularKey>
        {
            private readonly string globalKey;
            private readonly ImmutableArrayDictionary<string, string> granularity;

            public GranularKey(string globalKey, ImmutableArrayDictionary<string, string> granularity)
            {
                this.globalKey = globalKey;
                this.granularity = granularity;
            }

            public override int GetHashCode()
            {
                var hash = 23;
                hash = unchecked(hash * 31 + globalKey.GetHashCode());
                hash = unchecked(hash * 31 + ImmutableArrayDictionaryByValueEqualityComparer<string, string>.Instance.GetHashCode(granularity));
                return hash;
            }

            public override bool Equals(object other) => other is GranularKey otherKey && Equals(otherKey);

            public bool Equals(GranularKey other) =>
                string.Equals(globalKey, other.globalKey) && ImmutableArrayDictionaryByValueEqualityComparer<string, string>.Instance.Equals(granularity, other.granularity);
        }

        #endregion
    }
}