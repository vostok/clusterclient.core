using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Core.Transforms;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Clusterclient.Core.Tests.Helpers;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.Clusterclient.Core.Tests
{
    [TestFixture]
    internal class ClusterClient_Tests
    {
        private ILog log;

        [SetUp]
        public void TestSetup()
        {
            log = new SynchronousConsoleLog();
        }

        [Test]
        public void Ctor_should_throw_an_error_when_created_with_incorrect_configuration()
        {
            Action action = () => new ClusterClient(log, _ => {});

            action.Should().Throw<ClusterClientException>().Which.ShouldBePrinted();
        }

        [Test]
        public void Should_use_cluster_provider_as_is_when_there_is_no_replicas_transform()
        {
            var clusterProvider = Substitute.For<IClusterProvider>();

            var clusterClient = new ClusterClient(
                log,
                config =>
                {
                    config.ClusterProvider = clusterProvider;
                    config.Transport = Substitute.For<ITransport>();
                });

            clusterClient.ClusterProvider.Should().BeSameAs(clusterProvider);
            clusterClient.AsyncClusterProvider.Should().BeNull();
        }

        [Test]
        public void Should_wrap_cluster_provider_with_transforming_facade_if_there_is_a_replicas_transform()
        {
            var clusterClient = new ClusterClient(
                log,
                config =>
                {
                    config.ClusterProvider = Substitute.For<IClusterProvider>();
                    config.Transport = Substitute.For<ITransport>();
                    config.ReplicaTransform = Substitute.For<IReplicaTransform>();
                });

            clusterClient.ClusterProvider.Should().BeOfType<TransformingClusterProvider>();
            clusterClient.AsyncClusterProvider.Should().BeNull();
        }
        
        [Test]
        public async Task Should_wrap_async_cluster_provider()
        {
            IList<Uri> replicas = new[] {new Uri("http://kontur.ru/1"), new Uri("http://kontur.ru/2")};
            var clusterProvider = new AdHocAsyncClusterProvider(() => Task.FromResult<IList<Uri>>(replicas));

            var clusterClient = new ClusterClient(
                log,
                config =>
                {
                    config.AsyncClusterProvider = clusterProvider;
                    config.Transport = Substitute.For<ITransport>();
                });

            clusterClient.AsyncClusterProvider.Should().BeSameAs(clusterProvider);
            clusterClient.ClusterProvider.Should().NotBeNull();

            (await clusterClient.AsyncClusterProvider.GetClusterAsync()).Should().BeEquivalentTo(replicas);
            clusterClient.ClusterProvider.GetCluster().Should().BeEquivalentTo(replicas);
        }

        [Test]
        public void Should_log_error_when_setup_throttling_and_target_service_and_environment_not_defined()
        {
            var messageTemplate = "";
            log = Substitute.For<ILog>();
            log.IsEnabledFor(LogLevel.Error).Returns(true);
            log.When(l => l.Log(Arg.Any<LogEvent>()))
                .Do(info => messageTemplate = info.Arg<LogEvent>().MessageTemplate);
            
            var _ = new ClusterClient(log,
                configuration =>
                {
                    configuration.Transport = Substitute.For<ITransport>();
                    configuration.ClusterProvider = new FixedClusterProvider("https://test.ru");
                    configuration.SetupAdaptiveThrottling();
                });

            log.Received(1).Log(Arg.Is<LogEvent>(e => e.Level == LogLevel.Error));
            messageTemplate.Should().Match("Incorrect client configuration.*");
        }
        
        [Test]
        public void Should_log_error_when_setup_replica_budgeting_and_target_service_and_environment_not_defined()
        {
            var messageTemplate = "";
            log = Substitute.For<ILog>();
            log.IsEnabledFor(LogLevel.Error).Returns(true);
            log.When(l => l.Log(Arg.Any<LogEvent>()))
                .Do(info => messageTemplate = info.Arg<LogEvent>().MessageTemplate);
            
            var _ = new ClusterClient(log,
                configuration =>
                {
                    configuration.Transport = Substitute.For<ITransport>();
                    configuration.ClusterProvider = new FixedClusterProvider("https://test.ru");
                    configuration.SetupReplicaBudgeting();
                });

            log.Received(1).Log(Arg.Is<LogEvent>(e => e.Level == LogLevel.Error));
            messageTemplate.Should().Match("Incorrect client configuration.*");
        }

        [Test]
        public void Should_set_different_keys_to_adaptive_throttling_storage()
        {
            var firstConfiguration = (IClusterClientConfiguration)null;
            var secondConfiguration =  (IClusterClientConfiguration)null;
            
            var _ = new ClusterClient(log,
                configuration =>
                {
                    configuration.Transport = Substitute.For<ITransport>();
                    configuration.ClusterProvider = new FixedClusterProvider("https://test.ru");
                    configuration.SetupAdaptiveThrottling();

                    firstConfiguration = configuration;
                });

            _ = new ClusterClient(log,
                configuration =>
                {
                    configuration.Transport = Substitute.For<ITransport>();
                    configuration.ClusterProvider = new FixedClusterProvider("https://test.ru");
                    configuration.SetupAdaptiveThrottling();

                    secondConfiguration = configuration;
                });

            var firstThrottling = (AdaptiveThrottlingModule)firstConfiguration.Modules.First()
                .Value.Before[0];
            
            var secondThrottling = (AdaptiveThrottlingModule)secondConfiguration.Modules.First()
                .Value.Before[0];

            firstThrottling.Options.StorageKey.Should().NotBe(secondThrottling.Options.StorageKey);
        }
        
        [Test]
        public void Should_set_different_keys_to_replica_budgeting_storage()
        {
            var firstConfiguration = (IClusterClientConfiguration)null;
            var secondConfiguration =  (IClusterClientConfiguration)null;
            
            var _ = new ClusterClient(log,
                configuration =>
                {
                    configuration.Transport = Substitute.For<ITransport>();
                    configuration.ClusterProvider = new FixedClusterProvider("https://test.ru");
                    configuration.SetupReplicaBudgeting();

                    firstConfiguration = configuration;
                });

            _ = new ClusterClient(log,
                configuration =>
                {
                    configuration.Transport = Substitute.For<ITransport>();
                    configuration.ClusterProvider = new FixedClusterProvider("https://test.ru");
                    configuration.SetupReplicaBudgeting();

                    secondConfiguration = configuration;
                });

            var firstThrottling = (ReplicaBudgetingModule)firstConfiguration.Modules.First()
                .Value.Before[0];
            
            var secondThrottling = (ReplicaBudgetingModule)secondConfiguration.Modules.First()
                .Value.Before[0];

            firstThrottling.Options.StorageKey.Should().NotBe(secondThrottling.Options.StorageKey);
        }
    }
}
