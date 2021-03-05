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
        internal static Guid? GetMvidForFile(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var reader = new PEReader(stream);

            if (reader.HasMetadata)
            {
                var metadataReader = reader.GetMetadataReader();
                var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                return metadataReader.GetGuid(mvidHandle);
            }
            else
            {
                return null;
            }
        }
    }
}
