// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [ExportWorkspaceService(typeof(ISymbolRenamedCodeActionOperationFactoryWorkspaceService), TestWorkspace.WorkspaceName), Shared]
    public class TestSymbolRenamedCodeActionOperationFactoryWorkspaceService : ISymbolRenamedCodeActionOperationFactoryWorkspaceService
    {
        public CodeActionOperation CreateSymbolRenamedOperation(ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
        {
            return new Operation(symbol, newName, startingSolution, updatedSolution);
        }

        public class Operation : CodeActionOperation
        {
            private ISymbol _symbol;
            private string _newName;
            private Solution _startingSolution;
            private Solution _updatedSolution;

            public Operation(ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
            {
                _symbol = symbol;
                _newName = newName;
                _startingSolution = startingSolution;
                _updatedSolution = updatedSolution;
            }
        }
    }
}
