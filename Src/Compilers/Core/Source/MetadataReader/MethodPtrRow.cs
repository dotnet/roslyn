using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x05
    internal struct MethodPtrRow
    {
#if false
    internal readonly uint Method;
    internal MethodPtrRow(
      uint method) {
      this.Method = method;
    }
#endif
    }
}