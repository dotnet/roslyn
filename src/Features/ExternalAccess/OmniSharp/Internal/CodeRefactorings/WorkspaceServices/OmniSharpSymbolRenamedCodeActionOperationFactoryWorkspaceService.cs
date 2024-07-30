// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeRefactorings.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Internal.CodeRefactorings.WorkspaceServices
{
    [Shared]
    [ExportWorkspaceService(typeof(ISymbolRenamedCodeActionOperationFactoryWorkspaceService))]
    internal sealed class OmniSharpSymbolRenamedCodeActionOperationFactoryWorkspaceService : ISymbolRenamedCodeActionOperationFactoryWorkspaceService
    {
        private readonly IOmniSharpSymbolRenamedCodeActionOperationFactoryWorkspaceService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OmniSharpSymbolRenamedCodeActionOperationFactoryWorkspaceService(IOmniSharpSymbolRenamedCodeActionOperationFactoryWorkspaceService service)
        {
            _service = service;
        }

        public CodeActionOperation CreateSymbolRenamedOperation(ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
        {
            return _service.CreateSymbolRenamedOperation(symbol, newName, startingSolution, updatedSolution);
        }
    }
}
