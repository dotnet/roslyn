using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x19
    internal struct MethodImplRow
    {
        internal readonly uint Class;
        internal readonly uint MethodBody;
        internal readonly uint MethodDeclaration;
        internal MethodImplRow(
          uint @class,
          uint methodBody,
          uint methodDeclaration)
        {
            this.Class = @class;
            this.MethodBody = methodBody;
            this.MethodDeclaration = methodDeclaration;
        }
    }
}