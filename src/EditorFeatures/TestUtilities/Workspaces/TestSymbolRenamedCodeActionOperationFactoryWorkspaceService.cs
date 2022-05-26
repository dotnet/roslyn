// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [ExportWorkspaceService(typeof(ISymbolRenamedCodeActionOperationFactoryWorkspaceService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    public class TestSymbolRenamedCodeActionOperationFactoryWorkspaceService : ISymbolRenamedCodeActionOperationFactoryWorkspaceService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestSymbolRenamedCodeActionOperationFactoryWorkspaceService()
        {
        }

        public CodeActionOperation CreateSymbolRenamedOperation(ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
            => new Operation(symbol, newName, startingSolution, updatedSolution);

        public class Operation : CodeActionOperation
        {
            public ISymbol _symbol;
            public string _newName;
            public Solution _startingSolution;
            public Solution _updatedSolution;

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
