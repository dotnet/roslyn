// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Debugging
{
    /// <summary>
    /// Hash algorithms supported by the debugger used for source file checksums stored in the PDB.
    /// </summary>
    internal static class SourceHashAlgorithms
    {
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
    }
}
