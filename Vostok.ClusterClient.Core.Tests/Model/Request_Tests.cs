﻿using System;
using System.IO;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;

// ReSharper disable PossibleNullReferenceException

namespace Vostok.Clusterclient.Core.Tests.Model
{
    [TestFixture]
    internal class Request_Tests
    {
        private Request request;

        [SetUp]
        public void TestSetup()
        {
            request = new Request(RequestMethods.Post, new Uri("http://foo/bar?a=b"), Content.Empty, Headers.Empty);
        }

        [Test]
        public void Ctor_should_throw_when_method_is_null()
        {
            Action action = () => new Request(null, new Uri("http://foo/bar"));

            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Ctor_should_throw_when_url_is_null()
        {
            Action action = () => new Request(RequestMethods.Get, null);

            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void WithUrl_method_should_return_a_request_with_given_url()
        {
            var newUrl = new Uri("http://vostok.tools");

            var requestAfter = request.WithUrl(newUrl);

            requestAfter.Should().NotBeSameAs(request);
            requestAfter.Url.Should().BeSameAs(newUrl);
        }

        [Test]
        public void WithUrl_method_should_preserve_method_content_and_headers()
        {
            var newUrl = new Uri("http://vostok.tools");

            var requestAfter = request.WithUrl(newUrl);

            requestAfter.Method.Should().BeSameAs(request.Method);
            requestAfter.Content.Should().BeSameAs(request.Content);
            requestAfter.Headers.Should().BeSameAs(request.Headers);
        }

        [Test]
        public void WithUrl_method_should_preserve_stream_content()
        {
            request = request.WithContent(Stream.Null);

            var newUrl = new Uri("http://vostok.tools");

            var requestAfter = request.WithUrl(newUrl);

            requestAfter.StreamContent.Should().BeSameAs(request.StreamContent);
        }

        [Test]
        public void WithHeader_method_should_return_a_request_with_given_header()
        {
            var requestAfter = request.WithHeader("name", "value");

            requestAfter.Should().NotBeSameAs(request);
            requestAfter.Headers["name"].Should().Be("value");
        }

        [Test]
        public void WithHeader_method_should_preserve_method_url_and_content()
        {
            var requestAfter = request.WithHeader("name", "value");

            requestAfter.Method.Should().BeSameAs(request.Method);
            requestAfter.Url.Should().BeSameAs(request.Url);
            requestAfter.Content.Should().BeSameAs(request.Content);
        }

        [Test]
        public void WithHeader_method_should_preserve_stream_content()
        {
            request = request.WithContent(Stream.Null);

            var requestAfter = request.WithHeader("name", "value");

            requestAfter.StreamContent.Should().BeSameAs(request.StreamContent);
        }

        [Test]
        public void WithHeader_method_should_not_modify_original_request_headers()
        {
            request = request.WithHeader("a", "b").WithHeader("c", "d");

            request.WithHeader("e", "f");

            request.Headers.Names.Should().Equal("a", "c");
        }

        [Test]
        public void WithHeader_method_should_not_fail_if_request_did_not_have_any_headers()
        {
            request = new Request(request.Method, request.Url, request.Content);

            var requestAfter = request.WithHeader("name", "value");

            requestAfter.Headers["name"].Should().Be("value");
        }

        [Test]
        public void WithHeaders_method_should_fully_update_request_headers()
        {
            var newHeaders = Headers.Empty.Set("a", "b");

            var requestAfter = request.WithHeaders(newHeaders);

            requestAfter.Headers.Should().BeSameAs(newHeaders);
        }

        [Test]
        public void WithHeaders_method_should_not_modify_original_request_headers()
        {
            var headersBefore = request.Headers;

            var newHeaders = Headers.Empty.Set("a", "b");

            request.WithHeaders(newHeaders);

            request.Headers.Should().BeSameAs(headersBefore);
        }

        [Test]
        public void WithHeaders_method_should_preserve_method_url_and_content()
        {
            var newHeaders = Headers.Empty.Set("a", "b");

            var requestAfter = request.WithHeaders(newHeaders);

            requestAfter.Method.Should().BeSameAs(request.Method);
            requestAfter.Url.Should().BeSameAs(request.Url);
            requestAfter.Content.Should().BeSameAs(request.Content);
        }

        [Test]
        public void WithHeaders_method_should_preserve_stream_content()
        {
            request = request.WithContent(Stream.Null);

            var newHeaders = Headers.Empty.Set("a", "b");

            var requestAfter = request.WithHeaders(newHeaders);

            requestAfter.StreamContent.Should().BeSameAs(request.StreamContent);
        }

        [Test]
        public void WithContent_method_for_buffer_should_return_a_request_with_provided_content()
        {
            var content = new Content(new byte[16]);

            var requestAfter = request.WithContent(content);

            requestAfter.Should().NotBeSameAs(request);
            requestAfter.Content.Should().BeSameAs(content);
        }

        [Test]
        public void WithContent_method_for_buffer_should_preserve_method_and_url()
        {
            var requestAfter = request.WithContent(new Content(new byte[16]));

            requestAfter.Method.Should().BeSameAs(request.Method);
            requestAfter.Url.Should().BeSameAs(request.Url);
        }

        [Test]
        public void WithContent_method_for_buffer_should_not_touch_original_request_content()
        {
            var contentBefore = request.Content;

            request.WithContent(new Content(new byte[16]));

            var contentAfter = request.Content;

            contentAfter.Should().BeSameAs(contentBefore);
        }

        [Test]
        public void WithContent_method_for_buffer_should_set_content_length_header()
        {
            request.WithContent(new Content(new byte[16])).Headers.ContentLength.Should().Be("16");
        }

        [Test]
        public void WithContent_method_for_buffer_should_set_content_length_header_even_if_original_request_had_no_headers()
        {
            request = new Request(request.Method, request.Url, request.Content);

            request.WithContent(new Content(new byte[16])).Headers.ContentLength.Should().Be("16");
        }

        [Test]
        public void WithContent_method_for_buffer_should_preserve_original_request_headers()
        {
            request = request.WithHeader("k1", "v1").WithHeader("k2", "v2");

            request.WithContent(new Content(new byte[16])).Headers.Should().HaveCount(3);
        }

        [Test]
        public void WithContent_method_for_buffer_should_discard_any_previous_stream_content()
        {
            request = request.WithContent(Substitute.For<IStreamContent>());

            request = request.WithContent(new Content(new byte[16]));

            request.StreamContent.Should().BeNull();
        }

        [Test]
        public void WithContent_method_for_stream_should_return_a_request_with_provided_content()
        {
            var content = new StreamContent(Stream.Null, 123);

            var requestAfter = request.WithContent(content);

            requestAfter.Should().NotBeSameAs(request);
            requestAfter.StreamContent.Should().BeSameAs(content);
        }

        [Test]
        public void WithContent_method_for_stream_should_preserve_method_and_url()
        {
            var requestAfter = request.WithContent(new StreamContent(Stream.Null, 123));

            requestAfter.Method.Should().BeSameAs(request.Method);
            requestAfter.Url.Should().BeSameAs(request.Url);
        }

        [Test]
        public void WithContent_method_for_stream_should_not_touch_original_request_content()
        {
            var contentBefore = request.Content;

            request.WithContent(new StreamContent(Stream.Null, 123));

            var contentAfter = request.Content;

            contentAfter.Should().BeSameAs(contentBefore);
        }

        [Test]
        public void WithContent_method_for_stream_should_set_content_length_header_if_length_is_specified()
        {
            request.WithContent(new StreamContent(Stream.Null, 123)).Headers.ContentLength.Should().Be("123");
        }

        [Test]
        public void WithContent_method_for_stream_should_not_set_content_length_header_if_length_is_not_specified()
        {
            request.WithContent(new StreamContent(Stream.Null)).Headers.ContentLength.Should().BeNull();
        }

        [Test]
        public void WithContent_method_for_stream_should_set_content_length_header_even_if_original_request_had_no_headers()
        {
            request = new Request(request.Method, request.Url, request.Content);

            request.WithContent(new StreamContent(Stream.Null, 123)).Headers.ContentLength.Should().Be("123");
        }

        [Test]
        public void WithContent_method_for_stream_should_preserve_original_request_headers()
        {
            request = request.WithHeader("k1", "v1").WithHeader("k2", "v2");

            request.WithContent(new StreamContent(Stream.Null, 123)).Headers.Should().HaveCount(3);
        }

        [Test]
        public void WithContent_method_for_stream_should_discard_any_previous_buffer_content()
        {
            request = request.WithContent(new Content(new byte[16]));

            request = request.WithContent(new StreamContent(Stream.Null, 123));

            request.Content.Should().BeNull();
        }

        [Test]
        public void HasBody_property_should_return_false_when_request_does_not_contain_body_buffer_or_stream()
        {
            request = Request.Get("foo/bar");

            request.HasBody.Should().BeFalse();
        }

        [Test]
        public void HasBody_property_should_return_true_when_request_contains_body_buffer()
        {
            request = request.WithContent(new Content(new byte[16]));

            request.HasBody.Should().BeTrue();
        }

        [Test]
        public void HasBody_property_should_return_true_when_request_contains_body_stream()
        {
            request = request.WithContent(Substitute.For<IStreamContent>());

            request.HasBody.Should().BeTrue();
        }

        [Test]
        public void ToString_should_return_correct_value_when_printing_both_query_and_headers()
        {
            request = request.WithHeader("name", "value");

            request.ToString(true, true).Should().Be("POST http://foo/bar?a=b" + Environment.NewLine + "name: value");
        }

        [Test]
        public void ToString_should_return_correct_value_when_printing_headers_but_omitting_query()
        {
            request = request.WithHeader("name", "value");

            request.ToString(false, true).Should().Be("POST http://foo/bar" + Environment.NewLine + "name: value");
        }

        [Test]
        public void ToString_should_return_correct_value_when_printing_query_but_omitting_headers()
        {
            request = request.WithHeader("name", "value");

            request.ToString(true, false).Should().Be("POST http://foo/bar?a=b");
        }

        [Test]
        public void ToString_should_return_correct_value_when_omitting_both_query_and_headers()
        {
            request = request.WithHeader("name", "value");

            request.ToString(false, false).Should().Be("POST http://foo/bar");
        }

        [Test]
        public void ToString_should_omit_query_and_headers_by_default()
        {
            request = request.WithHeader("name", "value");

            request.ToString().Should().Be("POST http://foo/bar");
        }

        [Test]
        public void ToString_should_tolerate_empty_headers()
        {
            request.ToString(true, true).Should().Be("POST http://foo/bar?a=b");
        }

        [Test]
        public void ToString_should_tolerate_null_headers()
        {
            request = new Request(request.Method, request.Url, request.Content);

            request.ToString(true, true).Should().Be("POST http://foo/bar?a=b");
        }

        [TestCase(RequestMethods.Get, nameof(RequestMethods.Get))]
        [TestCase(RequestMethods.Head, nameof(RequestMethods.Head))]
        [TestCase(RequestMethods.Post, nameof(RequestMethods.Post))]
        [TestCase(RequestMethods.Put, nameof(RequestMethods.Put))]
        [TestCase(RequestMethods.Patch, nameof(RequestMethods.Patch))]
        [TestCase(RequestMethods.Delete, nameof(RequestMethods.Delete))]
        [TestCase(RequestMethods.Options, nameof(RequestMethods.Options))]
        [TestCase(RequestMethods.Trace, nameof(RequestMethods.Trace))]
        public void Factory_method_should_work_correctly_for_uri_argument_and_given_method(string methodValue, string factoryMethodName)
        {
            var factoryMethod = typeof(Request).GetMethod(factoryMethodName, new[] {typeof(Uri)});

            factoryMethod.Should().NotBeNull();

            var producedRequest = factoryMethod.Invoke(null, new object[] {request.Url}).Should().BeOfType<Request>().Which;

            producedRequest.Method.Should().Be(methodValue);
            producedRequest.Url.Should().BeSameAs(request.Url);
            producedRequest.Headers.Should().BeNull();
            producedRequest.Content.Should().BeNull();
        }

        [TestCase(RequestMethods.Get, nameof(RequestMethods.Get))]
        [TestCase(RequestMethods.Head, nameof(RequestMethods.Head))]
        [TestCase(RequestMethods.Post, nameof(RequestMethods.Post))]
        [TestCase(RequestMethods.Put, nameof(RequestMethods.Put))]
        [TestCase(RequestMethods.Patch, nameof(RequestMethods.Patch))]
        [TestCase(RequestMethods.Delete, nameof(RequestMethods.Delete))]
        [TestCase(RequestMethods.Options, nameof(RequestMethods.Options))]
        [TestCase(RequestMethods.Trace, nameof(RequestMethods.Trace))]
        public void Factory_method_should_work_correctly_for_string_argument_and_given_method(string methodValue, string factoryMethodName)
        {
            var factoryMethod = typeof(Request).GetMethod(factoryMethodName, new[] {typeof(string)});

            factoryMethod.Should().NotBeNull();

            var producedRequest = factoryMethod.Invoke(null, new object[] {request.Url.OriginalString}).Should().BeOfType<Request>().Which;

            producedRequest.Method.Should().Be(methodValue);
            producedRequest.Url.OriginalString.Should().BeSameAs(request.Url.OriginalString);
            producedRequest.Headers.Should().BeNull();
            producedRequest.Content.Should().BeNull();
        }

        [TestCase(nameof(RequestMethods.Get))]
        [TestCase(nameof(RequestMethods.Head))]
        [TestCase(nameof(RequestMethods.Post))]
        [TestCase(nameof(RequestMethods.Put))]
        [TestCase(nameof(RequestMethods.Patch))]
        [TestCase(nameof(RequestMethods.Delete))]
        [TestCase(nameof(RequestMethods.Options))]
        [TestCase(nameof(RequestMethods.Trace))]
        public void Factory_method_should_work_correctly_for_string_argument_with_both_absolute_and_relative_urls(string factoryMethodName)
        {
            var factoryMethod = typeof(Request).GetMethod(factoryMethodName, new[] {typeof(string)});

            factoryMethod.Should().NotBeNull();

            var absoluteRequest = factoryMethod.Invoke(null, new object[] {"http://foo/bar"}).Should().BeOfType<Request>().Which;
            var relativeRequest = factoryMethod.Invoke(null, new object[] {"foo/bar"}).Should().BeOfType<Request>().Which;

            absoluteRequest.Url.IsAbsoluteUri.Should().BeTrue();
            relativeRequest.Url.IsAbsoluteUri.Should().BeFalse();
        }
    }
}