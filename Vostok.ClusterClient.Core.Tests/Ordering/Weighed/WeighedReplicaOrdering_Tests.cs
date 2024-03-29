﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Clusterclient.Core.Ordering.Weighed;

namespace Vostok.Clusterclient.Core.Tests.Ordering.Weighed
{
    [TestFixture]
    internal class WeighedReplicaOrdering_Tests
    {
        private Uri replica1;
        private Uri replica2;
        private Uri replica3;
        private Uri replica4;
        private Uri replica5;
        private Uri[] replicas;

        private Request request;
        private RequestParameters parameters;
        private List<IReplicaWeightModifier> modifiers;
        private IReplicaWeightCalculator weightCalculator;
        private IReplicaStorageProvider storageProvider;
        private WeighedReplicaOrdering ordering;

        [SetUp]
        public void TestSetup()
        {
            request = Request.Get("foo/bar");
            parameters = RequestParameters.Empty;

            replicas = new[]
            {
                replica1 = new Uri("http://replica-1"),
                replica2 = new Uri("http://replica-2"),
                replica3 = new Uri("http://replica-3"),
                replica4 = new Uri("http://replica-4"),
                replica5 = new Uri("http://replica-5")
            };

            modifiers = new List<IReplicaWeightModifier>
            {
                Substitute.For<IReplicaWeightModifier>(),
                Substitute.For<IReplicaWeightModifier>(),
                Substitute.For<IReplicaWeightModifier>()
            };

            storageProvider = Substitute.For<IReplicaStorageProvider>();

            weightCalculator = Substitute.For<IReplicaWeightCalculator>();
            weightCalculator.GetWeight(null, null, null, null, null).ReturnsForAnyArgs(1.0);

            ordering = new WeighedReplicaOrdering(modifiers, weightCalculator);
        }

        [Test]
        public void Learn_should_forward_information_to_all_modifiers()
        {
            var result = new ReplicaResult(replicas.First(), Responses.Timeout, ResponseVerdict.Reject, TimeSpan.Zero);

            ordering.Learn(result, storageProvider);

            foreach (var modifier in modifiers)
            {
                modifier.Received(1).Learn(result, storageProvider);
            }
        }

        [Test]
        public void Order_should_calculate_weight_for_each_replica()
        {
            Order().Count();

            foreach (var replica in replicas)
            {
                weightCalculator.Received(1).GetWeight(replica, replicas, storageProvider, request, parameters);
            }
        }

        [Test]
        public void Order_should_return_an_empty_replicas_list_as_is()
        {
            replicas = new Uri[] {};

            Order().Should().BeSameAs(replicas);
        }

        [Test]
        public void Order_should_return_a_list_with_single_replica_as_is()
        {
            replicas = new[] {replicas.First()};

            Order().Should().BeSameAs(replicas);
        }

        [Test]
        public void Order_should_return_all_input_replicas()
        {
            for (var i = 1; i <= 100; i++)
            {
                replicas = Enumerable.Range(0, i).Select(j => new Uri($"http://foo/bar/{j}")).ToArray();

                Order().Should().BeEquivalentTo((IEnumerable<Uri>) replicas);
            }
        }

        [Test]
        public void Order_should_produce_a_uniform_random_distribution_when_all_weights_are_equal()
        {
            var distribution = ComputeDistribution(100 * 1000);

            distribution.Should().HaveCount(replicas.Length);

            foreach (var value in distribution.Values)
            {
                value.Should().BeInRange(18000, 22000);
            }
        }

        [Test]
        public void Order_should_exhibit_a_linear_dependency_between_weights_and_the_probability_of_replica_being_chosen_first()
        {
            SetupWeight(replica1, 0.2);
            SetupWeight(replica2, 0.4);
            SetupWeight(replica3, 0.6);
            SetupWeight(replica4, 0.8);
            SetupWeight(replica5, 1.0);

            var distribution = ComputeDistribution(100 * 1000);

            distribution.Should().HaveCount(replicas.Length);

            distribution[replica1].Should().BeInRange(5000, 8000);
            distribution[replica2].Should().BeInRange(12000, 15000);
            distribution[replica3].Should().BeInRange(18000, 22000);
            distribution[replica4].Should().BeInRange(25000, 28000);
            distribution[replica5].Should().BeInRange(32000, 35000);
        }
        
