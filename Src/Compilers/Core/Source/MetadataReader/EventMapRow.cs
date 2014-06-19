using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x12
    internal struct EventMapRow
    {
#if false
    internal readonly uint Parent;
    internal readonly uint EventList;
    internal EventMapRow(
      uint parent,
      uint eventList) {
      this.Parent = parent;
      this.EventList = eventList;
    }
#endif
    }
}