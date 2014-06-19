using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x26
    internal struct FileRow
    {
        internal readonly FileFlags Flags;
        internal readonly uint Name;
        internal readonly uint HashValue;
        internal FileRow(
          FileFlags flags,
          uint name,
          uint hashValue)
        {
            this.Flags = flags;
            this.Name = name;
            this.HashValue = hashValue;
        }
    }
}