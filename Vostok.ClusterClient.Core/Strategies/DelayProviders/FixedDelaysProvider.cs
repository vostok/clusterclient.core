﻿using System;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Strategies.DelayProviders
{
    /// <summary>
    /// Represents a delay provider which issues delays from a fixed set of values.
    /// </summary>
    [PublicAPI]
    public class FixedDelaysProvider : IForkingDelaysProvider
    {
        private readonly TimeSpan[] delays;
        private readonly TailDelayBehaviour tailBehaviour;

        public FixedDelaysProvider(TailDelayBehaviour tailBehaviour, [NotNull] params TimeSpan[] delays)
        {
            if (delays == null)
                throw new ArgumentNullException(nameof(delays));

            if (delays.Length == 0)
                throw new ArgumentException("At least one delay must be specified.", nameof(delays));

            this.delays = delays;
            this.tailBehaviour = tailBehaviour;
        }

        /// <inheritdoc />
        public TimeSpan? GetForkingDelay(Request request, IRequestTimeBudget budget, int currentReplicaIndex, int totalReplicas)
        {
            if (currentReplicaIndex < delays.Length)
                return delays[currentReplicaIndex];

            switch (tailBehaviour)
            {
                case TailDelayBehaviour.RepeatLastValue:
                    return delays.Last();
                case TailDelayBehaviour.RepeatAllValues:
                    return delays[currentReplicaIndex % delays.Length];
                default:
                    return null;
            }
        }

        /// <inheritdoc />
        public override string ToString() => "fixed";
    }
}