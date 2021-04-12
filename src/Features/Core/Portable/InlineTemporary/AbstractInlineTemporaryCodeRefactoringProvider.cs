// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InlineTemporary
{
    internal abstract class AbstractInlineTemporaryCodeRefactoringProvider<TVariableDeclaratorSyntax> : CodeRefactoringProvider
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        protected static async Task<ImmutableArray<ReferenceLocation>> GetReferenceLocationsAsync(
            Document document,
            TVariableDeclaratorSyntax variableDeclarator,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var local = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);

            if (local != null)
            {
                // Do not cascade when finding references to this local.  Cascading can cause us to find linked
                // references as well which can throw things off for us.  For inline variable, we only care about the
                // direct real references in this project context.
                var options = FindReferencesSearchOptions.Default.With(cascade: false);

                var findReferencesResult = await SymbolFinder.FindReferencesAsync(
                    local, document.Project.Solution, options, cancellationToken).ConfigureAwait(false);
                var referencedSymbol = findReferencesResult.SingleOrDefault(r => Equals(r.Definition, local));
                if (referencedSymbol != null)
                {
                    return referencedSymbol.LocationsArray.WhereAsArray(
                        loc => !semanticModel.SyntaxTree.OverlapsHiddenPosition(loc.Location.SourceSpan, cancellationToken));
                }
            }

            return ImmutableArray<ReferenceLocation>.Empty;
        }
    }
}
