﻿using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.ClusterClient.Abstractions.Model;
using Vostok.ClusterClient.Abstractions.Sending;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Core.Sending;
using Vostok.ClusterClient.Core.Strategies;
using Vostok.ClusterClient.Core.Strategies.TimeoutProviders;
using Vostok.ClusterClient.Core.Tests.Helpers;

namespace Vostok.ClusterClient.Core.Tests.Strategies
{
    [TestFixture]
    internal class SequentialRequestStrategy_Tests
    {
        private Uri replica1;
        private Uri replica2;
        private Uri replica3;
        private Uri[] replicas;
        private Request request;

        private ISequentialTimeoutsProvider timeoutsProvider;
        private IRequestSender sender;
        private CancellationTokenSource tokenSource;
        private CancellationToken token;

        private SequentialRequestStrategy strategy;

        [SetUp]
        public void TestSetup()
        {
            replica1 = new Uri("http://replica1/");
            replica2 = new Uri("http://replica2/");
            replica3 = new Uri("http://replica3/");
            replicas = new[] {replica1, replica2, replica3};

            request = Request.Get("/foo");

            timeoutsProvider = Substitute.For<ISequentialTimeoutsProvider>();
            timeoutsProvider.GetTimeout(null, null, 0, 0).ReturnsForAnyArgs(1.Seconds(), 2.Seconds(), 3.Seconds());

            sender = Substitute.For<IRequestSender>();
            SetupResult(replica1, ResponseVerdict.Reject);
            SetupResult(replica2, ResponseVerdict.Reject);
            SetupResult(replica3, ResponseVerdict.Reject);

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

            strategy = new SequentialRequestStrategy(timeoutsProvider);
        }

        [Test]
        public void Should_not_make_any_requests_when_time_budget_is_expired()
        {
            Send(Budget.Expired);

            sender.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public void Should_not_make_any_requests_when_request_body_stream_is_already_used()
        {
            var content = new SingleUseStreamContent(Stream.Null, 100);

            content.Stream.GetHashCode();

            request = Request.Post("foo/bar").WithContent(content);

            Send(Budget.Infinite);

            sender.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public void Should_consult_timeout_provider_for_each_replica_with_correct_parameters()
        {
            Send(Budget.Infinite);

            timeoutsProvider.ReceivedCalls().Should().HaveCount(3);
            timeoutsProvider.Received(1).GetTimeout(request, Budget.Infinite, 0, 3);
            timeoutsProvider.Received(1).GetTimeout(request, Budget.Infinite, 1, 3);
            timeoutsProvider.Received(1).GetTimeout(request, Budget.Infinite, 2, 3);
        }

        [Test]
        public void Should_send_request_to_each_replica_with_correct_timeout()
        {
            Send(Budget.Infinite);

            sender.ReceivedCalls().Should().HaveCount(3);
            sender.Received(1).SendToReplicaAsync(replica1, request, 1.Seconds(), token);
            sender.Received(1).SendToReplicaAsync(replica2, request, 2.Seconds(), token);
            sender.Received(1).SendToReplicaAsync(replica3, request, 3.Seconds(), token);
        }

        [Test]
        public void Should_limit_request_timeouts_by_remaining_time_budget()
        {
            Send(Budget.WithRemaining(1500.Milliseconds()));

            sender.ReceivedCalls().Should().HaveCount(3);
            sender.Received(1).SendToReplicaAsync(replica1, request, 1.Seconds(), token);
            sender.Received(1).SendToReplicaAsync(replica2, request, 1500.Milliseconds(), token);
            sender.Received(1).SendToReplicaAsync(replica3, request, 1500.Milliseconds(), token);
        }

        [Test]
        public void Should_stop_at_first_accepted_result()
        {
            SetupResult(replica2, ResponseVerdict.Accept);

            Send(Budget.Infinite);

            sender.ReceivedCalls().Should().HaveCount(2);
            sender.Received(1).SendToReplicaAsync(replica1, request, Arg.Any<TimeSpan>(), token);
            sender.Received(1).SendToReplicaAsync(replica2, request, Arg.Any<TimeSpan>(), token);
            sender.DidNotReceive().SendToReplicaAsync(replica3, request, Arg.Any<TimeSpan>(), token);
        }

        private void SetupResult(Uri replica, ResponseVerdict verdict)
        {
            sender
                .SendToReplicaAsync(replica, Arg.Any<Request>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                .ReturnsTask(new ReplicaResult(replica, new Response(ResponseCode.Ok), verdict, TimeSpan.Zero));
        }

        private void Send(IRequestTimeBudget budget)
        {
            strategy.SendAsync(request, sender, budget, replicas, replicas.Length, token).GetAwaiter().GetResult();
        }
    }
}
