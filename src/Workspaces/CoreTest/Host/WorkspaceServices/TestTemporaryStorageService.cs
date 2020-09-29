// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.Persistence
{
    [ExportWorkspaceService(typeof(ITemporaryStorageService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal sealed class TestTemporaryStorageService : ITemporaryStorageService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestTemporaryStorageService()
        {
        }

        public ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken = default)
            => new StreamStorage();

        public ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken = default)
            => new TextStorage();

        internal class StreamStorage : ITemporaryStreamStorage
        {
            private MemoryStream _stream;

            public static int s_DisposalCount = 0;

            public void Dispose()
            {
                _stream?.Dispose();
                _stream = null;
                s_DisposalCount++;
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

        internal class TextStorage : ITemporaryTextStorage
        {
            private string _text;
            private Encoding _encoding;

            public static int s_DisposalCount = 0;

            public void Dispose()
            {
                _text = null;
                _encoding = null;
                s_DisposalCount++;
            }

            public SourceText ReadText(CancellationToken cancellationToken = default)
                => SourceText.From(_text, _encoding);

            public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(ReadText(cancellationToken));

            public void WriteText(SourceText text, CancellationToken cancellationToken = default)
            {
                _text = text.ToString();
                _encoding = text.Encoding;
            }

            public Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default)
            {
                WriteText(text, cancellationToken);
                return Task.CompletedTask;
            }
        }
    }
}
