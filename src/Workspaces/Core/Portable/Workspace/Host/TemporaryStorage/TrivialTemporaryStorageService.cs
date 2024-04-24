// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal sealed partial class TrivialTemporaryStorageService : ITemporaryStorageServiceInternal
{
    public static readonly TrivialTemporaryStorageService Instance = new();

    private TrivialTemporaryStorageService()
    {
    }

    public ITemporaryStorageTextHandle WriteToTemporaryStorage(SourceText text, CancellationToken cancellationToken)
    {
        var storage = new TextStorage();
        storage.WriteText(text);
        var identifier = new TemporaryStorageTextIdentifier(
            Guid.NewGuid().ToString("N"), Offset: 0, Size: text.Length, text.ChecksumAlgorithm, text.Encoding, text.GetContentHash());
        var handle = new TrivialStorageTextHandle(identifier, storage);
        return handle;
    }

    public Task<ITemporaryStorageTextHandle> WriteToTemporaryStorageAsync(SourceText text, CancellationToken cancellationToken)
        => Task.FromResult(WriteToTemporaryStorage(text, cancellationToken));

    public ITemporaryStorageStreamHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var storage = new StreamStorage();
        storage.WriteStream(stream);
        var identifier = new TemporaryStorageIdentifier(Guid.NewGuid().ToString("N"), Offset: 0, Size: stream.Length);
        var handle = new TrivialStorageStreamHandle(identifier, storage);
        return handle;
    }

    private sealed class StreamStorage
    {
        private MemoryStream? _stream;

        public Stream ReadStream()
        {
            var stream = _stream ?? throw new InvalidOperationException();

            // Return a read-only view of the underlying buffer to prevent users from overwriting or directly
            // disposing the backing storage.
            return new MemoryStream(stream.GetBuffer(), 0, (int)stream.Length, writable: false);
        }

        public void WriteStream(Stream stream)
        {
            var newStream = new MemoryStream();
            stream.CopyTo(newStream);
            var existingValue = Interlocked.CompareExchange(ref _stream, newStream, null);
            if (existingValue is not null)
            {
                throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
            }
        }
    }

    private sealed class TextStorage
    {
        private SourceText? _sourceText;

        public SourceText ReadText()
            => _sourceText ?? throw new InvalidOperationException();

        public Task<SourceText> ReadTextAsync()
            => Task.FromResult(ReadText());

        public void WriteText(SourceText text)
        {
            // This is a trivial implementation, indeed. Note, however, that we retain a strong
            // reference to the source text, which defeats the intent of RecoverableTextAndVersion, but
            // is appropriate for this trivial implementation.
            var existingValue = Interlocked.CompareExchange(ref _sourceText, text, null);
            if (existingValue is not null)
                throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
        }
    }
}
