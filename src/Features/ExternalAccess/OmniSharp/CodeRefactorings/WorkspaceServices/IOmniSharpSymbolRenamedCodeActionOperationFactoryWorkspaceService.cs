// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeRefactorings.WorkspaceServices;

internal interface IOmniSharpSymbolRenamedCodeActionOperationFactoryWorkspaceService
{
    CodeActionOperation CreateSymbolRenamedOperation(ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution);
}
