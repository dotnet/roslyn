﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.Rebuild
{
    public static class Extensions
    {
        internal static void SkipNullTerminator(ref this BlobReader blobReader)
        {
            var b = blobReader.ReadByte();
            if (b != '\0')
            {
                throw new InvalidDataException($"Encountered unexpected byte \"{b}\" when expecting a null terminator");
            }
        }

        public static MetadataReader GetEmbeddedPdbMetadataReader(this PEReader peReader)
        {
            var entry = peReader.ReadDebugDirectory().Single(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            var provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
            return provider.GetMetadataReader();
        }
    }
}
