using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Commons.Collections;
using Vostok.Commons.Threading;

namespace Vostok.Clusterclient.Core.Ordering.Weighed
{
    /// <summary>
    /// <para>Represents a probabilistic ordering which orders replicas based on their weights.</para>
    /// <para>A weight is a number in customizable <c>[min-weight; max-weight]</c> range where <c>min-weight >= 0</c>. </para>
    /// <para>Weight for each replica is calculated by taking a constant initial value and modifying it with a chain of <see cref="IReplicaWeightModifier"/>s. Range limits are enforced after applying each modifier.</para>
    /// <para>Weights are used to compute replica selection probabilities. A replica with weight = <c>1.0</c> is twice more likely to come first than a replica with weight = <c>0.5</c>.</para>
    /// <para>If all replicas have same weight, the ordering essentially becomes random.</para>
    /// <para>Replicas with weight = <see cref="double.PositiveInfinity"/> come first regardless of other replica weights.</para>
    /// <para>Replicas with weight = <c>0</c> come last regardless of other replica weights.</para>
    /// <para>Developer's goal is to manipulate replica weights via <see cref="IReplicaWeightModifier"/>s to produce best possible results.</para>
    /// <para>Feedback from <see cref="Learn"/> method is forwarded to all modifiers.</para>
    /// </summary>
    [PublicAPI]
    public class WeighedReplicaOrdering : IReplicaOrdering
    {
        private const int PooledArraySize = 25;

        private static readonly UnboundedObjectPool<ArrayElement[]> Arrays = new(() => new ArrayElement[PooledArraySize]);

        private readonly IList<IReplicaWeightModifier> modifiers;
        private readonly IReplicaWeightCalculator weightCalculator;

        /// <param name="modifiers">A chain of <see cref="IReplicaWeightModifier"/> which will be used to modify replica weight.</param>
        /// <param name="minimumWeight">A minimal possible weight of replica. This parameter is optional and has default value <see cref="ClusterClientDefaults.MinimumReplicaWeight"/>.</param>
        /// <param name="maximumWeight">A maximal possible weight of replica. This parameter is optional and has default value <see cref="ClusterClientDefaults.MaximumReplicaWeight"/>.</param>
        /// <param name="initialWeight">A initial weight of replica. This parameter is optional and has default value <see cref="ClusterClientDefaults.InitialReplicaWeight"/>.</param>
        public WeighedReplicaOrdering(
            [NotNull] IList<IReplicaWeightModifier> modifiers,
            double minimumWeight = ClusterClientDefaults.MinimumReplicaWeight,
            double maximumWeight = ClusterClientDefaults.MaximumReplicaWeight,
            double initialWeight = ClusterClientDefaults.InitialReplicaWeight)
            : this(modifiers, new ReplicaWeightCalculator(modifiers, minimumWeight, maximumWeight, initialWeight))
        {
        }

        internal WeighedReplicaOrdering(
            [NotNull] IList<IReplicaWeightModifier> modifiers,
            [NotNull] IReplicaWeightCalculator weightCalculator)
        {
            this.modifiers = modifiers;
            this.weightCalculator = weightCalculator;
        }

        /// <inheritdoc />
        public void Learn(ReplicaResult result, IReplicaStorageProvider storageProvider)
        {
            foreach (var modifier in modifiers)
                modifier.Learn(result, storageProvider);
        }

        /// <inheritdoc />
        public IEnumerable<Uri> Order(IList<Uri> replicas, IReplicaStorageProvider storageProvider, Request request, RequestParameters parameters)
        {
            if (replicas.Count < 2)
                return replicas;

            return replicas.Count > PooledArraySize 
                ? OrderInternal(replicas, storageProvider, request, parameters, new ArrayElement[replicas.Count]) 
                : OrderUsingPooledArray(replicas, storageProvider, request, parameters);
        }

        private IEnumerable<Uri> OrderUsingPooledArray(IList<Uri> replicas, IReplicaStorageProvider storageProvider, Request request, RequestParameters parameters)
        {
            using (Arrays.Acquire(out var array))
                foreach (var replica in OrderInternal(replicas, storageProvider, request, parameters, array))
                    yield return replica;
        }

        private IEnumerable<Uri> OrderInternal(IList<Uri> replicas, IReplicaStorageProvider storageProvider, Request request, RequestParameters parameters, ArrayElement[] array)
        {
            List<Uri> replicasWithInfiniteWeight = null;
            List<Uri> replicasWithZeroWeight = null;

            var count = 0;
            var weightsSum = 0.0;
            foreach (var replica in replicas)
            {
                var weight = weightCalculator.GetWeight(replica, replicas, storageProvider, request, parameters);
                if (weight < 0.0)
                    throw new BugcheckException($"A negative weight has been calculated for replica '{replica}': {weight}.");

                // (iloktionov): Бесконечности портят расчёты на дереве, поэтому они обрабатываются отдельно и не вставляются в него:
                if (double.IsPositiveInfinity(weight))
                {
                    (replicasWithInfiniteWeight ??= new List<Uri>()).Add(replica);
                    continue;
                }

                // (iloktionov): Чтобы избежать детерминированного упорядочивания реплик с нулевым весом, их тоже придётся рассмотреть отдельно:
                if (weight < double.Epsilon)
                {
                    (replicasWithZeroWeight ??= new List<Uri>()).Add(replica);
                    continue;
                }

                // (iloktionov): Заполняем листовую ноду дерева:
                array[count++] = new ArrayElement
                {
                    Exists = true,
                    Weight = weight,
                    Replica = replica
                };
                weightsSum += weight;
            }

            // (iloktionov): Реплики с бесконечным весом должны иметь безусловный приоритет, при этом случайно переупорядочиваясь между собой:
            if (replicasWithInfiniteWeight != null)
            {
                Shuffle(replicasWithInfiniteWeight);

                foreach (var replica in replicasWithInfiniteWeight)
                    yield return replica;
            }

            for (var i = 0; i < count; i++)
            {
                var replica = SelectReplicaFromArray(count, array, weightsSum);
                yield return array[replica].Replica;
                array[replica].Exists = false;
                weightsSum -= array[replica].Weight;
            }

            // (iloktionov): Реплики с нулевым весом должны идти последними, при этом случайно переупорядочиваясь между собой:
            if (replicasWithZeroWeight != null)
            {
                Shuffle(replicasWithZeroWeight);

                foreach (var replica in replicasWithZeroWeight)
                    yield return replica;
            }
        }
        
        private static int SelectReplicaFromArray(int count, ArrayElement[] array, double weightsSum)
        {
            weightsSum *= ThreadSafeRandom.NextDouble();
            var result = -1;
            
            // (kungurtsev): Даже если weightsSum = 0 мы должны вернуть ещё существующий элемент:
            for (var i = 0; i < count && (weightsSum > 0 || result == -1); i++)
            {
                if (array[i].Exists)
                {
                    weightsSum -= array[i].Weight;
                    result = i;
                }
            }
            
            if (result == -1)
                throw new BugcheckException("Result is -1. Surely, this is a bug in code.");

            return result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Shuffle(List<Uri> replicas)
        {
            for (var i = 0; i < replicas.Count - 1; i++)
                Swap(replicas, i, ThreadSafeRandom.Next(i, replicas.Count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(List<Uri> replicas, int i, int j)
        {
            (replicas[i], replicas[j]) = (replicas[j], replicas[i]);
        }

        private struct ArrayElement
        {
            public bool Exists;
            public double Weight;
            public Uri Replica;
        }
    }
}