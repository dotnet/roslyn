// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class RenameTypeEditor : Editor
        {
            public RenameTypeEditor(TService service, State state, CancellationToken cancellationToken)
                : base(service, state, cancellationToken)
            {
            }

            internal override Task<IEnumerable<CodeActionOperation>> GetOperationsAsync()
            {
                var solution = SemanticDocument.Document.Project.Solution;
                return RenameTypeToMatchFileAsync(solution);
            }

            /// <summary>
            /// Renames a type to match its containing file name.
            /// </summary>
            private async Task<IEnumerable<CodeActionOperation>> RenameTypeToMatchFileAsync(Solution solution)
            {
                // TODO: detect conflicts ahead of time and open an inline rename session if any exists.
                // this will bring up dashboard with conflicts and will allow the user to resolve them.
                // if no such conflicts exist, proceed with RenameSymbolAsync.
                var symbol = State.SemanticDocument.SemanticModel.GetDeclaredSymbol(State.TypeNode, CancellationToken);
                var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, State.DocumentName, SemanticDocument.Document.Options, CancellationToken).ConfigureAwait(false);
                return new CodeActionOperation[] { new ApplyChangesOperation(newSolution) };
            }
        }
    }
}
