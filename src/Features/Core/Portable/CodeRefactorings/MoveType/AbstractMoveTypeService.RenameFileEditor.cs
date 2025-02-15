// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
{
    private sealed class RenameFileEditor(TService service, State state, string fileName, CancellationToken cancellationToken) : Editor(service, state, fileName, cancellationToken)
    {
        /// <summary>
        /// Renames the file to match the type contained in it.
        /// </summary>
        public override async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
        {
            var newSolution = await GetModifiedSolutionAsync().ConfigureAwait(false);
            return [new ApplyChangesOperation(newSolution)];
        }

        public override Task<Solution> GetModifiedSolutionAsync()
        {
            var modifiedSolution = SemanticDocument.Project.Solution
                .WithDocumentName(SemanticDocument.Document.Id, FileName);

            return Task.FromResult(modifiedSolution);
        }
    }
}
