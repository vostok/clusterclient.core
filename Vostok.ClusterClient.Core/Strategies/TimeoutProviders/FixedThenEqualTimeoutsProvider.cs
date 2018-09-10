﻿using System;
using JetBrains.Annotations;
using Vostok.ClusterClient.Abstractions.Model;
using Vostok.ClusterClient.Core.Model;

namespace Vostok.ClusterClient.Core.Strategies.TimeoutProviders
{
    /// <summary>
    /// Represents a timeout provider that combines a <see cref="FixedTimeoutsProvider"/> for first few requests and uses an <see cref="EqualTimeoutsProvider"/> for the rest of them.
    /// </summary>
    public class FixedThenEqualTimeoutsProvider : ISequentialTimeoutsProvider
    {
        private readonly FixedTimeoutsProvider fixedProvider;
        private readonly EqualTimeoutsProvider equalProvider;
        private readonly int fixedTimeoutsCount;

        public FixedThenEqualTimeoutsProvider(int tailDivisionFactor, [NotNull] params TimeSpan[] firstTimeouts)
        {
            equalProvider = new EqualTimeoutsProvider(tailDivisionFactor);
            fixedProvider = new FixedTimeoutsProvider(firstTimeouts);
            fixedTimeoutsCount = firstTimeouts.Length;
        }

        public TimeSpan GetTimeout(Request request, IRequestTimeBudget budget, int currentReplicaIndex, int totalReplicas) =>
            currentReplicaIndex < fixedTimeoutsCount
                ? fixedProvider.GetTimeout(request, budget, currentReplicaIndex, totalReplicas)
                : equalProvider.GetTimeout(request, budget, currentReplicaIndex - fixedTimeoutsCount, totalReplicas);

        public override string ToString() => $"{fixedProvider} + {equalProvider}";
    }
}