// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Services.Rename
{
    [ExportWorkspaceService(typeof(ISymbolRenamedCodeActionOperationFactoryWorkspaceService), ServiceLayer.Host), Shared]
    internal class LanguageServerSymbolRenamedCodeActionOperationFactoryWorkspaceService : ISymbolRenamedCodeActionOperationFactoryWorkspaceService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LanguageServerSymbolRenamedCodeActionOperationFactoryWorkspaceService()
        {
        }

        public CodeActionOperation CreateSymbolRenamedOperation(ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
            => new RenameCodeActionOperation(
                title: string.Format(WorkspacesResources.Rename_0_to_1, symbol.Name, newName),
                updateSolution: updatedSolution);

        private class RenameCodeActionOperation : CodeActionOperation
        {
            private readonly string _title;
            private readonly Solution _updateSolution;

            public RenameCodeActionOperation(string title, Solution updateSolution)
            {
                _title = title;
                _updateSolution = updateSolution;
            }

            public override void Apply(Workspace workspace, CancellationToken cancellationToken = default)
                => workspace.TryApplyChanges(_updateSolution);

            public override string? Title => _title;
        }
    }
}
