// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

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
