using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct ResourceDirectoryEntry
    {
        internal readonly int NameOrId;
        private readonly int DataOffset;
        internal bool IsDirectory
        {
            get
            {
                return (this.DataOffset & 0x80000000) == 0x80000000;
            }
        }

        internal int OffsetToDirectory
        {
            get
            {
                return this.DataOffset & 0x7FFFFFFF;
            }
        }

        internal int OffsetToData
        {
            get
            {
                return this.DataOffset & 0x7FFFFFFF;
            }
        }

        internal ResourceDirectoryEntry(
          int nameOrId,
          int dataOffset)
        {
            this.NameOrId = nameOrId;
            this.DataOffset = dataOffset;
        }
    }
}