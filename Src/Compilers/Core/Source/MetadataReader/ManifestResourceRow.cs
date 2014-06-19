using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x28
    internal struct ManifestResourceRow
    {
        internal readonly uint Offset;
        internal readonly ManifestResourceFlags Flags;
        internal readonly uint Name;
        internal readonly uint Implementation;
        internal ManifestResourceRow(
          uint offset,
          ManifestResourceFlags flags,
          uint name,
          uint implementation)
        {
            this.Offset = offset;
            this.Flags = flags;
            this.Name = name;
            this.Implementation = implementation;
        }
    }
}