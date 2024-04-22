// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed class TrivialTemporaryStorageService : ITemporaryStorageServiceInternal
{
    public static readonly TrivialTemporaryStorageService Instance = new();

    private static readonly ConditionalWeakTable<TemporaryStorageIdentifier, StreamStorage> s_streamStorage = new();

    private TrivialTemporaryStorageService()
    {
    }

    public ITemporaryTextStorageInternal CreateTemporaryTextStorage()
        => new TextStorage();

    public TemporaryStorageHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var storage = new StreamStorage();
        storage.WriteStream(stream);
        var identifier = new TemporaryStorageIdentifier(Guid.NewGuid().ToString("N"), 0, 0);
        var handle = new TemporaryStorageHandle(memoryMappedFile: null, identifier);
        s_streamStorage.Add(identifier, storage);
        return handle;
    }

    public Stream ReadFromTemporaryStorageService(TemporaryStorageIdentifier storageIdentifier, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(
            s_streamStorage.TryGetValue(storageIdentifier, out var streamStorage),
            "StorageIdentifier was not created by this storage service!");

        return streamStorage.ReadStream();
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
                throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
        }

        public Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default)
        {
            WriteText(text, cancellationToken);
            return Task.CompletedTask;
        }
    }
}
