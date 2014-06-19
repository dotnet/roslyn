using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x1C
    internal struct ImplMapRow
    {
        internal readonly PInvokeMapFlags PInvokeMapFlags;
        internal readonly uint MemberForwarded;
        internal readonly uint ImportName;
        internal readonly uint ImportScope;
        internal ImplMapRow(
          PInvokeMapFlags mapFlags,
          uint memberForwarded,
          uint importName,
          uint importScope)
        {
            this.PInvokeMapFlags = mapFlags;
            this.MemberForwarded = memberForwarded;
            this.ImportName = importName;
            this.ImportScope = importScope;
        }
    }
}