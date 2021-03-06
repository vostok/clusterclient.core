﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Ordering.Weighed.Adaptive
{
    /// <summary>
    /// <para>Represents a weight modifier which uses a concept of replica health that dynamically increases and decreases in response to replica behaviour.</para>
    /// <para>The exact nature of health and its application to weight is defined by an <see cref="IAdaptiveHealthImplementation{THealth}"/> instance.</para>
    /// <para>The actions to be taken on replica health in response to observed <see cref="ReplicaResult"/>s are defined by <see cref="IAdaptiveHealthTuningPolicy"/> instance.</para>
    /// </summary>
    /// <typeparam name="THealth">Type of health values used in <see cref="IAdaptiveHealthImplementation{THealth}"/>.</typeparam>
    [PublicAPI]
    public class AdaptiveHealthModifier<THealth> : IReplicaWeightModifier
    {
        private readonly IAdaptiveHealthImplementation<THealth> implementation;
        private readonly IAdaptiveHealthTuningPolicy tuningPolicy;
        private readonly ILog log;
        private readonly string storageKey;

        public AdaptiveHealthModifier(IAdaptiveHealthImplementation<THealth> implementation, IAdaptiveHealthTuningPolicy tuningPolicy, ILog log)
        {
            this.implementation = implementation;
            this.tuningPolicy = tuningPolicy;
            this.log = log ?? new SilentLog();

            storageKey = implementation.GetType().FullName;
        }

        /// <inheritdoc />
        public void Modify(Uri replica, IList<Uri> allReplicas, IReplicaStorageProvider storageProvider, Request request, RequestParameters parameters, ref double weight)
        {
            if (storageProvider.Obtain<THealth>(storageKey).TryGetValue(replica, out var currentHealth))
                implementation.ModifyWeight(currentHealth, ref weight);
        }

        /// <inheritdoc />
        public void Learn(ReplicaResult result, IReplicaStorageProvider storageProvider)
        {
            var storage = storageProvider.Obtain<THealth>(storageKey);

            while (true)
            {
                THealth newHealth;
                bool foundHealth;

                if (!(foundHealth = storage.TryGetValue(result.Replica, out var currentHealth)))
                    currentHealth = implementation.CreateDefaultHealth();

                switch (tuningPolicy.SelectAction(result))
                {
                    case AdaptiveHealthAction.Increase:
                        newHealth = implementation.IncreaseHealth(currentHealth);
                        break;

                    case AdaptiveHealthAction.Decrease:
                        newHealth = implementation.DecreaseHealth(currentHealth);
                        break;

                    default:
                        newHealth = currentHealth;
                        break;
                }

                if (implementation.AreEqual(currentHealth, newHealth))
                    break;

                var updatedHealth = foundHealth
                    ? storage.TryUpdate(result.Replica, newHealth, currentHealth)
                    : storage.TryAdd(result.Replica, newHealth);

                if (updatedHealth)
                {
                    implementation.LogHealthChange(result.Replica, currentHealth, newHealth, log);
                    break;
                }
            }
        }
    }
}