using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x0A
    internal struct MemberRefRow
    {
        internal readonly uint Class;
        internal readonly uint Name;
        internal readonly uint Signature;
        internal MemberRefRow(
          uint @class,
          uint name,
          uint signature)
        {
            this.Class = @class;
            this.Name = name;
            this.Signature = signature;
        }
    }
}