using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x0D
    internal struct FieldMarshalRow
    {
        internal readonly uint Parent;
        internal readonly uint NativeType;
        internal FieldMarshalRow(
          uint parent,
          uint nativeType)
        {
            this.Parent = parent;
            this.NativeType = nativeType;
        }
    }
}