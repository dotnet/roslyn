// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [ExportWorkspaceService(typeof(ISymbolRenamedCodeActionOperationFactoryWorkspaceService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioSymbolRenamedCodeActionOperationFactoryWorkspaceService : ISymbolRenamedCodeActionOperationFactoryWorkspaceService
    {
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        [ImportingConstructor]
        public VisualStudioSymbolRenamedCodeActionOperationFactoryWorkspaceService(
            [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices)
        {
            _refactorNotifyServices = refactorNotifyServices;
        }

        public CodeActionOperation CreateSymbolRenamedOperation(ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
        {
            return new RenameSymbolOperation(
                _refactorNotifyServices,
                symbol ?? throw new ArgumentNullException(nameof(symbol)),
                newName ?? throw new ArgumentNullException(nameof(newName)),
                startingSolution ?? throw new ArgumentNullException(nameof(startingSolution)),
                updatedSolution ?? throw new ArgumentNullException(nameof(updatedSolution)));
        }

        private class RenameSymbolOperation : CodeActionOperation
        {
            private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
            private readonly ISymbol _symbol;
            private readonly string _newName;
            private readonly Solution _startingSolution;
            private readonly Solution _updatedSolution;

            public RenameSymbolOperation(
                IEnumerable<IRefactorNotifyService> refactorNotifyServices,
                ISymbol symbol,
                string newName,
                Solution startingSolution,
                Solution updatedSolution)
            {
                _refactorNotifyServices = refactorNotifyServices;
                _symbol = symbol;
                _newName = newName;
                _startingSolution = startingSolution;
                _updatedSolution = updatedSolution;
            }

            public override void Apply(Workspace workspace, CancellationToken cancellationToken = default)
            {
                var updatedDocumentIds = _updatedSolution.GetChanges(_startingSolution).GetProjectChanges().SelectMany(p => p.GetChangedDocuments());

                foreach (var refactorNotifyService in _refactorNotifyServices)
                {
                    // If something goes wrong and some language service rejects the rename, we 
                    // can't really do anything about it because we're potentially in the middle of
                    // some unknown set of CodeActionOperations. This is a best effort approach.

                    if (refactorNotifyService.TryOnBeforeGlobalSymbolRenamed(workspace, updatedDocumentIds, _symbol, _newName, throwOnFailure: false))
                    {
                        refactorNotifyService.TryOnAfterGlobalSymbolRenamed(workspace, updatedDocumentIds, _symbol, _newName, throwOnFailure: false);
                    }
                }
            }

            public override string Title => string.Format(EditorFeaturesResources.Rename_0_to_1, _symbol.Name, _newName);
        }
    }
}
