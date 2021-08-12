﻿using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Ordering.Weighed.Relative;

namespace Vostok.Clusterclient.Core.Tests.Ordering.Weighed.Relative
{
    [TestFixture]
    public class ClusterState_Tests
    {
        private ClusterState clusterState;

        [SetUp]
        public void SetUp()
        {
            clusterState = new ClusterState();
        }

        [Test]
        public void Swap_should_create_new_raw_statistic()
        {
            var previousActiveStat = clusterState.CurrentStatistic;
            
            var _ = clusterState.SwapToNewRawStatistic();

            clusterState.CurrentStatistic.Should().NotBeSameAs(previousActiveStat);
        }
    }
}