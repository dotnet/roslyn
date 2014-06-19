using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x1B
    internal struct TypeSpecRow
    {
#if false
    internal readonly uint Signature;
    internal TypeSpecRow(
      uint signature) {
      this.Signature = signature;
    }
#endif
    }
}