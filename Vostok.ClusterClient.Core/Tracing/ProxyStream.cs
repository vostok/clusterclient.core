#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Threading;
using Vostok.Tracing.Abstractions;

namespace Vostok.Clusterclient.Core.Tracing;

internal sealed class ProxyStream : Stream
{
    private readonly Stream stream;
    private readonly Activity activity;
    private Activity additionalActivity;
    private long? read;
    private readonly AtomicBoolean disposed = false;

    public ProxyStream(Stream stream, Activity activity)
    {
        this.stream = stream;
        this.activity = activity;
    }

    public void AddAdditionalActivity(Activity value) =>
        additionalActivity = value;

    public override int Read(byte[] buffer, int offset, int count)
    {
        var result = stream.Read(buffer, offset, count);
        AddRead(result);
        return result;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var result = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        AddRead(result);
        return result;
    }

#if NETCOREAPP
    public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        var result = await stream.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
        AddRead(result);
        return result;
    }
#endif

    public override int ReadByte()
    {
        var result = stream.ReadByte();
        if (result != -1)
            AddRead(1);
        return result;
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        var result = stream.EndRead(asyncResult);
        AddRead(result);
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        DisposeBuilder();

        if (disposing)
            stream.Dispose();
    }

    public override void Close()
    {
        DisposeBuilder();

        stream.Close();
    }

    private void DisposeBuilder()
    {
        if (!disposed.TrySetTrue())
            return;

        if (read.HasValue)
        {
            activity.SetTag(WellKnownAnnotations.Http.Response.Size, read.Value);
            additionalActivity?.SetTag(WellKnownAnnotations.Http.Response.Size, read.Value);
        }

        activity.Dispose();
        additionalActivity?.Dispose();
    }

    private void AddRead(long value) =>
        read = (read ?? 0) + value;

    #region Delegating members

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => stream.CanWrite;

    public override bool CanTimeout => stream.CanTimeout;

    public override long Length => stream.Length;

    public override long Position
    {
        get => stream.Position;
        set => stream.Position = value;
    }

    public override int ReadTimeout
    {
        get => stream.ReadTimeout;
        set => stream.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => stream.WriteTimeout;
        set => stream.WriteTimeout = value;
    }

    public override void Flush()
        => stream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => stream.FlushAsync(cancellationToken);

    public override long Seek(long offset, SeekOrigin origin)
        => stream.Seek(offset, origin);

    public override void SetLength(long value)
        => stream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
        => stream.Write(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => stream.WriteAsync(buffer, offset, count, cancellationToken);

#if NETCOREAPP
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        => stream.WriteAsync(source, cancellationToken);
#endif

    public override void WriteByte(byte value)
        => stream.WriteByte(value);

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        => stream.BeginRead(buffer, offset, count, callback, state);

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        => stream.BeginWrite(buffer, offset, count, callback, state);

    public override void EndWrite(IAsyncResult asyncResult)
        => stream.EndWrite(asyncResult);

    public override object InitializeLifetimeService()
        => stream.InitializeLifetimeService();

    public override bool Equals(object obj)
        => stream.Equals(obj);

    public override int GetHashCode()
        => stream.GetHashCode();

    public override string ToString()
        => stream.ToString();

    #endregion
}
#endif