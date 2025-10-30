// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Hash algorithms supported by the debugger used for source file checksums stored in the PDB.
    /// </summary>
    internal static class SourceHashAlgorithms
    {
        public const SourceHashAlgorithm Default = SourceHashAlgorithm.Sha256;

        /// <summary>
        /// Defines a source hash algorithm constant we can re-use when creating source texts for open documents.
        /// This ensures that both LSP and documents opened as a text buffer are created with the same checksum algorithm
        /// so that we can compare their contents using checksums later on.
        /// </summary>
        public const SourceHashAlgorithm OpenDocumentChecksumAlgorithm = Default;

        private static readonly Guid s_guidSha1 = unchecked(new Guid((int)0xff1816ec, (short)0xaa5e, 0x4d10, 0x87, 0xf7, 0x6f, 0x49, 0x63, 0x83, 0x34, 0x60));
        private static readonly Guid s_guidSha256 = unchecked(new Guid((int)0x8829d00f, 0x11b8, 0x4213, 0x87, 0x8b, 0x77, 0x0e, 0x85, 0x97, 0xac, 0x16));

        public static bool IsSupportedAlgorithm(SourceHashAlgorithm algorithm)
            => algorithm switch
            {
                SourceHashAlgorithm.Sha1 => true,
                SourceHashAlgorithm.Sha256 => true,
                _ => false
            };

        public static Guid GetAlgorithmGuid(SourceHashAlgorithm algorithm)
            => algorithm switch
            {
                SourceHashAlgorithm.Sha1 => s_guidSha1,
                SourceHashAlgorithm.Sha256 => s_guidSha256,
                _ => throw ExceptionUtilities.UnexpectedValue(algorithm),
            };

        public static SourceHashAlgorithm GetSourceHashAlgorithm(Guid guid)
            => (guid == s_guidSha256) ? SourceHashAlgorithm.Sha256 :
               (guid == s_guidSha1) ? SourceHashAlgorithm.Sha1 :
               SourceHashAlgorithm.None;

        private static HashAlgorithm CreateInstance(SourceHashAlgorithm algorithm)
        {
            return algorithm switch
            {
                // CodeQL [SM02196] This is not enabled by default but exists as a compat option for existing builds.
                SourceHashAlgorithm.Sha1 => SHA1.Create(),
                SourceHashAlgorithm.Sha256 => SHA256.Create(),
                _ => throw ExceptionUtilities.UnexpectedValue(algorithm)
            };
        }

        public static HashAlgorithm CreateDefaultInstance()
        {
            return CreateInstance(Default);
        }
    }
}
