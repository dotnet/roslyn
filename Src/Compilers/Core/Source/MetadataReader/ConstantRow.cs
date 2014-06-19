using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x0B
    internal struct ConstantRow
    {
        internal readonly byte Type;
        internal readonly uint Parent;
        internal readonly uint Value;
        internal ConstantRow(
          byte type,
          uint parent,
          uint value)
        {
            this.Type = type;
            this.Parent = parent;
            this.Value = value;
        }
    }
}