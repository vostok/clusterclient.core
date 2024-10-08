﻿using System;
using System.Text;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Misc;

namespace Vostok.Clusterclient.Core.Model
{
    /// <summary>
    /// <para>Represents an HTTP request (method, url, headers and body content).</para>
    /// <para>Every <see cref="Request"/> object is effectively immutable. Any modifications produce a new object.</para>
    /// <para>Look at <see cref="RequestUrlBuilder"/> to quickly build request urls with collection initializer syntax.</para>
    /// <para>Look at <see cref="RequestHeadersExtensions"/> to quickly set common headers.</para>
    /// <para>Look at <see cref="RequestContentExtensions"/> to quickly add content to requests.</para>
    /// <para>Look at <see cref="RequestQueryExtensions"/> to quickly add query parameters to requests.</para>
    /// </summary>
    [PublicAPI]
    public class Request
    {
        public Request(
            [NotNull] string method,
            [NotNull] Uri url,
            [CanBeNull] Content content = null,
            [CanBeNull] Headers headers = null)
            : this(method, url, null, null, null, content, headers)
        {
        }

        public Request(
            [NotNull] string method,
            [NotNull] Uri url,
            [CanBeNull] IStreamContent content,
            [CanBeNull] Headers headers = default)
            : this(method, url, null, content, null, null, headers)
        {
        }

        public Request(
            [NotNull] string method,
            [NotNull] Uri url,
            [CanBeNull] CompositeContent content,
            [CanBeNull] Headers headers = default)
            : this(method, url, content, null, null, null, headers)
        {
        }

        public Request(
            [NotNull] string method,
            [NotNull] Uri url,
            [CanBeNull] IContentProducer contentProducer,
            [CanBeNull] Headers headers = default)
            : this(method, url, null, null, contentProducer, null, headers)
        {
        }

        private Request(
            [NotNull] string method,
            [NotNull] Uri url,
            [CanBeNull] CompositeContent compositeContent,
            [CanBeNull] IStreamContent streamContent,
            [CanBeNull] IContentProducer contentProducer,
            [CanBeNull] Content content,
            [CanBeNull] Headers headers)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            Url = url ?? throw new ArgumentNullException(nameof(url));
            CompositeContent = compositeContent;
            StreamContent = streamContent;
            ContentProducer = contentProducer;
            Content = content;
            Headers = headers;
        }

        /// <summary>
        /// Returns request method (one of <see cref="RequestMethods"/>).
        /// </summary>
        [NotNull]
        public string Method { get; }

        /// <summary>
        /// Returns request url.
        /// </summary>
        [NotNull]
        public Uri Url { get; }

        /// <summary>
        /// Returns request content or <c>null</c> if there is none.
        /// </summary>
        [CanBeNull]
        public Content Content { get; }

        /// <summary>
        /// Returns request composite content or <c>null</c> if there is none.
        /// </summary>
        [CanBeNull]
        public CompositeContent CompositeContent { get; }

        /// <summary>
        /// Returns request stream content or <c>null</c> if there is none.
        /// </summary>
        [CanBeNull]
        public IStreamContent StreamContent { get; }

        [CanBeNull]
        public IContentProducer ContentProducer { get; }

        /// <summary>
        /// Returns request headers or <c>null</c> if there are none.
        /// </summary>
        [CanBeNull]
        public Headers Headers { get; }

        /// <summary>
        /// Returns <c>true</c> if current instance has a non-null <see cref="Content"/>, <see cref="CompositeContent"/> or <see cref="StreamContent"/>.
        /// </summary>
        public bool HasBody => Content != null || CompositeContent != null || StreamContent != null || ContentProducer != null;

