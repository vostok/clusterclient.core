﻿using System;
using System.Collections.Generic;
using Vostok.Clusterclient.Core.Misc;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Relative
{
    internal class Weights
    {
        private readonly Dictionary<Uri, Weight> weights =
            new Dictionary<Uri, Weight>();

        public Weight? Get(Uri replica, TimeSpan ttl) =>
            weights.TryGetValue(replica, out var weight)
                ? DateTime.UtcNow - weight.Timestamp <= ttl 
                    ? weight 
                    : (Weight?)null 
                : null;

        public void Update(IReadOnlyDictionary<Uri, Weight> newWeights)
        {
            foreach (var (replica, weight) in newWeights)
                weights[replica] = weight;
        }
    }
}