// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.InternalUtilities
{
    internal static class PEReaderExtensions
    {
        public static int GetTimestamp(this PEReader peReader)
            => peReader.PEHeaders.CoffHeader.TimeDateStamp;

        public static int GetSizeOfImage(this PEReader peReader)
            => peReader.PEHeaders.PEHeader.SizeOfImage;

        public static Guid GetMvid(this PEReader peReader)
        {
            var metadataReader = peReader.GetMetadataReader();
            var moduleDefinition = peReader.GetMetadataReader().GetModuleDefinition();
            return metadataReader.GetGuid(moduleDefinition.Mvid);
        }
    }
}
