// Licensed to the .NET Foundation under one or more agreements.
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
                throw new InvalidDataException(string.Format(RebuildResources.Encountered_unexpected_byte_0_when_expecting_a_null_terminator, b));
            }
        }

        public static MetadataReader? GetEmbeddedPdbMetadataReader(this PEReader peReader)
        {
            var entry = peReader.ReadDebugDirectory().SingleOrDefault(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            if (entry.Type == DebugDirectoryEntryType.Unknown)
            {
                return null;
            }

            var provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
            return provider.GetMetadataReader();
        }
    }
}
