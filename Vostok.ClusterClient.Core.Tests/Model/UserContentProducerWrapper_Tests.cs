using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Tests.Helpers;

namespace Vostok.Clusterclient.Core.Tests.Model
{
    [TestFixture]
    internal class UserContentProducerWrapper_Tests
    {
        [Test]
        public void WasUsed_property_should_always_initially_return_false([Values] bool contentReusable)
        {
            var contentProducer = Substitute.For<IContentProducer>();
            var content = new UserContentProducerWrapper(contentProducer);

            contentProducer.IsReusable.Returns(contentReusable);

            content.WasUsed.Should().BeFalse();
        }

        [Test]
        public void WasUsed_property_should_return_true_after_first_content_producing_when_underlying_content_not_reusable()
        {
            var contentProducer = Substitute.For<IContentProducer>();
            var content = new UserContentProducerWrapper(contentProducer);

            content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();
            content.WasUsed.Should().BeTrue();
        }

        [Test]
        public void WasUsed_property_should_return_false_after_content_producing_when_underlying_content_reusable()
        {
            var contentProducer = Substitute.For<IContentProducer>();
            contentProducer.IsReusable.Returns(true);
            var content = new UserContentProducerWrapper(contentProducer);

            content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();
            content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();
            content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();

            content.WasUsed.Should().BeFalse();
        }

        [TestCase(null)]
        [TestCase(1234)]
        public void Length_should_return_underlying_content_length(long value)
        {
            var contentProducer = Substitute.For<IContentProducer>();
            contentProducer.Length.Returns(value);
            var content = new UserContentProducerWrapper(contentProducer);

            content.Length.Should().Be(value);
        }

        [Test]
        public void IsReusable_should_return_underlying_is_reusable([Values] bool value)
        {
            var contentProducer = Substitute.For<IContentProducer>();
            contentProducer.IsReusable.Returns(value);
            var content = new UserContentProducerWrapper(contentProducer);

            contentProducer.IsReusable.Returns(value);

            content.IsReusable.Should().Be(value);
        }

        [Test]
        public void IsReusable_should_be_cached_when_initially_true()
        {
            var contentProducer = Substitute.For<IContentProducer>();
            contentProducer.IsReusable.Returns(true);
            var content = new UserContentProducerWrapper(contentProducer);

            content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();

            contentProducer.IsReusable.Returns(false);

            new Action(
                    () =>
                    {
                        content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();
                        content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();
                        content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();
                    })
                .Should()
                .NotThrow();
        }

        [Test]
        public void IsReusable_should_be_cached_when_initially_false()
        {
            var contentProducer = Substitute.For<IContentProducer>();
            contentProducer.IsReusable.Returns(false);
            var content = new UserContentProducerWrapper(contentProducer);

            content.ProduceAsync(Stream.Null, CancellationToken.None).Wait();

            contentProducer.IsReusable.Returns(true);

            new Action(() => content.ProduceAsync(Stream.Null, CancellationToken.None).Wait()).Should().ThrowExactly<ContentAlreadyUsedException>().Which.ShouldBePrinted();
        }

        [Test]
        public void ProduceAsync_should_call_underlying_content_method()
        {
            var contentProducer = Substitute.For<IContentProducer>();
            var content = new UserContentProducerWrapper(contentProducer);
            var stream = Substitute.For<Stream>();

            content.ProduceAsync(stream, CancellationToken.None).Wait();

            contentProducer.Received(1).ProduceAsync(Arg.Is<Stream>(x => ReferenceEquals(x, stream)), Arg.Any<CancellationToken>());
        }

        [Test]
        public void ProduceAsync_should_throw_after_first_call_when_underlying_content_not_reusable()
        {
            var contentProducer = Substitute.For<IContentProducer>();
            var content = new UserContentProducerWrapper(contentProducer);

            Action action = () => content.ProduceAsync(Stream.Null, CancellationToken.None);

            action();

            action.Should().ThrowExactly<ContentAlreadyUsedException>().Which.ShouldBePrinted();
        }
    }
}