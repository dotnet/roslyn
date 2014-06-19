using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x08
    internal struct ParamRow
    {
        internal readonly ParamFlags Flags;
        internal readonly ushort Sequence;
        internal readonly uint Name;
        internal ParamRow(
          ParamFlags flags,
          ushort sequence,
          uint name)
        {
            this.Flags = flags;
            this.Sequence = sequence;
            this.Name = name;
        }
    }
}