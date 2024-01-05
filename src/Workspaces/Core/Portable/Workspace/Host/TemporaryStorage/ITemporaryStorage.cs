// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    [Obsolete("Roslyn no longer exports a mechanism to store arbitrary data in-memory.")]
    public interface ITemporaryTextStorage : IDisposable
    {
        SourceText ReadText(CancellationToken cancellationToken = default);
        Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default);
        void WriteText(SourceText text, CancellationToken cancellationToken = default);
        Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default);
    }

    [Obsolete("Roslyn no longer exports a mechanism to store arbitrary data in-memory.")]
    public interface ITemporaryStreamStorage : IDisposable
    {
        Stream ReadStream(CancellationToken cancellationToken = default);
        Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default);
        void WriteStream(Stream stream, CancellationToken cancellationToken = default);
        Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// TemporaryStorage can be used to read and write text to a temporary storage location.
    /// </summary>
    internal interface ITemporaryTextStorageInternal : IDisposable
    {
        SourceText ReadText(CancellationToken cancellationToken = default);
        Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default);
        void WriteText(SourceText text, CancellationToken cancellationToken = default);
        Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default);
    }

    internal interface ITemporaryStreamStorageInternal : IDisposable
    {
        Stream ReadStream(CancellationToken cancellationToken = default);
        Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default);
        void WriteStream(Stream stream, CancellationToken cancellationToken = default);
        Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
