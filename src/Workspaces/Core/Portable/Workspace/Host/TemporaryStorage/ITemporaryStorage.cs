// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// TemporaryStorage can be used to read and write text to a temporary storage location.
    /// </summary>
    public interface ITemporaryTextStorage : IDisposable
    {
        SourceText ReadText(CancellationToken cancellationToken = default(CancellationToken));
        Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default(CancellationToken));
        void WriteText(SourceText text, CancellationToken cancellationToken = default(CancellationToken));
        Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface ITemporaryStreamStorage : IDisposable
    {
        Stream ReadStream(CancellationToken cancellationToken = default(CancellationToken));
        Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken));
        void WriteStream(Stream stream, CancellationToken cancellationToken = default(CancellationToken));
        Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken));
    }
}
