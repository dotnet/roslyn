using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x29
    internal struct NestedClassRow
    {
        internal readonly uint NestedClass;
        internal readonly uint EnclosingClass;
        internal NestedClassRow(
          uint nestedClass,
          uint enclosingClass)
        {
            this.NestedClass = nestedClass;
            this.EnclosingClass = enclosingClass;
        }
    }
}