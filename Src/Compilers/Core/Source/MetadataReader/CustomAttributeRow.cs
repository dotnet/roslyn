using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x0C
    internal struct CustomAttributeRow
    {
        internal readonly uint Parent;
        internal readonly uint Type;
        internal readonly uint Value;
        internal CustomAttributeRow(
          uint parent,
          uint type,
          uint value)
        {
            this.Parent = parent;
            this.Type = type;
            this.Value = value;
        }
    }
}