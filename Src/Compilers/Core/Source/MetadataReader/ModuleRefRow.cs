using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x1A
    internal struct ModuleRefRow
    {
        internal readonly uint Name;
        internal ModuleRefRow(
          uint name)
        {
            this.Name = name;
        }
    }
}