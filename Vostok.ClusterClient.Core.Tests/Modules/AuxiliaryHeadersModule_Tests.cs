﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;

namespace Vostok.Clusterclient.Core.Tests.Modules
{
    [TestFixture]
    internal class AuxiliaryHeadersModule_Tests
    {
        private IRequestContext context;
        private AuxiliaryHeadersModule module;

        private string priorityHeader;
        private string identityHeader;

        [SetUp]
        public void TestSetup()
        {
            context = new RequestContext(
                Request.Get("foo/bar"),
                RequestParameters.Empty,
                budget: default,
                log: default,
                clusterProvider: default,
                asyncClusterProvider: default,
                replicaOrdering: default,
                transport: default,
                maximumReplicasToUse: default,
                connectionAttempts: default,
                clientApplicationName: Guid.NewGuid().ToString());

            module = new AuxiliaryHeadersModule(
                priorityHeader = Guid.NewGuid().ToString(),
                identityHeader = Guid.NewGuid().ToString());
        }

        [TestCase(RequestPriority.Critical)]
        [TestCase(RequestPriority.Ordinary)]
        [TestCase(RequestPriority.Sheddable)]
        public void Should_set_request_priority_to_headers(RequestPriority priority)
        {
            context.Parameters = context.Parameters.WithPriority(priority);

            Execute();

            context.Request.Headers?[priorityHeader].Should().Be(priority.ToString());
        }

        [Test]
        public void Should_set_client_application_name_to_headers()
        {
            Execute();

            context.Request.Headers?[identityHeader].Should().Be(context.ClientApplicationName);
        }

        [Test]
        public void Should_not_override_per_request_client_application_name()
        {
            var clientName = Guid.NewGuid().ToString();

            context.Request = context.Request.WithHeader(identityHeader, clientName);
            
            Execute();

            context.Request.Headers?[identityHeader].Should().Be(clientName);
        }

        private void Execute()
        {
            var taskSource = new TaskCompletionSource<ClusterResult>();

            var task = taskSource.Task;

            module.ExecuteAsync(context, _ => task).Should().BeSameAs(task);
        }
    }
}