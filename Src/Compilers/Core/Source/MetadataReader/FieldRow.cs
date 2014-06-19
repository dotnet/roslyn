using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x04
    internal struct FieldRow
    {
        internal readonly FieldFlags Flags;
        internal readonly uint Name;
        internal readonly uint Signature;
        internal FieldRow(
          FieldFlags flags,
          uint name,
          uint signature)
        {
            this.Flags = flags;
            this.Name = name;
            this.Signature = signature;
        }
    }
}