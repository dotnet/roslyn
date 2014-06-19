using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x18
    internal struct MethodSemanticsRow
    {
        internal readonly MethodSemanticsFlags SemanticsFlag;
        internal readonly uint Method;
        internal readonly uint Association;
        internal MethodSemanticsRow(
          MethodSemanticsFlags semanticsFlag,
          uint method,
          uint association)
        {
            this.SemanticsFlag = semanticsFlag;
            this.Method = method;
            this.Association = association;
        }
    }
}