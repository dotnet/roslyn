using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal enum MoveTypeOperationKind
    {
        MoveType,
        MoveTypeScope,
        RenameType,
        RenameFile
    }
}