        /// <summary>
        /// Produces a new <see cref="Request"/> instance with given url. Current instance is not modified.
        /// </summary>
        /// <returns>A new <see cref="Request"/> object with updated url.</returns>
        [Pure]
        [NotNull]
        public Request WithUrl([NotNull] string url)
        {
            return WithUrl(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Produces a new <see cref="Request"/> instance with given url. Current instance is not modified.
        /// </summary>
        /// <returns>A new <see cref="Request"/> object with updated url.</returns>
        [Pure]
        [NotNull]
        public Request WithUrl([NotNull] Uri url)
        {
            return new Request(Method, url, CompositeContent, StreamContent, ContentProducer, Content, Headers);
        }

        /// <summary>
        /// <para>Produces a new <see cref="Request"/> instance with given body content. Current instance is not modified.</para>
        /// <para>If current instance contains a non-null <see cref="StreamContent"/>, <see cref="CompositeContent"/> or <see cref="IContentProducer"/> property, it will be discarded in new instance.</para>
        /// </summary>
        /// <returns>A new <see cref="Request"/> object with updated content.</returns>
        [Pure]
        [NotNull]
        public Request WithContent([NotNull] Content content)
        {
            return new Request(Method, Url, content, (Headers ?? Headers.Empty).Set(HeaderNames.ContentLength, content.Length));
        }

        /// <summary>
        /// <para>Produces a new <see cref="Request"/> instance with given body stream content. Current instance is not modified.</para>
        /// <para>If current instance contains a non-null <see cref="Content"/>, <see cref="CompositeContent"/> or <see cref="IContentProducer"/> property, it will be discarded in new instance.</para>
        /// </summary>
        /// <returns>A new <see cref="Request"/> object with updated content.</returns>
        [Pure]
        [NotNull]
        public Request WithContent([NotNull] IStreamContent content)
        {
            return new Request(Method, Url, content, content.Length.HasValue ? (Headers ?? Headers.Empty).Set(HeaderNames.ContentLength, content.Length.Value) : Headers);
        }

        /// <summary>
        /// <para>Produces a new <see cref="Request"/> instance with body filled with given <see cref="IContentProducer"/> instance. Current instance is not modified.</para>
        /// <para>If current instance contains a non-null <see cref="Content"/>, <see cref="CompositeContent"/> or <see cref="StreamContent"/> property, it will be discarded in new instance.</para>
        /// </summary>
        /// <returns>A new <see cref="Request"/> object with updated content.</returns>
        [Pure]
        [NotNull]
        public Request WithContent([NotNull] IContentProducer content)
        {
            return new Request(Method, Url, content, content.Length.HasValue ? (Headers ?? Headers.Empty).Set(HeaderNames.ContentLength, content.Length.Value) : Headers);
        }

        /// <summary>
        /// <para>Produces a new <see cref="Request"/> instance with given body content. Current instance is not modified.</para>
        /// <para>If current instance contains a non-null <see cref="StreamContent"/> or <see cref="Content"/> property, it will be discarded in new instance.</para>
        /// </summary>
        /// <returns>A new <see cref="Request"/> object with updated content.</returns>
        [Pure]
        [NotNull]
        public Request WithContent([NotNull] CompositeContent content)
        {
            return new Request(Method, Url, content, (Headers ?? Headers.Empty).Set(HeaderNames.ContentLength, content.Length));
        }

        /// <summary>
        /// <para>Produces a new <see cref="Request"/> instance where the header with given name will have given value.</para>
        /// <para>See <see cref="Headers"/> class documentation for details.</para>
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value. ToString() is used to obtain string value.</param>
        /// <returns>A new <see cref="Request"/> object with updated headers.</returns>
        [Pure]
        [NotNull]
        public Request WithHeader<T>([NotNull] string name, [NotNull] T value)
        {
            return WithHeader(name, value.ToString());
        }

        /// <summary>
        /// <para>Produces a new <see cref="Request"/> instance where the header with given name will have given value.</para>
        /// <para>See <see cref="Headers"/> class documentation for details.</para>
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns>A new <see cref="Request"/> object with updated headers.</returns>
        [Pure]
        [NotNull]
        public Request WithHeader([NotNull] string name, [NotNull] string value)
        {
            return new Request(Method, Url, CompositeContent, StreamContent, ContentProducer, Content, (Headers ?? Headers.Empty).Set(name, value));
        }

        /// <summary>
        /// <para>Produces a new <see cref="Request"/> instance with headers substituted by given collection.</para>
        /// <para>See <see cref="Headers"/> class documentation for details.</para>
        /// </summary>
        /// <returns>A new <see cref="Request"/> object with given headers.</returns>
        [Pure]
        [NotNull]
        public Request WithHeaders([NotNull] Headers headers)
        {
            return new Request(Method, Url, CompositeContent, StreamContent, ContentProducer, Content, headers);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString(false, false);
        }

        /// <param name="includeQuery">Append query string to result</param>
        /// <param name="includeHeaders">Append all headers to result</param>
        /// <returns>String representation of <see cref="Request"/> instance.</returns>
        [PublicAPI]
        public string ToString(bool includeQuery, bool includeHeaders)
        {
            var querySettings = includeQuery ? RequestParametersLoggingSettings.DefaultEnabled : RequestParametersLoggingSettings.DefaultDisabled;
            var headersSettings = includeHeaders ? RequestParametersLoggingSettings.DefaultEnabled : RequestParametersLoggingSettings.DefaultDisabled;
            return ToString(querySettings, headersSettings, singleLineManner: false);
        }

        /// <inheritdoc cref="ToString(bool,bool)"/>
        [PublicAPI]
        public string ToString([NotNull] RequestParametersLoggingSettings includeQuery, [NotNull] RequestParametersLoggingSettings includeHeaders)
        {
            return ToString(includeQuery, includeHeaders, singleLineManner: false);
        }

        internal string ToString([NotNull] RequestParametersLoggingSettings includeQuery, [NotNull] RequestParametersLoggingSettings includeHeaders, bool singleLineManner)
        {
            if (includeQuery == null)
                throw new ArgumentNullException(nameof(includeQuery));
            if (includeHeaders == null)
                throw new ArgumentNullException(nameof(includeHeaders));

            var builder = new StringBuilder();

            builder.Append(Method);
            builder.Append(" ");

            if (includeQuery.Enabled)
            {
                if (includeQuery.IsEnabledForAllKeys())
                {
                    builder.Append(Url);
                }
                else
                {
                    var requestUrlParser = new RequestUrlParser(Url.ToString());

                    builder.Append(requestUrlParser.Path);

                    LoggingUtils.AppendQueryString(builder, includeQuery, requestUrlParser);
                }
            }
            else
            {
                RequestUrlParsingHelpers.TryParseUrlPath(Url.ToString(), out var path, out _);
                builder.Append(path);
            }

            if (includeHeaders.Enabled && Headers is {Count: > 0})
            {
                LoggingUtils.AppendHeaders(builder, Headers, includeHeaders, singleLineManner, appendTitle: true);
            }

            return builder.ToString();
        }

        #region Factory methods

        /// <summary>
        /// Creates a new request with <c>GET</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Get([NotNull] Uri url)
        {
            return new Request(RequestMethods.Get, url);
        }

        /// <summary>
        /// Creates a new request with <c>GET</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Get([NotNull] string url)
        {
            return Get(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Creates a new request with <c>POST</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Post([NotNull] Uri url)
        {
            return new Request(RequestMethods.Post, url);
        }

        /// <summary>
        /// Creates a new request with <c>POST</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Post([NotNull] string url)
        {
            return Post(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Creates a new request with <c>PUT</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Put([NotNull] Uri url)
        {
            return new Request(RequestMethods.Put, url);
        }

        /// <summary>
        /// Creates a new request with <c>PUT</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Put([NotNull] string url)
        {
            return Put(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Creates a new request with <c>HEAD</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Head([NotNull] Uri url)
        {
            return new Request(RequestMethods.Head, url);
        }

        /// <summary>
        /// Creates a new request with <c>HEAD</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Head([NotNull] string url)
        {
            return Head(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Creates a new request with <c>PATCH</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Patch([NotNull] Uri url)
        {
            return new Request(RequestMethods.Patch, url);
        }

        /// <summary>
        /// Creates a new request with <c>PATCH</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Patch([NotNull] string url)
        {
            return Patch(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Creates a new request with <c>DELETE</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Delete([NotNull] Uri url)
        {
            return new Request(RequestMethods.Delete, url);
        }

        /// <summary>
        /// Creates a new request with <c>DELETE</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Delete([NotNull] string url)
        {
            return Delete(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Creates a new request with <c>OPTIONS</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Options([NotNull] Uri url)
        {
            return new Request(RequestMethods.Options, url);
        }

        /// <summary>
        /// Creates a new request with <c>OPTIONS</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Options([NotNull] string url)
        {
            return Options(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Creates a new request with <c>TRACE</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Trace([NotNull] Uri url)
        {
            return new Request(RequestMethods.Trace, url);
        }

        /// <summary>
        /// Creates a new request with <c>TRACE</c> method and given <paramref name="url"/>.
        /// </summary>
        [NotNull]
        public static Request Trace([NotNull] string url)
        {
            return Trace(new Uri(url, UriKind.RelativeOrAbsolute));
        }

        #endregion
    }
}