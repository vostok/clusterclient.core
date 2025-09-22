using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Misc;

namespace Vostok.Clusterclient.Core.Model
{
    /// <summary>
    /// Represents an HTTP response from server.
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    [PublicAPI]
    public class Response : IDisposable
    {
        private readonly Content content;
        private readonly Headers headers;
        private readonly Stream stream;
        private readonly Headers trailingHeaders;
        private readonly Func<Headers> getTrailingHeaders;

        public Response(
            ResponseCode code,
            [CanBeNull] Content content = null,
            [CanBeNull] Headers headers = null,
            [CanBeNull] Stream stream = null,
            [CanBeNull] Headers trailingHeaders = null,
            [CanBeNull] Func<Headers> getTrailingHeaders = null)
        {
            if (content != null && stream != null)
                throw new ArgumentException("A response can't have both buffered content and a body stream.");
            if (trailingHeaders != null && getTrailingHeaders != null)
                throw new ArgumentException("A response can't have both read trailers and trailers callback");
            
            Code = code;
            this.content = content;
            this.headers = headers;
            this.stream = stream;
            this.trailingHeaders = trailingHeaders;
            this.getTrailingHeaders = getTrailingHeaders;
        }
        
        // BACKWARDS COMPATIBILITY OVERLOAD
        public Response(
            ResponseCode code,
            [CanBeNull] Content content,
            [CanBeNull] Headers headers,
            [CanBeNull] Stream stream) : this(code, content, headers, stream, null, null)
        {
        }

        /// <summary>
        /// Returns response code.
        /// </summary>
        public ResponseCode Code { get; }

        /// <summary>
        /// Returns response body content or an empty content if there is none.
        /// </summary>
        [NotNull]
        public Content Content => content ?? Content.Empty;

        /// <summary>
        /// Returns response headers or empty headers if there are none.
        /// </summary>
        [NotNull]
        public Headers Headers => headers ?? Headers.Empty;

        /// <summary>
        /// Returns response stream or an empty stream if there is none.
        /// </summary>
        [NotNull]
        public Stream Stream => stream ?? Stream.Null;

        /// <summary>
        /// Returns value of trailers field if present, then tries to invoke trailers callback with no result caching.
        /// If none of the two is present, returns empty headers
        /// </summary>
        [NotNull]
        public Headers TrailingHeaders => trailingHeaders ?? getTrailingHeaders?.Invoke() ?? Headers.Empty;

        /// <summary>
        /// Returns true if this response has buffered content, or false otherwise.
        /// </summary>
        public bool HasContent => content != null && content.Length > 0;

        /// <summary>
        /// Returns true if this response has any headers, or false otherwise.
        /// </summary>
        public bool HasHeaders => headers != null;

        /// <summary>
        /// Returns true if this response has response body stream, or false otherwise.
        /// </summary>
        public bool HasStream => stream != null;

        /// <summary>
        /// Returns <c>true</c> if <see cref="Code"/> belongs to 2xx range (see <see cref="ResponseCodeExtensions.IsSuccessful"/>), or false otherwise.
        /// </summary>
        public bool IsSuccessful => Code.IsSuccessful();

        /// <summary>
        /// Dispose underlying response stream.
        /// </summary>
        public void Dispose()
        {
            stream?.Dispose();
        }

        /// <summary>
        /// <para>Produces a new <see cref="Response"/> instance where the header with given name will have given value.</para>
        /// <para>See <see cref="Headers"/> class documentation for details.</para>
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns>A new <see cref="Response"/> object with updated headers.</returns>
        [Pure]
        [NotNull]
        public Response WithHeader([NotNull] string name, [NotNull] string value)
        {
            return new Response(Code, content, Headers.Set(name, value), stream, trailingHeaders, getTrailingHeaders);
        }

        /// <summary>
        /// <para>Produces a new <see cref="Response"/> instance where the header with given name will have given value.</para>
        /// <para>See <see cref="Headers"/> class documentation for details.</para>
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns>A new <see cref="Response"/> object with updated headers.</returns>
        [Pure]
        [NotNull]
        public Response WithHeader<T>([NotNull] string name, [NotNull] T value)
        {
            return WithHeader(name, value.ToString());
        }

