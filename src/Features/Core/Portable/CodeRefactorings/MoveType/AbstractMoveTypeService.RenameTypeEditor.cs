// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
{
    private sealed class RenameTypeEditor(
        TService service,
        SemanticDocument document,
        TTypeDeclarationSyntax typeDeclaration,
        string fileName,
        CancellationToken cancellationToken) : Editor(service, document, typeDeclaration, fileName, cancellationToken)
    {
        /// <summary>
        /// Renames a type to match its containing file name.
        /// </summary>
        public override async Task<Solution?> GetModifiedSolutionAsync()
        {
            // TODO: detect conflicts ahead of time and open an inline rename session if any exists.
            // this will bring up dashboard with conflicts and will allow the user to resolve them.
            // if no such conflicts exist, proceed with RenameSymbolAsync.
            var solution = SemanticDocument.Project.Solution;
            var symbol = SemanticDocument.SemanticModel.GetRequiredDeclaredSymbol(this.TypeDeclaration, CancellationToken);
            return await Renamer.RenameSymbolAsync(solution, symbol, new SymbolRenameOptions(), FileName, CancellationToken).ConfigureAwait(false);
        }
    }
}
