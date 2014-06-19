using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x16
    internal struct PropertyPtrRow
    {
#if false
    internal readonly uint Property;
    internal PropertyPtrRow(
      uint property) {
      this.Property = property;
    }
#endif
    }
}