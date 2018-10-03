﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Core.Ordering.Storage;
using Vostok.ClusterClient.Core.Ordering.Weighed;
using Vostok.ClusterClient.Core.Tests.Helpers;

namespace Vostok.ClusterClient.Core.Tests.Ordering.Weighed
{
    [TestFixture]
    internal class ReplicaWeightCalculator_Tests
    {
        private const double MinWeight = 0.0;
        private const double MaxWeight = 10.0;
        private const double InitialWeight = 1.0;

        private Uri replica;
        private IList<Uri> replicas;
        private Request request;
        private RequestParameters parameters;
        private IReplicaStorageProvider storageProvider;
        private List<IReplicaWeightModifier> modifiers;
        private ReplicaWeightCalculator calculator;

        [SetUp]
        public void TestSetup()
        {
            replica = new Uri("http://replica");
            replicas = new List<Uri> {replica};
            request = Request.Get("foo/bar");
            parameters = RequestParameters.Empty;
            modifiers = new List<IReplicaWeightModifier>();
            storageProvider = Substitute.For<IReplicaStorageProvider>();
            calculator = new ReplicaWeightCalculator(modifiers, MinWeight, MaxWeight, InitialWeight);
        }

        [Test]
        public void Ctor_should_throw_an_error_when_modifiers_list_is_null()
        {
            Action action = () => new ReplicaWeightCalculator(null, MinWeight, MaxWeight, InitialWeight);

            action.Should().Throw<ArgumentNullException>().Which.ShouldBePrinted();
        }

        [Test]
        public void Ctor_should_throw_an_error_when_minimum_weight_is_negative()
        {
            Action action = () => new ReplicaWeightCalculator(modifiers, -0.01, MaxWeight, InitialWeight);

            action.Should().Throw<ArgumentOutOfRangeException>().Which.ShouldBePrinted();
        }

        [Test]
        public void Ctor_should_throw_an_error_when_minimum_weight_is_greater_than_maximum_weight()
        {
            Action action = () => new ReplicaWeightCalculator(modifiers, MaxWeight, MinWeight, InitialWeight);

            action.Should().Throw<ArgumentException>().Which.ShouldBePrinted();
        }

        [Test]
        public void Ctor_should_throw_an_error_when_initial_weight_is_greater_than_maximum_weight()
        {
            Action action = () => new ReplicaWeightCalculator(modifiers, MinWeight, MaxWeight, MaxWeight + 1);

            action.Should().Throw<ArgumentOutOfRangeException>().Which.ShouldBePrinted();
        }

        [Test]
        public void Ctor_should_throw_an_error_when_initial_weight_is_less_than_minimum_weight()
        {
            Action action = () => new ReplicaWeightCalculator(modifiers, MinWeight, MaxWeight, MinWeight - 1);

            action.Should().Throw<ArgumentOutOfRangeException>().Which.ShouldBePrinted();
        }

        [Test]
        public void GetWeight_should_return_initial_weight_when_there_are_no_modifiers()
        {
            calculator.GetWeight(replica, replicas, storageProvider, request, parameters).Should().Be(InitialWeight);
        }

        [Test]
        public void GetWeight_should_call_all_weight_modifiers_in_order()
        {
            modifiers.Add(CreateModifier(w => w + 1));
            modifiers.Add(CreateModifier(w => w*2));
            modifiers.Add(CreateModifier(w => w + 3));

            calculator.GetWeight(replica, replicas, storageProvider, request, parameters).Should().Be(7.0);

            Received.InOrder(() =>
            {
                var w1 = InitialWeight;
                var w2 = w1 + 1;
                var w3 = w2*2;

                modifiers[0].Modify(replica, replicas, storageProvider, request, parameters, ref w1);
                modifiers[1].Modify(replica, replicas, storageProvider, request, parameters, ref w2);
                modifiers[2].Modify(replica, replicas, storageProvider, request, parameters, ref w3);
            });
        }

        [Test]
        public void GetWeight_should_enforce_weight_limits_between_all_modifier_invocations()
        {
            modifiers.Add(CreateModifier(w => 100.0));
            modifiers.Add(CreateModifier(w => -100.0));
            modifiers.Add(CreateModifier(w => w + 2));

            calculator.GetWeight(replica, replicas, storageProvider, request, parameters).Should().Be(2.0);

            Received.InOrder(() =>
            {
                var w1 = InitialWeight;
                var w2 = MaxWeight;
                var w3 = MinWeight;

                modifiers[0].Modify(replica, replicas, storageProvider, request, parameters, ref w1);
                modifiers[1].Modify(replica, replicas, storageProvider, request, parameters, ref w2);
                modifiers[2].Modify(replica, replicas, storageProvider, request, parameters, ref w3);
            });
        }

        [Test]
        public void GetWeight_should_convert_NaN_weight_to_zero()
        {
            calculator = new ReplicaWeightCalculator(modifiers, MinWeight, MaxWeight, double.NaN);

            calculator.GetWeight(replica, replicas, storageProvider, request, parameters).Should().Be(0);
        }

        private static IReplicaWeightModifier CreateModifier(Func<double, double> transform)
        {
            var modifier = Substitute.For<IReplicaWeightModifier>();

            var dummy = 0.0;

            modifier
                .WhenForAnyArgs(m => m.Modify(null, null, null, null, null, ref dummy))
                .Do(info => { info[4] = transform(info.Arg<double>()); });

            return modifier;
        }
    }
}
