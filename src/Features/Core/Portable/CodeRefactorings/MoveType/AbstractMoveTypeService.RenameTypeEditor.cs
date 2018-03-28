// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Rename;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class RenameTypeEditor : Editor
        {
            public RenameTypeEditor(TService service, State state, string fileName, CancellationToken cancellationToken)
                : base(service, state, fileName, cancellationToken)
            {
            }

            internal override Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
            {
                return RenameTypeToMatchFileAsync();
            }

            /// <summary>
            /// Renames a type to match its containing file name.
            /// </summary>
            private async Task<ImmutableArray<CodeActionOperation>> RenameTypeToMatchFileAsync()
            {
                // TODO: detect conflicts ahead of time and open an inline rename session if any exists.
                // this will bring up dashboard with conflicts and will allow the user to resolve them.
                // if no such conflicts exist, proceed with RenameSymbolAsync.
                var solution = SemanticDocument.Document.Project.Solution;
                var symbol = State.SemanticDocument.SemanticModel.GetDeclaredSymbol(State.TypeNode, CancellationToken);
                var documentOptions = await SemanticDocument.Document.GetOptionsAsync(CancellationToken).ConfigureAwait(false);
                var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, FileName, documentOptions, CancellationToken).ConfigureAwait(false);
                return ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(newSolution));
            }
        }
    }
}
