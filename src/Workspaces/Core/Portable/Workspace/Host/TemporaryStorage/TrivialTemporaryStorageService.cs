// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [ExportWorkspaceService(typeof(ITemporaryStorageService)), Shared]
    internal sealed class TrivialTemporaryStorageService : ITemporaryStorageService
    {
        [ImportingConstructor]
        public TrivialTemporaryStorageService()
        {
        }

        public ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken = default)
        {
            return new StreamStorage();
        }

        public ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken = default)
        {
            return new TextStorage();
        }

        private sealed class StreamStorage : ITemporaryStreamStorage
        {
            private MemoryStream _stream;

            public void Dispose()
            {
                _stream?.Dispose();
                _stream = null;
            }

            public Stream ReadStream(CancellationToken cancellationToken = default)
            {
                if (_stream == null)
                {
                    throw new InvalidOperationException();
                }

                _stream.Position = 0;
                return _stream;
            }

            public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default)
            {
                if (_stream == null)
                {
                    throw new InvalidOperationException();
                }

                _stream.Position = 0;
                return Task.FromResult((Stream)_stream);
            }

            public void WriteStream(Stream stream, CancellationToken cancellationToken = default)
            {
                var newStream = new MemoryStream();
                stream.CopyTo(newStream);
                _stream = newStream;
            }

            public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default)
            {
                var newStream = new MemoryStream();
                await stream.CopyToAsync(newStream).ConfigureAwait(false);
                _stream = newStream;
            }
        }

        private sealed class TextStorage : ITemporaryTextStorage
        {
            private SourceText _sourceText;

            public void Dispose()
            {
                _sourceText = null;
            }

            public SourceText ReadText(CancellationToken cancellationToken = default)
            {
                return _sourceText;
            }

            public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ReadText(cancellationToken));
            }

            public void WriteText(SourceText text, CancellationToken cancellationToken = default)
            {
                // This is a trivial implementation, indeed. Note, however, that we retain a strong
                // reference to the source text, which defeats the intent of RecoverableTextAndVersion, but
                // is appropriate for this trivial implementation.
                _sourceText = text;
            }

            public Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default)
            {
                WriteText(text, cancellationToken);
                return Task.CompletedTask;
            }
        }
    }
}
