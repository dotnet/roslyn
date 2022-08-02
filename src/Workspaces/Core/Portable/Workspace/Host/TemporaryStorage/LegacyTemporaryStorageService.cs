﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Legacy implementation of obsolete public API <see cref="ITemporaryStorageService"/>.
/// </summary>
[Obsolete]
[ExportWorkspaceService(typeof(ITemporaryStorageService)), Shared]
internal sealed class LegacyTemporaryStorageService : ITemporaryStorageService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LegacyTemporaryStorageService()
    {
    }

    public ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken = default)
        => new StreamStorage();

    public ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken = default)
        => new TextStorage();

    private sealed class StreamStorage : ITemporaryStreamStorage
    {
        private MemoryStream? _stream;

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }

        public Stream ReadStream(CancellationToken cancellationToken = default)
        {
            var stream = _stream ?? throw new InvalidOperationException();

            // Return a read-only view of the underlying buffer to prevent users from overwriting or directly
            // disposing the backing storage.
            return new MemoryStream(stream.GetBuffer(), 0, (int)stream.Length, writable: false);
        }

        public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReadStream(cancellationToken));
        }

        public void WriteStream(Stream stream, CancellationToken cancellationToken = default)
        {
            var newStream = new MemoryStream();
            stream.CopyTo(newStream);
            var existingValue = Interlocked.CompareExchange(ref _stream, newStream, null);
            if (existingValue is not null)
            {
                throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
            }
        }

        public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default)
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

    private sealed class TextStorage : ITemporaryTextStorage
    {
        private SourceText? _sourceText;

        public void Dispose()
            => _sourceText = null;

        public SourceText ReadText(CancellationToken cancellationToken = default)
            => _sourceText ?? throw new InvalidOperationException();

        public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ReadText(cancellationToken));

        public void WriteText(SourceText text, CancellationToken cancellationToken = default)
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
