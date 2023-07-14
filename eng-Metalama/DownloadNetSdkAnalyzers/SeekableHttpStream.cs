using System.Net;
using System.Net.Http.Headers;

namespace DownloadNetSdkAnalyzers;

// Mostly copied from https://codereview.stackexchange.com/q/230621/2041.

public class SeekableHttpStream : Stream
{
    private long _position;
    private long _underlyingStreamOffset;
    private Stream? _underlyingStream;
    private bool _forceRequest;

    internal SeekableHttpStream(
        HttpClient client,
        HttpResponseMessage response,
        HttpRequestMessage request)
    {
        Client = client;
        Response = response;
        Request = request;

        var headers = response.Headers;
        var acceptRanges = headers?.AcceptRanges;
        if (acceptRanges == null || !acceptRanges.Contains("bytes"))
        {
            throw new ArgumentException("server does not support HTTP range requests", nameof(request));
        }
        var contentHeaders = response.Content.Headers;
        if (contentHeaders.ContentLength != null)
        {
            Length = contentHeaders.ContentLength.Value;
        }
        else if (contentHeaders.ContentRange != null)
        {
            if (contentHeaders.ContentRange.Length == null)
            {
                throw new ArgumentException("missing Content-Range length", nameof(request));
            }

            Length = contentHeaders.ContentRange.Length.Value;
        }
        else
        {
            throw new ArgumentException("failed to determine stream length", nameof(request));
        }
    }

    public HttpClient Client { get; }

    public HttpResponseMessage Response { get; }

    public HttpRequestMessage Request { get; }

    public override bool CanRead => _position < Length;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        EnsureStreamOpen().GetAwaiter().GetResult();
        int read = _underlyingStream!.Read(buffer, offset, count);
        _position += read;
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        EnsureStreamOpen().GetAwaiter().GetResult();
        int read = _underlyingStream!.Read(buffer);
        _position += read;
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await EnsureStreamOpen(cancellationToken).ConfigureAwait(false);
        int read = await _underlyingStream!.ReadAsync(buffer, offset, count, cancellationToken)
            .ConfigureAwait(false);
        _position += read;
        return read;
    }

    public override int ReadByte()
    {
        EnsureStreamOpen().GetAwaiter().GetResult();

        var value = _underlyingStream!.ReadByte();
        ++_position;

        return value;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await EnsureStreamOpen(cancellationToken).ConfigureAwait(false);

        int read = await _underlyingStream!.ReadAsync(buffer, cancellationToken)
             .ConfigureAwait(false);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return SeekAsync(offset, origin).GetAwaiter().GetResult();
    }

    private Task EnsureStreamOpen(CancellationToken cancellationToken = default)
    {
        if (_underlyingStream == null)
        {
            _forceRequest = true;
            return SeekAsync(0, SeekOrigin.Current, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public async Task<long> SeekAsync(long offset, SeekOrigin origin, CancellationToken cancellationToken = default)
    {
        const long SeekThreshold = 1024 * 1024;

        long newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        if (newPosition < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if (newPosition > Length)
        {
            throw new NotSupportedException("seeking beyond the length of the stream is not supported");
        }

        long delta = newPosition - _position;
        if (_underlyingStream == null)
        {
            if (_forceRequest)
            {
                await OpenUnderlyingStream(newPosition, cancellationToken)
                    .ConfigureAwait(false);
            }
            _position = newPosition;
        }
        else if (_underlyingStream.CanSeek && newPosition >= _underlyingStreamOffset && newPosition <= _underlyingStreamOffset + _underlyingStream.Length)
        {
            _underlyingStream.Position = newPosition - _underlyingStreamOffset;
            _position = newPosition;
        }
        else if (delta < 0 || delta > SeekThreshold)
        {
            await OpenUnderlyingStream(newPosition, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (delta > 0)
        {
            var buffer = new byte[delta];
            await ReadExactlyAsync(buffer, cancellationToken);
        }

        return _position;
    }

    private async Task<HttpRequestMessage> CopyHttpRequest()
    {
        var clone = new HttpRequestMessage(Request.Method, Request.RequestUri);

        if (Request.Content != null)
        {
            var bytes = await Request.Content.ReadAsByteArrayAsync()
                .ConfigureAwait(false);
            clone.Content = new ByteArrayContent(bytes);

            if (Request.Content.Headers != null)
                foreach (var h in Request.Content.Headers)
                    clone.Content.Headers.Add(h.Key, h.Value);
        }

        clone.Version = Request.Version;

#pragma warning disable CS0618 // Type or member is obsolete
        foreach (var prop in Request.Properties)
        {
            clone.Properties.Add(prop);
        }
#pragma warning restore CS0618

        foreach (var header in Request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private async Task OpenUnderlyingStream(long position, CancellationToken cancellationToken = default)
    {
        const long UseBufferedStreamThreshold = 4 * 1024 * 1024;

        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        using var newRequest = await CopyHttpRequest()
            .ConfigureAwait(false);
        if (position > 0)
        {
            var responseHeaders = Response.Headers;
            var contentHeaders = Response.Content.Headers;

            if (responseHeaders.ETag != null)
            {
                newRequest.Headers.IfRange = new RangeConditionHeaderValue(responseHeaders.ETag);
            }
            else if (contentHeaders.LastModified != null)
            {
                newRequest.Headers.IfRange = new RangeConditionHeaderValue(contentHeaders.LastModified.Value);
            }

            newRequest.Headers.Range = new RangeHeaderValue(position, null);
        }

        long remainingLength = Length - position;

        var response = await Client.SendAsync(
            newRequest,
            remainingLength <= UseBufferedStreamThreshold ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        ).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            response.EnsureSuccessStatusCode();
        }
        else if (position > 0 && response.StatusCode != HttpStatusCode.PartialContent)
        {
            response.Dispose();
            throw new InvalidOperationException("range request not supported or content has changed since last request");
        }
        else
        {
            try
            {
                var stream = await response.Content.ReadAsStreamAsync()
                    .ConfigureAwait(false);
                if (_underlyingStream != null)
                {
                    await _underlyingStream.DisposeAsync()
                        .ConfigureAwait(false);
                }

                _underlyingStream = stream;
                _underlyingStreamOffset = position;
                _forceRequest = false;
                _position = position;
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        _underlyingStream?.Dispose();
        Response.Dispose();
        Request.Dispose();
        base.Dispose(disposing);
    }

    #region Unsupported write methods

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotSupportedException();
    }

    public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();

    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public override void WriteByte(byte value)
    {
        throw new NotSupportedException();
    }

    public override int WriteTimeout
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    #endregion Unsupported write methods
}

public static class HttpClientExtensions
{
    public static async Task<SeekableHttpStream> GetSeekableStreamAsync(this HttpClient client, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        return await SendSeekableStreamAsync(client, request)
            .ConfigureAwait(false);
    }

    public static async Task<SeekableHttpStream> GetSeekableStreamAsync(this HttpClient client, Uri requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        return await SendSeekableStreamAsync(client, request)
            .ConfigureAwait(false);
    }

    public static async Task<SeekableHttpStream> SendSeekableStreamAsync(this HttpClient client, HttpRequestMessage request)
    {
        HttpMethod method = request.Method;
        try
        {
            request.Method = HttpMethod.Head;
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            try
            {
                return new SeekableHttpStream(client, response, request);
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
        finally
        {
            request.Method = method;
        }
    }
}
