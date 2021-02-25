// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BuildValidator
{
    internal static class Extensions
    {
#if !NETCOREAPP
        internal static void Write(this FileStream stream, ReadOnlySpan<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                stream.WriteByte(span[i]);
            }
        }
#endif

        internal static MetadataReader GetEmbeddedPdbMetadataReader(this PEReader peReader)
        {
            var entry = peReader.ReadDebugDirectory().Single(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            var provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
            return provider.GetMetadataReader();
        }
    }
}
