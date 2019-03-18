// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal enum MoveTypeOperationKind
    {
        /// <summary>
        /// Moves a type to it's own file
        /// </summary>
        MoveType,

        /// <summary>
        /// Functionally doesn't change the type symbol, but moves it to it's own
        /// namespace declaration scope. 
        /// </summary>
        MoveTypeNamespaceScope,

        /// <summary>
        /// Renames the target type
        /// </summary>
        RenameType,

        /// <summary>
        /// Renames the file containing the target type
        /// </summary>
        RenameFile
    }
}