        [Test]
        public void Order_should_exhibit_a_linear_dependency_between_weights_and_the_probability_of_replica_being_chosen_second()
        {
            SetupWeight(replica1, 0.2);
            SetupWeight(replica2, 0.4);
            SetupWeight(replica3, 0.6);
            SetupWeight(replica4, 0.8);
            SetupWeight(replica5, 1.0);

            var distribution = new Dictionary<Uri, double>();
            var count = 10_000;
            
            for (var i = 0; i < count; i++)
            {
                // note (kungurtsev, 01.03.2022): forces replica2 to be extracted first
                var ordered = Order().Take(2).ToList();
                while (ordered[0] != replica2)
                    ordered = Order().Take(2).ToList();

                if (distribution.ContainsKey(ordered[1]))
                {
                    distribution[ordered[1]]++;
                }
                else
                {
                    distribution[ordered[1]] = 1;
                }
            }

            distribution.Should().HaveCount(replicas.Length - 1);

            var (p1, p3, p4, p5) = (distribution[replica1] / count, distribution[replica3] / count, distribution[replica4] / count, distribution[replica5] / count);

            // note (kungurtsev, 01.03.2022): all replicas except replica2 should have corresponding probabilities
            // 2.6 = 0.2 + 0.6 + 0.8 + 1.0
            p1.Should().BeApproximately(0.2 / 2.6, 0.1);
            p3.Should().BeApproximately(0.6 / 2.6, 0.1);
            p4.Should().BeApproximately(0.8 / 2.6, 0.1);
            p5.Should().BeApproximately(1.0 / 2.6, 0.1);
        }

        [Test]
        public void Order_should_give_priority_to_replicas_with_infinite_weight()
        {
            SetupWeight(replica5, double.PositiveInfinity);

            for (var i = 0; i < 100; i++)
            {
                Order().First().Should().Be(replica5);
            }
        }

        [Test]
        public void Order_should_give_out_replicas_with_zero_weight_last()
        {
            SetupWeight(replica1, 0);

            for (var i = 0; i < 100; i++)
            {
                Order().Last().Should().Be(replica1);
            }
        }

        [Test]
        public void Order_should_randomly_reorder_replicas_with_infinite_weight()
        {
            SetupWeight(replica4, double.PositiveInfinity);
            SetupWeight(replica5, double.PositiveInfinity);

            var distribution = ComputeDistribution(100 * 1000);

            distribution.Should().HaveCount(2);

            distribution[replica4].Should().BeInRange(40000, 60000);
            distribution[replica5].Should().BeInRange(40000, 60000);
        }

        [Test]
        public void Order_should_randomly_reorder_replicas_with_zero_weight()
        {
            foreach (var replica in replicas)
            {
                SetupWeight(replica, 0);
            }

            var distribution = ComputeDistribution(100 * 1000);

            distribution.Should().HaveCount(replicas.Length);

            foreach (var value in distribution.Values)
            {
                value.Should().BeInRange(18000, 22000);
            }
        }

        [Test]
        public void Order_should_not_mess_up_when_called_concurrently()
        {
            using (var result1 = Order().GetEnumerator())
            using (var result2 = Order().GetEnumerator())
            using (var result3 = Order().GetEnumerator())
            {
                for (var i = 0; i < replicas.Length; i++)
                {
                    result1.MoveNext().Should().BeTrue();
                    result2.MoveNext().Should().BeTrue();
                    result3.MoveNext().Should().BeTrue();
                }
            }
        }

        private void SetupWeight(Uri replica, double weight)
        {
            weightCalculator.GetWeight(replica, Arg.Any<IList<Uri>>(), storageProvider, request, parameters).Returns(weight);
        }

        private IEnumerable<Uri> Order()
        {
            return ordering.Order(replicas, storageProvider, request, parameters);
        }

        private Dictionary<Uri, int> ComputeDistribution(int iterations)
        {
            var distribution = new Dictionary<Uri, int>();

            for (var i = 0; i < iterations; i++)
            {
                var firstReplica = Order().First();

                if (distribution.ContainsKey(firstReplica))
                {
                    distribution[firstReplica]++;
                }
                else
                {
                    distribution[firstReplica] = 1;
                }
            }

            return distribution;
        }
    }
}