        /// <summary>
        /// <para>Produces a new <see cref="Response"/> instance where the header with given name will not exist.</para>
        /// <para>See <see cref="Headers"/> class documentation for details.</para>
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <returns>A new <see cref="Response"/> object with updated headers.</returns>
        [Pure]
        [NotNull]
        public Response RemoveHeader([NotNull] string name)
        {
            var newHeaders = Headers.Remove(name);

            if (ReferenceEquals(newHeaders, Headers))
                return this;

            return new Response(Code, content, newHeaders, stream, trailingHeaders, getTrailingHeaders);
        }

        /// <summary>
        /// Produces a new <see cref="Response"/> instance with given body content. Current instance is not modified.
        /// </summary>
        /// <returns>A new <see cref="Response"/> object with updated content.</returns>
        [Pure]
        [NotNull]
        public Response WithContent([NotNull] Content newContent)
        {
            return new Response(Code, newContent, headers, stream, trailingHeaders, getTrailingHeaders);
        }

        /// <summary>
        /// Produces a new <see cref="Response"/> instance with given body stream. Current instance is not modified.
        /// </summary>
        /// <returns>A new <see cref="Response"/> object with updated stream.</returns>
        [Pure]
        [NotNull]
        public Response WithStream([NotNull] Stream newStream)
        {
            return new Response(Code, content, headers, newStream, trailingHeaders, getTrailingHeaders);
        }
        
        
        /// <summary>
        /// Produces a new <see cref="Response"/> instance with given trailers. Current instance is not modified.
        /// </summary>
        /// <returns>A new <see cref="Response"/> object with updated trailers.</returns>
        [Pure]
        [NotNull]
        public Response WithTrailingHeaders([NotNull] Headers newTrailers)
        {
            return new Response(Code, content, headers, stream, newTrailers, getTrailingHeaders);
        }

        /// <summary>
        /// Produces a new <see cref="Response"/> instance with given trailers callback. Current instance is not modified.
        /// </summary>
        /// <param name="newGetTrailers">New callback to be used</param>
        /// <returns>A new <see cref="Response"/> object with updated trailers callback.</returns>
        [Pure]
        [NotNull]
        public Response WithTrailingHeadersCallback([NotNull] Func<Headers> newGetTrailers)
        {
            return new Response(Code, content, headers, stream, trailingHeaders, newGetTrailers);
        }

        /// <summary>
        /// Throws a <see cref="ClusterClientException"/> if <see cref="Code"/> doesn't belong to 2xx range (see <see cref="ResponseCodeExtensions.IsSuccessful"/>)
        /// </summary>
        public Response EnsureSuccessStatusCode()
        {
            if (!IsSuccessful)
                throw new ClusterClientException($"Response status code '{Code}' indicates unsuccessful outcome.");

            return this;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString(false);
        }

        /// <param name="includeHeaders">Append all headers to result</param>
        /// <returns>String representation of current <see cref="Response"/> instance.</returns>
        [PublicAPI]
        public string ToString(bool includeHeaders)
        {
            var headersSettings = includeHeaders ? RequestParametersLoggingSettings.DefaultEnabled : RequestParametersLoggingSettings.DefaultDisabled;
            return ToString(headersSettings, singleLineManner: false);
        }

        /// <inheritdoc cref="ToString(bool)"/>
        [PublicAPI]
        public string ToString([NotNull] RequestParametersLoggingSettings includeHeaders)
        {
            return ToString(includeHeaders, singleLineManner: false);
        }

        internal string ToString([NotNull] RequestParametersLoggingSettings includeHeaders, bool singleLineManner)
        {
            if (includeHeaders == null)
                throw new ArgumentNullException(nameof(includeHeaders));

            var builder = new StringBuilder();

            builder.Append((int)Code);
            builder.Append(" ");
            builder.Append(Code);

            if (includeHeaders.Enabled && Headers.Count > 0)
            {
                LoggingUtils.AppendHeaders(builder, Headers, includeHeaders, singleLineManner, appendTitle: true);
            }

            return builder.ToString();
        }
    }
}