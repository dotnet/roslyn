using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x00
    internal struct ModuleRow
    {
        internal readonly ushort Generation;
        internal readonly uint Name;
        internal readonly uint MVId;
        internal readonly uint EnCId;
        internal readonly uint EnCBaseId;
        internal ModuleRow(
          ushort generation,
          uint name,
          uint moduleVersionId,
          uint encId,
          uint encBaseId)
        {
            this.Generation = generation;
            this.Name = name;
            this.MVId = moduleVersionId;
            this.EnCId = encId;
            this.EnCBaseId = encBaseId;
        }
    }
}