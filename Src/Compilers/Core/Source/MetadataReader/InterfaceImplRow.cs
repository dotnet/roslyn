using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x09
    internal struct InterfaceImplRow
    {
#if false
    internal readonly uint Class;
    internal readonly uint Interface;
    internal InterfaceImplRow(
      uint @class,
      uint @interface) {
      this.Class = @class;
      this.Interface = @interface;
    }
#endif
    }
}