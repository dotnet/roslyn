// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed class TrivialTemporaryStorageService : ITemporaryStorageServiceInternal
{
    /// <summary>
    /// Threshold at which we sweep the dictionaries for dead weak references on each write.
    /// </summary>
    private const int CleanupThreshold = 64;

    private readonly ConcurrentDictionary<string, WeakReference<ITemporaryStorageStreamHandle>> _streamHandles = new();
    private readonly ConcurrentDictionary<string, WeakReference<ITemporaryStorageTextHandle>> _textHandles = new();

    public ITemporaryStorageStreamHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var newStream = new MemoryStream();
        stream.CopyTo(newStream);
        var handle = new StreamStorage(newStream);
        Contract.ThrowIfNull(handle.Identifier.Name);
        _streamHandles.TryAdd(handle.Identifier.Name, new WeakReference<ITemporaryStorageStreamHandle>(handle));
        CleanupIfNeeded(_streamHandles);
        return handle;
    }

    public ITemporaryStorageTextHandle WriteToTemporaryStorage(SourceText text, CancellationToken cancellationToken)
    {
        var handle = new TextStorage(text);
        Contract.ThrowIfNull(handle.Identifier.Name);
        _textHandles.TryAdd(handle.Identifier.Name, new WeakReference<ITemporaryStorageTextHandle>(handle));
        CleanupIfNeeded(_textHandles);
        return handle;
    }

    public async Task<ITemporaryStorageTextHandle> WriteToTemporaryStorageAsync(SourceText text, CancellationToken cancellationToken)
    {
        return WriteToTemporaryStorage(text, cancellationToken);
    }

    public ITemporaryStorageStreamHandle GetStreamHandle(TemporaryStorageIdentifier storageIdentifier)
    {
        Contract.ThrowIfNull(storageIdentifier.Name);
        if (_streamHandles.TryGetValue(storageIdentifier.Name, out var weakRef) && weakRef.TryGetTarget(out var handle))
            return handle;

        throw new InvalidOperationException($"No stream handle found for storage identifier '{storageIdentifier.Name}'.");
    }

    public ITemporaryStorageTextHandle GetTextHandle(
        TemporaryStorageIdentifier storageIdentifier,
        SourceHashAlgorithm checksumAlgorithm,
        Encoding? encoding,
        ImmutableArray<byte> contentHash)
    {
        Contract.ThrowIfNull(storageIdentifier.Name);
        if (_textHandles.TryGetValue(storageIdentifier.Name, out var weakRef) && weakRef.TryGetTarget(out var handle))
            return handle;

        throw new InvalidOperationException($"No text handle found for storage identifier '{storageIdentifier.Name}'.");
    }

    private static void CleanupIfNeeded<T>(ConcurrentDictionary<string, WeakReference<T>> dictionary) where T : class
    {
        if (dictionary.Count <= CleanupThreshold)
            return;

        foreach (var pair in dictionary)
        {
            if (!pair.Value.TryGetTarget(out _))
                dictionary.TryRemove(pair.Key, out _);
        }
    }

    private sealed class StreamStorage : ITemporaryStorageStreamHandle
    {
        private readonly MemoryStream _stream;

        public TemporaryStorageIdentifier Identifier { get; }

        public StreamStorage(MemoryStream stream)
        {
            _stream = stream;
            Identifier = new TemporaryStorageIdentifier(Guid.NewGuid().ToString(), 0, _stream.Length);
        }

        public Stream ReadFromTemporaryStorage()
        {
            // Return a read-only view of the underlying buffer to prevent users from overwriting or directly
            // disposing the backing storage.
            return new MemoryStream(_stream.GetBuffer(), 0, (int)_stream.Length, writable: false);
        }
    }

    private sealed class TextStorage : ITemporaryStorageTextHandle
    {
        private readonly SourceText _sourceText;

        public TemporaryStorageIdentifier Identifier { get; }

        public TextStorage(SourceText sourceText)
        {
            _sourceText = sourceText;
            Identifier = new TemporaryStorageIdentifier(Guid.NewGuid().ToString(), 0, _sourceText.Length);
        }

        public SourceText ReadFromTemporaryStorage(CancellationToken cancellationToken)
        {
            return _sourceText;
        }

        public async Task<SourceText> ReadFromTemporaryStorageAsync(CancellationToken cancellationToken)
        {
            return _sourceText;
        }
    }
}
