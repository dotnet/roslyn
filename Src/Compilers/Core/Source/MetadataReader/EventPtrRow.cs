using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x13
    internal struct EventPtrRow
    {
#if false
    internal readonly uint Event;
    internal EventPtrRow(
      uint @event) {
      this.Event = @event;
    }
#endif
    }
}