using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x27
    internal struct ExportedTypeRow
    {
        internal readonly TypeDefFlags Flags;
        internal readonly uint TypeDefId;
        internal readonly uint TypeName;
        internal readonly uint TypeNamespace;
        internal readonly uint Implementation;
        internal ExportedTypeRow(
          TypeDefFlags typeDefFlags,
          uint typeDefId,
          uint typeName,
          uint typeNamespace,
          uint implementation)
        {
            this.Flags = typeDefFlags;
            this.TypeDefId = typeDefId;
            this.TypeName = typeName;
            this.TypeNamespace = typeNamespace;
            this.Implementation = implementation;
        }

        internal bool IsNested
        {
            get
            {
                return (this.Flags & TypeDefFlags.NestedMask) != 0;
            }
        }
    }
}