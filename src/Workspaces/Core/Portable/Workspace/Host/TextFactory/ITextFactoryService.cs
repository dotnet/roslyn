// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A factory for creating <see cref="SourceText"/> instances.
    /// </summary>
    internal interface ITextFactoryService : IWorkspaceService
    {
        /// <summary>
        /// Creates <see cref="SourceText"/> from a stream.
        /// </summary>
        /// <param name="stream">The stream to read the text from. Must be readable and seekable. The text is read from the start of the stream.</param>
        /// <param name="defaultEncoding">
        /// Specifies an encoding to be used if the actual encoding can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
        /// If not specified auto-detect heuristics are used to determine the encoding. If these heuristics fail the decoding is assumed to be the system encoding.
        /// Note that if the stream starts with Byte Order Mark the value of <paramref name="defaultEncoding"/> is ignored.
        /// </param>
        /// <param name="checksumAlgorithm">Algorithm to calculate content checksum.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="InvalidDataException">
        /// The stream content can't be decoded using the specified <paramref name="defaultEncoding"/>, or
        /// <paramref name="defaultEncoding"/> is null and the stream appears to be a binary file.
        /// </exception>
        /// <exception cref="IOException">An IO error occurred while reading from the stream.</exception>
        SourceText CreateText(Stream stream, Encoding? defaultEncoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken);

        /// <summary>
        /// Creates <see cref="SourceText"/> from a reader with given <paramref name="encoding"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> to read the text from.</param>
        /// <param name="encoding">Specifies an encoding for the <see cref="SourceText"/>SourceText. 
        /// it could be null. but if null is given, it won't be able to calculate checksum</param>
        /// <param name="checksumAlgorithm">Algorithm to calculate content checksum.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        SourceText CreateText(TextReader reader, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken);
    }
}

