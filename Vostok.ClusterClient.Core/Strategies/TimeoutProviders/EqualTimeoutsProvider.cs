﻿using System;
using Vostok.ClusterClient.Abstractions.Model;
using Vostok.ClusterClient.Core.Helpers;
using Vostok.ClusterClient.Core.Model;
using Vostok.Commons.Helpers.Extensions;

namespace Vostok.ClusterClient.Core.Strategies.TimeoutProviders
{
    /// <summary>
    /// <para>Represents a timeout provider which divides time budget equally between several replicas (their count is called division factor).</para>
    /// <para>However, if any of the replicas does not fully use its time quanta, redistribution occurs for remaining replicas.</para>
    /// </summary>
    /// <example>
    /// Let's say we have a division factor = 3 and a time budget = 12 sec. Then we might observe following distribution patterns:
    /// <para>4 sec --> 4 sec --> 4 sec (all replicas use full timeout).</para>
    /// <para>3 sec --> 4.5 sec --> 4.5 sec (first replica failed prematurely, redistribution occured).</para>
    /// <para>1 sec --> 1 sec --> 10 sec (first two replicas failed prematurely, redistribution occured).</para>
    /// </example>
    public class EqualTimeoutsProvider : ISequentialTimeoutsProvider
    {
        private readonly int divisionFactor;

        public EqualTimeoutsProvider(int divisionFactor)
        {
            if (divisionFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(divisionFactor), "Division factor must be a positive number.");

            this.divisionFactor = divisionFactor;
        }

        public TimeSpan GetTimeout(Request request, IRequestTimeBudget budget, int currentReplicaIndex, int totalReplicas)
        {
            if (currentReplicaIndex >= divisionFactor)
                return budget.Remaining;

            var effectiveDivisionFactor = Math.Min(divisionFactor, totalReplicas) - currentReplicaIndex;

            return TimeSpanExtensions.Max(TimeSpan.Zero, budget.Remaining.Divide(effectiveDivisionFactor));
        }

        public override string ToString() => "equal-" + divisionFactor;
    }
}