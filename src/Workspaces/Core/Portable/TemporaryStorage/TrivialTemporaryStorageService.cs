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

internal sealed class TrivialTemporaryStorageService : ITemporaryStorageServiceInternal
{
    public static readonly TrivialTemporaryStorageService Instance = new();

    private TrivialTemporaryStorageService()
    {
    }

    public ITemporaryStorageStreamHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var newStream = new MemoryStream();
        stream.CopyTo(newStream);
        return new StreamStorage(newStream);
    }

    public ITemporaryStorageTextHandle WriteToTemporaryStorage(SourceText text, CancellationToken cancellationToken)
    {
        return new TextStorage(text);
    }

    public async Task<ITemporaryStorageTextHandle> WriteToTemporaryStorageAsync(SourceText text, CancellationToken cancellationToken)
    {
        return new TextStorage(text);
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
