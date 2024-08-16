// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineTemporary;

internal abstract class AbstractInlineTemporaryCodeRefactoringProvider<
    TIdentifierNameSyntax,
    TVariableDeclaratorSyntax> : CodeRefactoringProvider
    where TIdentifierNameSyntax : SyntaxNode
    where TVariableDeclaratorSyntax : SyntaxNode
{
    protected static async Task<ImmutableArray<TIdentifierNameSyntax>> GetReferenceLocationsAsync(
        Document document,
        TVariableDeclaratorSyntax variableDeclarator,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var local = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);

        if (local != null)
        {
            // Do not cascade when finding references to this local.  Cascading can cause us to find linked
            // references as well which can throw things off for us.  For inline variable, we only care about the
            // direct real references in this project context.
            var options = FindReferencesSearchOptions.Default with { Cascade = false };

            var findReferencesResult = await SymbolFinder.FindReferencesAsync(
                local, document.Project.Solution, options, cancellationToken).ConfigureAwait(false);
            var referencedSymbol = findReferencesResult.SingleOrDefault(r => Equals(r.Definition, local));
            if (referencedSymbol != null)
            {
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                return referencedSymbol.Locations
                    .Where(loc => !semanticModel.SyntaxTree.OverlapsHiddenPosition(loc.Location.SourceSpan, cancellationToken))
                    .Select(loc => root.FindToken(loc.Location.SourceSpan.Start).Parent as TIdentifierNameSyntax)
                    .WhereNotNull()
                    .ToImmutableArray();
            }
        }

        return [];
    }
}
