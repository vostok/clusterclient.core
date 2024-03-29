﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Tests.Modules
{
    [TestFixture]
    internal class RequestModuleChainBuilder_Tests
    {
        private IRequestModule module1;
        private IRequestModule module2;
        private IRequestModule module3;
        private IRequestModule module4;
        private IRequestContext context;
        private List<IRequestModule> calledModules;

        [SetUp]
        public void TestSetup()
        {
            module1 = Substitute.For<IRequestModule>();
            module2 = Substitute.For<IRequestModule>();
            module3 = Substitute.For<IRequestModule>();
            module4 = Substitute.For<IRequestModule>();

            calledModules = new List<IRequestModule>();

            module1.ExecuteAsync(null, null)
                .ReturnsForAnyArgs(
                    info =>
                    {
                        calledModules.Add(module1);
                        return info.Arg<Func<IRequestContext, Task<ClusterResult>>>()(info.Arg<IRequestContext>());
                    });

            module2.ExecuteAsync(null, null)
                .ReturnsForAnyArgs(
                    info =>
                    {
                        calledModules.Add(module2);
                        return info.Arg<Func<IRequestContext, Task<ClusterResult>>>()(info.Arg<IRequestContext>());
                    });

            module3.ExecuteAsync(null, null)
                .ReturnsForAnyArgs(
                    info =>
                    {
                        calledModules.Add(module3);
                        return info.Arg<Func<IRequestContext, Task<ClusterResult>>>()(info.Arg<IRequestContext>());
                    });

            module4.ExecuteAsync(null, null)
                .ReturnsForAnyArgs(
                    _ =>
                    {
                        calledModules.Add(module4);
                        return Task.FromResult(ClusterResult.UnexpectedException(context.Request));
                    });

            context = Substitute.For<IRequestContext>();
        }

        [Test]
        public void Should_build_a_delegate_which_invokes_all_modules_in_correct_order()
        {
            var chainDelegate = RequestModuleChainBuilder.BuildChainDelegate(new[] {module1, module2, module3, module4});

            chainDelegate(context).Result.Status.Should().Be(ClusterResultStatus.UnexpectedException);

            calledModules.Should().Equal(module1, module2, module3, module4);
        }

        [Test]
        public void Should_build_a_delegate_which_checks_cancellation_token_before_each_module_invocation()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            tokenSource.Cancel();

            context.CancellationToken.Returns(token);

            var chainDelegate = RequestModuleChainBuilder.BuildChainDelegate(new[] {module1, module2, module3, module4});

            chainDelegate(context).Result.Status.Should().Be(ClusterResultStatus.Canceled);

            calledModules.Should().BeEmpty();
        }

        [Test]
        public void Should_build_a_chain_with_correct_modules_composition_and_disposition()
        {
            var configuration = Substitute.For<IClusterClientConfiguration>();

            configuration.Modules.Returns(
                new Dictionary<Type, RelatedModules>
                {
                    [typeof(LoggingModule)] = new RelatedModules
                    {
                        Before = {module1, module2}
                    }
                });

            configuration.Logging.Returns(new LoggingOptions());

            var storageProvider = Substitute.For<IReplicaStorageProvider>();

            var modules = RequestModuleChainBuilder.BuildChain(configuration, storageProvider);

            modules.Should().HaveCount(14);

            modules[0].Should().BeOfType<LeakPreventionModule>();
            modules[1].Should().BeOfType<GlobalErrorCatchingModule>();
            modules[2].Should().BeOfType<RequestTransformationModule>();
            modules[3].Should().BeOfType<AuxiliaryHeadersModule>();
            modules[4].Should().BeSameAs(module1);
            modules[5].Should().BeSameAs(module2);
            modules[6].Should().BeOfType<LoggingModule>();
            modules[7].Should().BeOfType<ResponseTransformationModule>();
            modules[8].Should().BeOfType<ErrorCatchingModule>();
            modules[9].Should().BeOfType<RequestValidationModule>();
            modules[10].Should().BeOfType<TimeoutValidationModule>();
            modules[11].Should().BeOfType<RequestRetryModule>();
            modules[12].Should().BeOfType<AbsoluteUrlSenderModule>();
            modules[13].Should().BeOfType<RequestExecutionModule>();
        }

        [Test]
        public void Should_build_a_chain_with_correct_modules_composition_and_disposition_when_optional_modules_are_enabled()
        {
            var configuration = Substitute.For<IClusterClientConfiguration>();

            configuration.Logging.Returns(new LoggingOptions());

            configuration.Modules.Returns(
                new Dictionary<Type, RelatedModules>
                {
                    [typeof(AbsoluteUrlSenderModule)] = new RelatedModules
                    {
                        Before =
                        {
                            new AdaptiveThrottlingModule(new AdaptiveThrottlingOptionsPerPriority("foo")),
                            new ReplicaBudgetingModule(new ReplicaBudgetingOptions("foo"))
                        }
                    }
                });

            var storageProvider = Substitute.For<IReplicaStorageProvider>();

            var modules = RequestModuleChainBuilder.BuildChain(configuration, storageProvider);

            modules.Should().HaveCount(14);

            modules[0].Should().BeOfType<LeakPreventionModule>();
            modules[1].Should().BeOfType<GlobalErrorCatchingModule>();
            modules[2].Should().BeOfType<RequestTransformationModule>();
            modules[3].Should().BeOfType<AuxiliaryHeadersModule>();
            modules[4].Should().BeOfType<LoggingModule>();
            modules[5].Should().BeOfType<ResponseTransformationModule>();
            modules[6].Should().BeOfType<ErrorCatchingModule>();
            modules[7].Should().BeOfType<RequestValidationModule>();
            modules[8].Should().BeOfType<TimeoutValidationModule>();
            modules[9].Should().BeOfType<RequestRetryModule>();
            modules[10].Should().BeOfType<AdaptiveThrottlingModule>();
            modules[11].Should().BeOfType<ReplicaBudgetingModule>();
            modules[12].Should().BeOfType<AbsoluteUrlSenderModule>();
            modules[13].Should().BeOfType<RequestExecutionModule>();
        }
        
        [Test]
        public void Should_add_optional_modules_recursively()
        {
            var configuration = Substitute.For<IClusterClientConfiguration>();
        
            configuration.Logging.Returns(new LoggingOptions());
        
            configuration.Modules.Returns(
            new Dictionary<Type, RelatedModules>
            {
                [typeof(GlobalErrorCatchingModule)] = new RelatedModules
                    {
                        Before =
                        {
                            new AdaptiveThrottlingModule(new AdaptiveThrottlingOptionsPerPriority("foo")),
                        }
                    },
                    [typeof(AdaptiveThrottlingModule)] = new RelatedModules
                {
                    After =
                    {
                        new ReplicaBudgetingModule(new ReplicaBudgetingOptions("foo"))
                    }
                }
            });
        
            var storageProvider = Substitute.For<IReplicaStorageProvider>();
        
            var modules = RequestModuleChainBuilder.BuildChain(configuration, storageProvider);
        
            modules.Should().HaveCount(14);
        
            modules[0].Should().BeOfType<LeakPreventionModule>();
            modules[1].Should().BeOfType<AdaptiveThrottlingModule>();
            modules[2].Should().BeOfType<ReplicaBudgetingModule>();
            modules[3].Should().BeOfType<GlobalErrorCatchingModule>();
            modules[4].Should().BeOfType<RequestTransformationModule>();
            modules[5].Should().BeOfType<AuxiliaryHeadersModule>();
            modules[6].Should().BeOfType<LoggingModule>();
            modules[7].Should().BeOfType<ResponseTransformationModule>();
            modules[8].Should().BeOfType<ErrorCatchingModule>();
            modules[9].Should().BeOfType<RequestValidationModule>();
            modules[10].Should().BeOfType<TimeoutValidationModule>();
            modules[11].Should().BeOfType<RequestRetryModule>();
            modules[12].Should().BeOfType<AbsoluteUrlSenderModule>();
            modules[13].Should().BeOfType<RequestExecutionModule>();
        }
        
        [Test]
        public void Should_add_optional_modules_for_some_module_once()
        {
            var configuration = Substitute.For<IClusterClientConfiguration>();
        
            configuration.Logging.Returns(new LoggingOptions());
        
            configuration.Modules.Returns(
            new Dictionary<Type, RelatedModules>
            {
                [typeof(GlobalErrorCatchingModule)] = new RelatedModules
                    {
                        Before =
                        {
                            new AdaptiveThrottlingModule(new AdaptiveThrottlingOptionsPerPriority("foo")),
                        }
                    },
                    [typeof(AdaptiveThrottlingModule)] = new RelatedModules
                    {
                        After =
                        {
                            new ReplicaBudgetingModule(new ReplicaBudgetingOptions("foo"))
                        }
                    },
                    [typeof(AuxiliaryHeadersModule)] = new RelatedModules
                    {
                        After =
                        {
                            new GlobalErrorCatchingModule()
                        }
                    }
            });
        
            var storageProvider = Substitute.For<IReplicaStorageProvider>();
        
            var modules = RequestModuleChainBuilder.BuildChain(configuration, storageProvider);
        
            modules.Should().HaveCount(15);
        
            modules[0].Should().BeOfType<LeakPreventionModule>();
            modules[1].Should().BeOfType<AdaptiveThrottlingModule>();
            modules[2].Should().BeOfType<ReplicaBudgetingModule>();
            modules[3].Should().BeOfType<GlobalErrorCatchingModule>();
            modules[4].Should().BeOfType<RequestTransformationModule>();
            modules[5].Should().BeOfType<AuxiliaryHeadersModule>();
            modules[6].Should().BeOfType<GlobalErrorCatchingModule>();
            modules[7].Should().BeOfType<LoggingModule>();
            modules[8].Should().BeOfType<ResponseTransformationModule>();
            modules[9].Should().BeOfType<ErrorCatchingModule>();
            modules[10].Should().BeOfType<RequestValidationModule>();
            modules[11].Should().BeOfType<TimeoutValidationModule>();
            modules[12].Should().BeOfType<RequestRetryModule>();
            modules[13].Should().BeOfType<AbsoluteUrlSenderModule>();
            modules[14].Should().BeOfType<RequestExecutionModule>();
        }

        [Test]
        public void Should_respect_removed_built_in_modules_when_building_a_chain()
        {
            var config = new ClusterClientConfiguration(new SilentLog());

            config.RemoveRequestModule(RequestModule.AbsoluteUrlSender);

            var chain = RequestModuleChainBuilder.BuildChain(config, Substitute.For<IReplicaStorageProvider>());

            chain.Should().NotContain(module => module is AbsoluteUrlSenderModule);
        }

        [Test]
        public void Should_respect_removed_custom_modules_when_building_a_chain()
        {
            var config = new ClusterClientConfiguration(new SilentLog());

            config.SetupThreadPoolLimitsTuning();

            config.RemoveRequestModule(RequestModule.ThreadPoolTuning);

            var chain = RequestModuleChainBuilder.BuildChain(config, Substitute.For<IReplicaStorageProvider>());

            chain.Should().NotContain(module => module is ThreadPoolTuningModule);
        }
    }
}