using System;
using System.Collections.Generic;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Relative.Interfaces
{
    internal interface IWeights : IEnumerable<KeyValuePair<Uri, Weight>>
    {
        Weight? Get(Uri replica, TimeSpan weightsTTL);

        void Update(IReadOnlyDictionary<Uri, Weight> updatedWeights, RelativeWeightSettings settings);
    }
}