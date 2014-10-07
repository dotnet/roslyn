// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A factory for creating <see cref="SourceText"/> instances.
    /// </summary>
    public interface ITextFactoryService : IWorkspaceService
    {
        /// <summary>
        /// Creates <see cref="SourceText"/> from a stream.
        /// </summary>
        /// <param name="stream">The stream to read the text from. Must be readable and seekable. The text is read from the start of the stream.</param>
        /// <param name="defaultEncoding">
        /// Specifies an encoding to be used if the actual encoding can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
        /// If not specified auto-detect heristics are used to determine the encoding. If these heristics fail the decoding is assumed to be the system encoding.
        /// Note that if the stream starts with Byte Order Mark the value of <paramref name="defaultEncoding"/> is ignored.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="InvalidDataException">
        /// The stream content can't be decoded using the specified <paramref name="defaultEncoding"/>, or
        /// <paramref name="defaultEncoding"/> is null and the stream appears to be a binary file.
        /// </exception>
        /// <exception cref="IOException">An IO error occured while reading from the stream.</exception>
        SourceText CreateText(Stream stream, Encoding defaultEncoding, CancellationToken cancellationToken = default(CancellationToken));
    }
}