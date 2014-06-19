using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x25
    internal struct AssemblyRefOSRow
    {
#if false
    internal readonly uint OSPlatformId;
    internal readonly uint OSMajorVersionId;
    internal readonly uint OSMinorVersionId;
    internal readonly uint AssemblyRef;
    internal AssemblyRefOSRow(
      uint osPlatformId,
      uint osMajorVersionId,
      uint osMinorVersionId,
      uint assemblyRef) {
      this.OSPlatformId = osPlatformId;
      this.OSMajorVersionId = osMajorVersionId;
      this.OSMinorVersionId = osMinorVersionId;
      this.AssemblyRef = assemblyRef;
    }
#endif
    }
}