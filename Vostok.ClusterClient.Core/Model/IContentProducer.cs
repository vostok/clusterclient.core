using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Model
{
    /// <summary>
    /// Represents a request body source which will be used in <see cref="Request"/>.
    /// </summary>
    [PublicAPI]
    public interface IContentProducer
    {
        /// <summary>
        /// <para>Indicates if content can be produced multiple times.</para>
        /// <para>When set to false:</para>
        /// <list type="bullet">
        /// <item><description><see cref="ProduceAsync"/> method will be called exactly once.</description></item>
        /// <item><description>failed requests will never be retried or forked.</description></item>
        /// </list> 
        /// <para>User should take note that this property should:</para>
        /// <list type="bullet">
        /// <item><description>only be set to true when implementation allows multiple calls to <see cref="ProduceAsync"/></description></item>
        /// <item><description>be consistent and not flip between true and false values during producing</description></item>
        /// </list> 
        /// </summary>
        bool IsReusable { get; }

        /// <summary>
        /// A request body length. If value is provided, it will be used for <see cref="HeaderNames.ContentLength"/> header.
        /// </summary>
        long? Length { get; }

        /// <summary>
        /// Writes content to request stream.
        /// </summary>
        /// <param name="requestStream">A stream used for writing request body content. Note that request stream allows only writing and will throw <see cref="NotSupportedException"/> in any other case.</param>
        /// <param name="cancellationToken">A cancellation token used for request content producing.</param>
        /// <returns></returns>
        Task ProduceAsync(Stream requestStream, CancellationToken cancellationToken);
    }
}