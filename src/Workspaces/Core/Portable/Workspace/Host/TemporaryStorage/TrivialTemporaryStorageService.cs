// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class TrivialTemporaryStorageService : ITemporaryStorageService
    {
        public static readonly TrivialTemporaryStorageService Instance = new TrivialTemporaryStorageService();

        private TrivialTemporaryStorageService()
        {
        }

        public ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken = default)
            => new StreamStorage();

        public ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken = default)
            => new TextStorage();

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
#if NETCOREAPP
                await stream.CopyToAsync(newStream, cancellationToken).ConfigureAwait(false);
# else
                await stream.CopyToAsync(newStream).ConfigureAwait(false);
#endif
                _stream = newStream;
            }
        }

        private sealed class TextStorage : ITemporaryTextStorage
        {
            private SourceText _sourceText;

            public void Dispose()
                => _sourceText = null;

            public SourceText ReadText(CancellationToken cancellationToken = default)
                => _sourceText;

            public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(ReadText(cancellationToken));

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
