using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x2A
    internal struct GenericParamRow
    {
        internal readonly ushort Number;
        internal readonly GenericParamFlags Flags;
        internal readonly uint Owner;
        internal readonly uint Name;
        internal GenericParamRow(
          ushort number,
          GenericParamFlags flags,
          uint owner,
          uint name)
        {
            this.Number = number;
            this.Flags = flags;
            this.Owner = owner;
            this.Name = name;
        }
    }
}