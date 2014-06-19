using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x2C
    internal struct GenericParamConstraintRow
    {
#if false
    internal readonly uint Owner;
    internal readonly uint Constraint;
    internal GenericParamConstraintRow(
      uint owner,
      uint constraint) {
      this.Owner = owner;
      this.Constraint = constraint;
    }
#endif
    }
}