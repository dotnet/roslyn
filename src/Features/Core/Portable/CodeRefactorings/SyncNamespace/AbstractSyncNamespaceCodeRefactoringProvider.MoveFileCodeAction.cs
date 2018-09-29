// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TService : AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode 
    {
        private class MoveFileCodeAction : CodeAction
        {
            private readonly State _state;
            private readonly TService _service;

            public override string Title => "Move file to new folder to match namespace declaration.";

            public MoveFileCodeAction(TService service, State state)
            {
                _service = service;
                _state = state;
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                => await MoveFileToMatchNamespaceAsync(cancellationToken).ConfigureAwait(false);

            private async Task<ImmutableArray<CodeActionOperation>> MoveFileToMatchNamespaceAsync(CancellationToken cancellationToken)
            {
                // TODO: search and provide options to use existing folders

                var oldDocument = _state.Document;
                var newDocumentId = DocumentId.CreateNewId(oldDocument.Project.Id, oldDocument.Name);

                var newSolution = oldDocument.Project.Solution.RemoveDocument(oldDocument.Id);
                var newFolders = _state.RelativeDeclaredNamespace.Split(new[] { '.' }).ToArray();
                var newFilePath = Path.Combine(Path.GetDirectoryName(oldDocument.Project.FilePath), Path.Combine(newFolders.ToArray()), Path.GetFileName(oldDocument.FilePath));

                var text = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                newSolution = newSolution.AddDocument(newDocumentId, oldDocument.Name, text, newFolders, newFilePath);

                return ImmutableArray.Create<CodeActionOperation>(
                    new ApplyChangesOperation(newSolution),
                    new OpenDocumentOperation(newDocumentId, activateIfAlreadyOpen: true));
            }
        }
    }
}
