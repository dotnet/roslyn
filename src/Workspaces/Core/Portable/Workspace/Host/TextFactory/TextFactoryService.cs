// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Intentionally not exported.  This instance can instead be used whenever a specialized instance is not provided
    /// in the mef composition.
    /// </summary>
    internal sealed class TextFactoryService : ITextFactoryService
    {
        public static readonly ITextFactoryService Default = new TextFactoryService();

        private TextFactoryService()
        {
        }

        public SourceText CreateText(Stream stream, Encoding? defaultEncoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return EncodedStringText.Create(stream, defaultEncoding, checksumAlgorithm);
        }

        public SourceText CreateText(TextReader reader, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return (reader is TextReaderWithLength textReaderWithLength)
                ? SourceText.From(textReaderWithLength, textReaderWithLength.Length, encoding, checksumAlgorithm)
                : SourceText.From(reader.ReadToEnd(), encoding, checksumAlgorithm);
        }
    }
}

