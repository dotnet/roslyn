using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x06
    internal struct MethodRow
    {
        internal readonly int RVA;
        internal readonly MethodImplFlags ImplFlags;
        internal readonly MethodFlags Flags;
        internal readonly uint Name;
        internal readonly uint Signature;
        internal readonly uint ParamList;
        internal MethodRow(
          int rva,
          MethodImplFlags implFlags,
          MethodFlags flags,
          uint name,
          uint signature,
          uint paramList)
        {
            this.RVA = rva;
            this.ImplFlags = implFlags;
            this.Flags = flags;
            this.Name = name;
            this.Signature = signature;
            this.ParamList = paramList;
        }
    }
}