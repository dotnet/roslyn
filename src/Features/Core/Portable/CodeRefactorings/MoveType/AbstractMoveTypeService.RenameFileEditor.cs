// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
{
    private class RenameFileEditor(TService service, State state, string fileName, CancellationToken cancellationToken) : Editor(service, state, fileName, cancellationToken)
    {
        public override Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
            => Task.FromResult(RenameFileToMatchTypeName());

        public override Task<Solution> GetModifiedSolutionAsync()
        {
            var modifiedSolution = SemanticDocument.Project.Solution
                .WithDocumentName(SemanticDocument.Document.Id, FileName);

            return Task.FromResult(modifiedSolution);
        }

        /// <summary>
        /// Renames the file to match the type contained in it.
        /// </summary>
        private ImmutableArray<CodeActionOperation> RenameFileToMatchTypeName()
        {
            var documentId = SemanticDocument.Document.Id;
            var oldSolution = SemanticDocument.Document.Project.Solution;
            var newSolution = oldSolution.WithDocumentName(documentId, FileName);

            return [new ApplyChangesOperation(newSolution)];
        }
    }
}
