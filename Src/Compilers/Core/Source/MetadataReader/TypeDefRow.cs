using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x02
    internal struct TypeDefRow
    {
        internal readonly TypeDefFlags Flags;
        internal readonly uint Name;
        internal readonly uint Namespace;
        internal readonly uint Extends;
        internal readonly uint FieldList;
        internal readonly uint MethodList;

        internal TypeDefRow(
          TypeDefFlags flags,
          uint name,
          uint @namespace,
          uint extends,
          uint fieldList,
          uint methodList)
        {
            this.Flags = flags;
            this.Name = name;
            this.Namespace = @namespace;
            this.Extends = extends;
            this.FieldList = fieldList;
            this.MethodList = methodList;
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