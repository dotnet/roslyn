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
        private readonly string _content;
        private readonly Encoding? _encoding;
        private readonly SourceHashAlgorithm _checksumAlgorithm;

        public TemporaryStorageIdentifier Identifier { get; }

        public TextStorage(SourceText sourceText)
        {
            // Rather than holding onto the SourceText, just hold onto the underlying content;
            // this better matches the real implementation where creating temporary storage for text does
            // not root the underlying text objects.
            _content = sourceText.ToString();
            _encoding = sourceText.Encoding;
            _checksumAlgorithm = sourceText.ChecksumAlgorithm;
            Identifier = new TemporaryStorageIdentifier(Guid.NewGuid().ToString(), 0, _content.Length);
        }

        public SourceText ReadFromTemporaryStorage(CancellationToken cancellationToken)
        {
            return SourceText.From(_content, _encoding, _checksumAlgorithm);
        }

        public async Task<SourceText> ReadFromTemporaryStorageAsync(CancellationToken cancellationToken)
        {
            return SourceText.From(_content, _encoding, _checksumAlgorithm);
        }
    }
}
