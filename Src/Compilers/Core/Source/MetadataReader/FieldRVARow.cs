using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x1D
    internal struct FieldRVARow
    {
#if false
    internal readonly int RVA;
    internal readonly uint Field;
    internal FieldRVARow(
      int rva,
      uint field) {
      this.RVA = rva;
      this.Field = field;
    }
#endif
    }
}