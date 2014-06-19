using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x15
    internal struct PropertyMapRow
    {
#if false
    internal readonly uint Parent;
    internal readonly uint PropertyList;
    internal PropertyMapRow(
      uint parent,
      uint propertyList) {
      this.Parent = parent;
      this.PropertyList = propertyList;
    }
#endif
    }
}