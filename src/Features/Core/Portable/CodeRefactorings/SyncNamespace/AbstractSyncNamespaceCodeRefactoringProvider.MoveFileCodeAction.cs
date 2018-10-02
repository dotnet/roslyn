// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

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
            private readonly string _newPath;

            public override string Title => "Move file to new folder to match namespace declaration.";

            public MoveFileCodeAction(State state, string newPath)
            {
                _state = state;
                _newPath = newPath;
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                => await MoveFileToMatchNamespaceAsync(cancellationToken).ConfigureAwait(false);

            private async Task<ImmutableArray<CodeActionOperation>> MoveFileToMatchNamespaceAsync(CancellationToken cancellationToken)
            {
                var oldDocument = _state.Document;
                var newDocumentId = DocumentId.CreateNewId(oldDocument.Project.Id, oldDocument.Name);

                var newSolution = oldDocument.Project.Solution.RemoveDocument(oldDocument.Id);
                var newFolders = _state.RelativeDeclaredNamespace.Split(new[] { '.' }).ToArray();

                var text = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                newSolution = newSolution.AddDocument(newDocumentId, oldDocument.Name, text, newFolders, _newPath);

                return ImmutableArray.Create<CodeActionOperation>(
                    new ApplyChangesOperation(newSolution),
                    new OpenDocumentOperation(newDocumentId, activateIfAlreadyOpen: true));
            }

            // TODO: 
            // 1. search and provide options to use existing folders
            // 2. Handle MTFM project
            public static ImmutableArray<MoveFileCodeAction> Create(State state)
            {
                var document = state.Document;
                var newFolders = state.RelativeDeclaredNamespace.Split(new[] { '.' }).ToArray();

                var newRelativePath = Path.Combine(Path.Combine(newFolders.ToArray()), PathUtilities.GetFileName(document.FilePath));
                var newFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(PathUtilities.GetDirectoryName(document.Project.FilePath), newRelativePath);

                return File.Exists(newFilePath) 
                    ? ImmutableArray<MoveFileCodeAction>.Empty 
                    : ImmutableArray.Create(new MoveFileCodeAction(state, newFilePath));
            }
        }
    }
}
