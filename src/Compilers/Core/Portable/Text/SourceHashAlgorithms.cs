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
        private static readonly Guid s_guidSha384 = unchecked(new Guid((int)0xd99cfeb1, (short)0x8c43, 0x444a, 0x8a, 0x6c, 0xb6, 0x12, 0x69, 0xd2, 0xa0, 0xbf));
        private static readonly Guid s_guidSha512 = unchecked(new Guid((int)0xef2d1afc, 0x6550, 0x46d6, 0xb1, 0x4b, 0xd7, 0x0a, 0xfe, 0x9a, 0x55, 0x66));

        public static bool IsSupportedAlgorithm(SourceHashAlgorithm algorithm)
            => algorithm switch
            {
                SourceHashAlgorithm.Sha1 => true,
                SourceHashAlgorithm.Sha256 => true,
                SourceHashAlgorithm.Sha384 => true,
                SourceHashAlgorithm.Sha512 => true,
                _ => false
            };

        public static Guid GetAlgorithmGuid(SourceHashAlgorithm algorithm)
            => algorithm switch
            {
                SourceHashAlgorithm.Sha1 => s_guidSha1,
                SourceHashAlgorithm.Sha256 => s_guidSha256,
                SourceHashAlgorithm.Sha384 => s_guidSha384,
                SourceHashAlgorithm.Sha512 => s_guidSha512,
                _ => throw ExceptionUtilities.UnexpectedValue(algorithm),
            };

        public static SourceHashAlgorithm GetSourceHashAlgorithm(Guid guid)
            => (guid == s_guidSha256) ? SourceHashAlgorithm.Sha256 :
               (guid == s_guidSha1) ? SourceHashAlgorithm.Sha1 :
               (guid == s_guidSha384) ? SourceHashAlgorithm.Sha384 :
               (guid == s_guidSha512) ? SourceHashAlgorithm.Sha512 :
               SourceHashAlgorithm.None;

        private static HashAlgorithm CreateInstance(SourceHashAlgorithm algorithm)
        {
            return algorithm switch
            {
                // CodeQL [SM02196] This is not enabled by default but exists as a compat option for existing builds.
                SourceHashAlgorithm.Sha1 => SHA1.Create(),
                SourceHashAlgorithm.Sha256 => SHA256.Create(),
                SourceHashAlgorithm.Sha384 => SHA384.Create(),
                SourceHashAlgorithm.Sha512 => SHA512.Create(),
                _ => throw ExceptionUtilities.UnexpectedValue(algorithm)
            };
        }

        public static HashAlgorithm CreateDefaultInstance()
        {
            return CreateInstance(Default);
        }

        public static bool TryParseAlgorithmName(string name, out SourceHashAlgorithm algorithm)
        {
            if (string.Equals("sha1", name, StringComparison.OrdinalIgnoreCase))
            {
                algorithm = SourceHashAlgorithm.Sha1;
                return true;
            }

            if (string.Equals("sha256", name, StringComparison.OrdinalIgnoreCase))
            {
                algorithm = SourceHashAlgorithm.Sha256;
                return true;
            }

            if (string.Equals("sha384", name, StringComparison.OrdinalIgnoreCase))
            {
                algorithm = SourceHashAlgorithm.Sha384;
                return true;
            }

            if (string.Equals("sha512", name, StringComparison.OrdinalIgnoreCase))
            {
                algorithm = SourceHashAlgorithm.Sha512;
                return true;
            }

            algorithm = SourceHashAlgorithm.None;
            return false;
        }
    }
}
