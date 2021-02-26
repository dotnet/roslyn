﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BuildValidator
{
    internal static class Extensions
    {
#if !NETCOREAPP
        internal static unsafe void Write(this FileStream stream, ReadOnlySpan<byte> span)
        {
            fixed (byte* dataPointer = span)
            {
                using var unmanagedStream = new UnmanagedMemoryStream(dataPointer, span.Length);
                unmanagedStream.CopyTo(stream);
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
