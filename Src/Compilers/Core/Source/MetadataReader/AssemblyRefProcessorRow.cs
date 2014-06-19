using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x24
    internal struct AssemblyRefProcessorRow
    {
#if false
    internal readonly uint Processor;
    internal readonly uint AssemblyRef;
    internal AssemblyRefProcessorRow(
      uint processor,
      uint assemblyRef) {
      this.Processor = processor;
      this.AssemblyRef = assemblyRef;
    }
#endif
    }
}