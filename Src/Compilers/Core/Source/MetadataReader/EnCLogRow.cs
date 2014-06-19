using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x1E
    internal struct EnCLogRow
    {
#if false
    internal readonly uint Token;
    internal readonly uint FuncCode;
    internal EnCLogRow(
      uint token,
      uint funcCode) {
      this.Token = token;
      this.FuncCode = funcCode;
    }
#endif
    }
}