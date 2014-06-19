using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x2B
    internal struct MethodSpecRow
    {
        internal readonly uint Method;
        internal readonly uint Instantiation;
        internal MethodSpecRow(
          uint method,
          uint instantiation)
        {
            this.Method = method;
            this.Instantiation = instantiation;
        }
    }
}