using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x14
    internal struct EventRow
    {
        internal readonly EventFlags Flags;
        internal readonly uint Name;
        internal readonly uint EventType;
        internal EventRow(
          EventFlags flags,
          uint name,
          uint eventType)
        {
            this.Flags = flags;
            this.Name = name;
            this.EventType = eventType;
        }
    }
}