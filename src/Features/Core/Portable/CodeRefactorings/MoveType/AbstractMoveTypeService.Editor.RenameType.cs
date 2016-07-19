// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private partial class Editor
        {
            /// <summary>
            /// Renames a type to match its containing file name.
            /// </summary>
            private async Task<IEnumerable<CodeActionOperation>> RenameTypeToMatchFileAsync(Solution solution)
            {
                var symbol = _state.SemanticDocument.SemanticModel.GetDeclaredSymbol(_state.TypeNode, _cancellationToken);
                var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, _state.DocumentName, SemanticDocument.Document.Options, _cancellationToken).ConfigureAwait(false);
                return new CodeActionOperation[] { new ApplyChangesOperation(newSolution) };
            }
        }
    }
}
