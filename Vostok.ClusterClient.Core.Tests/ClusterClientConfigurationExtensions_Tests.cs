﻿using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.ClusterClient.Abstractions;
using Vostok.ClusterClient.Abstractions.Criteria;
using Vostok.ClusterClient.Abstractions.Modules;
using Vostok.ClusterClient.Abstractions.Transforms;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Core.Tests
{
    [TestFixture]
    internal class ClusterClientConfigurationExtensions_Tests
    {
        private ClusterClientConfiguration configuration;

        [SetUp]
        public void TestSetup()
        {
            configuration = new ClusterClientConfiguration(new SilentLog());
        }

        [Test]
        public void SetupResponseCriteria_should_build_correct_criteria_list()
        {
            var criterion1 = Substitute.For<IResponseCriterion>();
            var criterion2 = Substitute.For<IResponseCriterion>();

            configuration.SetupResponseCriteria(criterion1, criterion2);

            configuration.ResponseCriteria.Should().Equal(criterion1, criterion2);
        }

        [Test]
        public void AddRequestMoudle_should_not_fail_if_modules_list_is_null()
        {
            configuration.Modules = null;

            configuration.AddRequestModule(Substitute.For<IRequestModule>());

            configuration.Modules.Should().HaveCount(1);
        }

        [Test]
        public void AddRequestTransform_should_not_fail_if_transforms_list_is_null()
        {
            configuration.RequestTransforms = null;

            configuration.AddRequestTransform(Substitute.For<IRequestTransform>());

            configuration.RequestTransforms.Should().HaveCount(1);
        }

        [Test]
        public void AddResponseTransform_should_not_fail_if_transforms_list_is_null()
        {
            configuration.ResponseTransforms = null;

            configuration.AddResponseTransform(Substitute.For<IResponseTransform>());

            configuration.ResponseTransforms.Should().HaveCount(1);
        }
    }
}
