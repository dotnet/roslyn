// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal sealed class TrivialTemporaryStorageService : ITemporaryStorageServiceInternal
{
    public static readonly TrivialTemporaryStorageService Instance = new();

    private TrivialTemporaryStorageService()
    {
    }

    public ITemporaryStreamStorageInternal CreateTemporaryStreamStorage()
        => new StreamStorage();

    public ITemporaryTextStorageInternal CreateTemporaryTextStorage()
        => new TextStorage();

    private sealed class StreamStorage : ITemporaryStreamStorageInternal
    {
        private MemoryStream? _stream;

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }

        public Stream ReadStream(CancellationToken cancellationToken)
        {
            var stream = _stream ?? throw new InvalidOperationException();

            // Return a read-only view of the underlying buffer to prevent users from overwriting or directly
            // disposing the backing storage.
            return new MemoryStream(stream.GetBuffer(), 0, (int)stream.Length, writable: false);
        }

        public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ReadStream(cancellationToken));
        }

        public void WriteStream(Stream stream, CancellationToken cancellationToken)
        {
            var newStream = new MemoryStream();
            stream.CopyTo(newStream);
            var existingValue = Interlocked.CompareExchange(ref _stream, newStream, null);
            if (existingValue is not null)
            {
                throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
            }
        }

        public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            var newStream = new MemoryStream();
#if NETCOREAPP
            await stream.CopyToAsync(newStream, cancellationToken).ConfigureAwait(false);
# else
            await stream.CopyToAsync(newStream).ConfigureAwait(false);
#endif
            var existingValue = Interlocked.CompareExchange(ref _stream, newStream, null);
            if (existingValue is not null)
            {
                throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
            }
        }
    }

    private sealed class TextStorage : ITemporaryTextStorageInternal
    {
        private SourceText? _sourceText;

        public void Dispose()
            => _sourceText = null;

        public SourceText ReadText(CancellationToken cancellationToken)
            => _sourceText ?? throw new InvalidOperationException();

        public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken)
            => Task.FromResult(ReadText(cancellationToken));

        public void WriteText(SourceText text, CancellationToken cancellationToken)
        {
            // This is a trivial implementation, indeed. Note, however, that we retain a strong
            // reference to the source text, which defeats the intent of RecoverableTextAndVersion, but
            // is appropriate for this trivial implementation.
            var existingValue = Interlocked.CompareExchange(ref _sourceText, text, null);
            if (existingValue is not null)
            {
                throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
            }
        }

        public Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default)
        {
            WriteText(text, cancellationToken);
            return Task.CompletedTask;
        }
    }
}
