// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rebuild
{
    public readonly struct SourceFileInfo
    {
        public string SourceFilePath { get; }
        public SourceHashAlgorithm HashAlgorithm { get; }
        public byte[] Hash { get; }
        public SourceText? EmbeddedText { get; }
        public byte[]? EmbeddedCompressedHash { get; }

        public SourceFileInfo(
            string sourceFilePath,
            SourceHashAlgorithm hashAlgorithm,
            byte[] hash,
            SourceText? embeddedText,
            byte[]? embeddedCompressedHash)
        {
            SourceFilePath = sourceFilePath;
            HashAlgorithm = hashAlgorithm;
            Hash = hash;
            EmbeddedText = embeddedText;
            EmbeddedCompressedHash = embeddedCompressedHash;
        }
    }
}