using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x01
    internal struct TypeRefRow
    {
        internal readonly uint ResolutionScope;
        internal readonly uint Name;
        internal readonly uint Namespace;
        internal TypeRefRow(
          uint resolutionScope,
          uint name,
          uint @namespace)
        {
            this.ResolutionScope = resolutionScope;
            this.Name = name;
            this.Namespace = @namespace;
        }
    }
}