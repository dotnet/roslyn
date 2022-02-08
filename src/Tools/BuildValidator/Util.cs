// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BuildValidator
{
    internal static class Util
    {
        internal static PortableExecutableInfo? GetPortableExecutableInfo(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var peReader = new PEReader(stream);
            if (GetMvid(peReader) is { } mvid)
            {
                var isReadyToRun = IsReadyToRunImage(peReader);
                return new PortableExecutableInfo(filePath, mvid, isReadyToRun);
            }

            return null;
        }

        internal static Guid? GetMvid(PEReader peReader)
        {
            if (peReader.HasMetadata)
            {
                var metadataReader = peReader.GetMetadataReader();
                var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                return metadataReader.GetGuid(mvidHandle);
            }
            else
            {
                return null;
            }
        }

        internal static bool IsReadyToRunImage(PEReader peReader)
        {
            if (peReader.PEHeaders is null ||
                peReader.PEHeaders.PEHeader is null ||
                peReader.PEHeaders.CorHeader is null)
            {
                return false;
            }

            if ((peReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) == 0)
            {
                PEExportTable exportTable = peReader.GetExportTable();
                return exportTable.TryGetValue("RTR_HEADER", out _);
            }
            else
            {
                return peReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size != 0;
            }
        }
    }
}
