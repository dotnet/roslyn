using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x10
    internal struct FieldLayoutRow
    {
#if false
    internal readonly uint Offset;
    internal readonly uint Field;
    internal FieldLayoutRow(
      uint offset,
      uint field) {
      this.Offset = offset;
      this.Field = field;
    }
#endif
    }
}