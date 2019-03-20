// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class RenameFileEditor : Editor
        {
            public RenameFileEditor(TService service, State state, string fileName, CancellationToken cancellationToken)
                : base(service, state, fileName, cancellationToken)
            {
            }

            internal override Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
                => RenameFileToMatchTypeNameAsync();

            /// <summary>
            /// Renames the file to match the type contained in it.
            /// </summary>
            private async Task<ImmutableArray<CodeActionOperation>> RenameFileToMatchTypeNameAsync()
            {
                // What I actually want to do:
                //var documentId = SemanticDocument.Document.Id;
                //var oldSolution = SemanticDocument.Document.Project.Solution;
                //var newSolution = oldSolution.WithDocumentName(documentId, FileName);

                //return ImmutableArray.Create<CodeActionOperation>(
                //    new ApplyChangesOperation(newSolution));

                // Just testing things out
                var solution = SemanticDocument.Document.Project.Solution;
                var symbol = State.SemanticDocument.SemanticModel.GetDeclaredSymbol(State.TypeNode, CancellationToken);
                var documentOptions = await SemanticDocument.Document.GetOptionsAsync(CancellationToken).ConfigureAwait(false);
                var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, "WOW", documentOptions, CancellationToken).ConfigureAwait(false);
                newSolution = newSolution.WithDocumentName(SemanticDocument.Document.Id, FileName);
                return ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(newSolution));
            }
        }
    }
}
