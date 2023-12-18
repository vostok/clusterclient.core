using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Tests.Modules
{
    [TestFixture]
    internal class AdaptiveThrottlingModule_Tests
    {
        private const int MinimumRequests = 50;
        private const double CriticalRatio = 2.0;
        private const double ProbabilityCap = 0.8;

        private Uri replica;
        private Request request;
        private ClusterResult acceptedResult;
        private ClusterResult rejectedResult;
        private IRequestContext context;

        private AdaptiveThrottlingOptions options;
        private AdaptiveThrottlingModule module;

        [SetUp]
        public void TestSetup()
        {
            replica = new Uri("http://replica");
            request = Request.Get("foo/bar");
            acceptedResult = new ClusterResult(ClusterResultStatus.Success, new[] {new ReplicaResult(replica, new Response(ResponseCode.Accepted), ResponseVerdict.Accept, TimeSpan.Zero)}, null, request);
            rejectedResult = new ClusterResult(ClusterResultStatus.ReplicasExhausted, new[] {new ReplicaResult(replica, new Response(ResponseCode.TooManyRequests), ResponseVerdict.Reject, TimeSpan.Zero)}, null, request);

            context = Substitute.For<IRequestContext>();
            context.Log.Returns(new SilentLog());

            options = new AdaptiveThrottlingOptions(Guid.NewGuid().ToString(), 1, MinimumRequests, CriticalRatio, ProbabilityCap);
            module = new AdaptiveThrottlingModule(options);
        }

        [Test]
        public void Should_correctly_compute_rejection_probability_for_different_priority()
        {
            Accept(1, RequestPriority.Critical);
            Reject(1, RequestPriority.Ordinary);
            var criticalProbability = module.RejectionProbability(RequestPriority.Critical);
            var ordinaryProbability = module.RejectionProbability(RequestPriority.Ordinary);
            criticalProbability.Should().BeLessThan(ordinaryProbability);
        }

        [Test]
        public void Should_handle_null_priority_as_sheddable()
        {
            Accept(1);
            module.Accepts(RequestPriority.Sheddable).Should().Be(1);
            module.Requests(RequestPriority.Sheddable).Should().Be(1);
            Reject(1);
            module.Accepts(RequestPriority.Sheddable).Should().Be(1);
            module.Requests(RequestPriority.Sheddable).Should().Be(2);
        }

        [TestCase(RequestPriority.Critical)]
        [TestCase(RequestPriority.Ordinary)]
        [TestCase(RequestPriority.Sheddable)]
        public void Should_correctly_handle_request_by_priority(RequestPriority? priority)
        {
            var priorities = new[] {RequestPriority.Critical, RequestPriority.Ordinary, RequestPriority.Sheddable};

            Accept(1, priority);
            module.Accepts(priority).Should().Be(1);
            foreach (var p in priorities)
            {
                module.Requests(p).Should().Be(p == priority ? 1 : 0);
            }

            Reject(1, priority);
            module.Accepts(priority).Should().Be(1);
            foreach (var p in priorities)
            {
                module.Requests(p).Should().Be(p == priority ? 2 : 0);
            }
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_increment_according_to_priority_requests_and_accepts_on_accepted_results(RequestPriority? priority)
        {
            for (var i = 1; i <= 10; i++)
            {
                Execute(acceptedResult, priority);

                module.Requests(priority).Should().Be(i);
                module.Accepts(priority).Should().Be(i);
            }
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_increment_according_to_priority_requests_and_accepts_on_OperationCancelledException_when_request_is_cancelled_by_token(RequestPriority? priority)
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                context.CancellationToken.Returns(cts.Token);

                for (var i = 1; i <= 10; i++)
                {
                    Execute(new OperationCanceledException(), priority);

                    module.Requests(priority).Should().Be(i);
                    module.Accepts(priority).Should().Be(i);
                }
            }
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_increment_according_to_priority_only_requests_on_OperationCancelledException_when_token_is_not_signaled(RequestPriority? priority)
        {
            using (var cts = new CancellationTokenSource())
            {
                context.CancellationToken.Returns(cts.Token);

                for (var i = 1; i <= 10; i++)
                {
                    Execute(new OperationCanceledException(), priority);

                    module.Requests(priority).Should().Be(i);
                    module.Accepts(priority).Should().Be(0);
                }
            }
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_increment_according_to_priority_only_requests_on_rejected_results(RequestPriority? priority)
        {
            for (var i = 1; i <= 10; i++)
            {
                Execute(rejectedResult, priority);

                module.Requests(priority).Should().Be(i);
                module.Accepts(priority).Should().Be(0);
            }
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_correctly_compute_according_to_priority_requests_to_accepts_ratio(RequestPriority? priority)
        {
            module.Ratio(priority).Should().Be(0.0);

            Accept(10, priority);

            module.Ratio(priority).Should().Be(1.0);

            Reject(10, priority);

            module.Ratio(priority).Should().Be(2.0);
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_not_reject_according_to_priority_requests_until_minimum_count_is_reached(RequestPriority? priority)
        {
            for (var i = 0; i < MinimumRequests - 1; i++)
                Execute(rejectedResult, priority).Should().BeSameAs(rejectedResult);
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_honor_according_to_priority_rejection_probability_cap(RequestPriority? priority)
        {
            Accept(1, priority);

            Reject(100, priority);

            module.RejectionProbability(priority).Should().Be(ProbabilityCap);
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_increase_according_to_priority_rejection_probability_as_more_requests_are_rejected(RequestPriority? priority)
        {
            Accept(1, priority);
            Reject(1, priority);

            while (module.RejectionProbability(priority) < ProbabilityCap)
            {
                var previous = module.RejectionProbability(priority);

                Reject(1, priority);

                module.RejectionProbability(priority).Should().BeGreaterThan(previous);
            }
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_gradually_decrease_according_to_priority_rejection_probability_to_zero_after_requests_become_accepted_after_big_failure(RequestPriority? priority)
        {
            Accept(10, priority);

            while (module.RejectionProbability(priority) < ProbabilityCap)
            {
                Reject(1, priority);
            }

            for (var i = 0; i < 10 * 1000; i++)
            {
                Accept(1, priority);

                if (module.RejectionProbability(priority) <= 0.001)
                    Assert.Pass();
            }

            Assert.Fail("Rejection probability did not vanish after 10k accepts.");
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_reject_according_to_priority_with_throttled_result_when_rejection_probability_allows(RequestPriority? priority)
        {
            options = new AdaptiveThrottlingOptions(Guid.NewGuid().ToString(), 1, MinimumRequests, CriticalRatio, 1.0);
            module = new AdaptiveThrottlingModule(options);

            Accept(1, priority);

            while (module.RejectionProbability(priority) < 0.999)
                Reject(1, priority);

            for (var i = 0; i < 100; i++)
            {
                var requestsBefore = module.Requests(priority);
                var acceptsBefore = module.Accepts(priority);

                var result = Execute(acceptedResult, priority);
                if (result.Status == ClusterResultStatus.Throttled)
                {
                    module.Requests(priority).Should().Be(requestsBefore + 1);
                    module.Accepts(priority).Should().Be(acceptsBefore);
                    Assert.Pass();
                }
            }

            Assert.Fail("No requests were rejected in 100 attempts, which was highly expected.");
        }

        [TestCaseSource(nameof(PriorityCase))]
        [Explicit]
        public void Should_forget_according_to_priority_old_information_as_time_passes(RequestPriority? priority)
        {
            Accept(100, priority);
            Reject(100, priority);

            Thread.Sleep(1.Minutes() + 5.Seconds());

            module.Requests(priority).Should().Be(0);
            module.Accepts(priority).Should().Be(0);
        }

        [TestCaseSource(nameof(PriorityCase))]
        public void Should_not_account_according_to_priority_for_requests_still_in_progress(RequestPriority? priority)
        {
            var parameters = new RequestParameters(priority: priority);
            context.Parameters.Returns(parameters);

            var tcs = new TaskCompletionSource<ClusterResult>();

            var tasks = new List<Task<ClusterResult>>();

            for (var i = 0; i < 500; i++)
            {
                tasks.Add(module.ExecuteAsync(context, _ => tcs.Task));
            }

            module.Requests(priority).Should().Be(0);
            module.Accepts(priority).Should().Be(0);

            tcs.TrySetResult(rejectedResult);

            foreach (var task in tasks)
            {
                task.GetAwaiter().GetResult().Should().BeSameAs(rejectedResult);
            }

            module.Requests(priority).Should().Be(500);
            module.Accepts(priority).Should().Be(0);

            Console.Out.WriteLine(module.RejectionProbability);
        }

        public static object[] PriorityCase =
        {
            new object[] {null},
            new object[] {RequestPriority.Critical},
            new object[] {RequestPriority.Ordinary},
            new object[] {RequestPriority.Sheddable}
        };

        private void Accept(int count, RequestPriority? priority = null)
        {
            for (var i = 0; i < count; i++)
                Execute(acceptedResult, priority);
        }

        private void Reject(int count, RequestPriority? priority = null)
        {
            for (var i = 0; i < count; i++)
                Execute(rejectedResult, priority);
        }

        private ClusterResult Execute(ClusterResult result, RequestPriority? priority = null)
        {
            var parameters = new RequestParameters(priority: priority);
            context.Parameters.Returns(parameters);
            return module.ExecuteAsync(context, _ => Task.FromResult(result)).GetAwaiter().GetResult();
        }

        private void Execute<T>(T exception, RequestPriority? priority = null)
            where T : Exception
        {
            try
            {
                var parameters = new RequestParameters(priority: priority);
                context.Parameters.Returns(parameters);
                module.ExecuteAsync(context, _ => Task.FromException<ClusterResult>(exception)).GetAwaiter().GetResult();
            }
            catch (T)
            {
            }
        }
    }
}