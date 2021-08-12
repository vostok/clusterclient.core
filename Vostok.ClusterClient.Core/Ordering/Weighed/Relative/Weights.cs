using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Ordering.Weighed.Relative.Interfaces;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Relative
{
    internal class Weights : IWeights
    {
        private volatile Dictionary<Uri, Weight> currentWeights =
            new Dictionary<Uri, Weight>();

        public Weight? Get(Uri replica, TimeSpan weightsTTL) =>
            currentWeights.TryGetValue(replica, out var weight)
                ? DateTime.UtcNow - weight.Timestamp <= weightsTTL
                    ? weight
                    : (Weight?)null
                : null;

        public void Update(IReadOnlyDictionary<Uri, Weight> updatedWeights, RelativeWeightSettings settings)
        {
            var newReplicas = new HashSet<Uri>(updatedWeights.Select(p => p.Key));
            var newWeights = new Dictionary<Uri, Weight>(updatedWeights.Count);
            var currentTime = DateTime.UtcNow;
            foreach (var (currentReplica, currentWeight) in currentWeights)
            {
                if (newReplicas.Contains(currentReplica))
                {
                    newWeights[currentReplica] = updatedWeights[currentReplica];
                    newReplicas.Remove(currentReplica);
                    continue;
                }

                if (currentTime - currentWeight.Timestamp < settings.WeightsTTL)
                    newWeights[currentReplica] = ApplyRegenerationIfNeed(currentWeight, settings);
            }

            foreach (var newReplica in newReplicas)
                newWeights[newReplica] = updatedWeights[newReplica];

            currentWeights = newWeights;
        }

        private Weight ApplyRegenerationIfNeed(Weight weight, RelativeWeightSettings settings)
        {
            var ageMinutes = (DateTime.UtcNow - weight.Timestamp).TotalMinutes;
            ageMinutes = Math.Max(0, ageMinutes - settings.RegenerationLag.TotalMinutes);

            var regenerationAmount = Math.Max(0, ageMinutes * settings.RegenerationRatePerMinute);
            var newValue = Math.Min(1.0, weight.Value + regenerationAmount);
            return new Weight(newValue, weight.Timestamp);
        }

        public IEnumerator<KeyValuePair<Uri, Weight>> GetEnumerator() =>
            ((IEnumerable<KeyValuePair<Uri, Weight>>)currentWeights).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}