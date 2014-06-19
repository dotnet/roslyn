using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x0F
    internal struct ClassLayoutRow
    {
#if false
    internal readonly ushort PackingSize;
    internal readonly uint ClassSize;
    internal readonly uint Parent;
    internal ClassLayoutRow(
      ushort packingSize,
      uint classSize,
      uint parent) {
      this.PackingSize = packingSize;
      this.ClassSize = classSize;
      this.Parent = parent;
    }
#endif
    }
}