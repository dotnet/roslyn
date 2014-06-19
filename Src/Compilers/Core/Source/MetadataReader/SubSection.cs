using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct SubSection
    {
        internal readonly string SectionName;
        internal readonly uint Offset;
        internal readonly MemoryBlock MemoryBlock;
        internal SubSection(
          string sectionName,
          uint offset,
          MemoryBlock memoryBlock)
        {
            this.SectionName = sectionName;
            this.Offset = offset;
            this.MemoryBlock = memoryBlock;
        }

        internal SubSection(
          string sectionName,
          int offset,
          MemoryBlock memoryBlock)
        {
            this.SectionName = sectionName;
            this.Offset = (uint)offset;
            this.MemoryBlock = memoryBlock;
        }
    }
}