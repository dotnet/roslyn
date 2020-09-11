// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class NamespaceSymbolReferenceFinder : AbstractReferenceFinder<INamespaceSymbol>
    {
        private static readonly SymbolDisplayFormat s_globalNamespaceFormat = new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Included);

        protected override bool CanFind(INamespaceSymbol symbol)
            => true;

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            INamespaceSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, findInGlobalSuppressions: true, cancellationToken, GetNamespaceIdentifierName(symbol));
        }

        private static string GetNamespaceIdentifierName(INamespaceSymbol symbol)
        {
            return symbol.IsGlobalNamespace
                ? symbol.ToDisplayString(s_globalNamespaceFormat)
                : symbol.Name;
        }

        protected override async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            INamespaceSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var identifierName = GetNamespaceIdentifierName(symbol);
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            var tokens = await GetIdentifierOrGlobalNamespaceTokensWithTextAsync(
                document, semanticModel, identifierName, cancellationToken).ConfigureAwait(false);
            var nonAliasReferences = await FindReferencesInTokensAsync(symbol,
                document,
                semanticModel,
                tokens,
                t => syntaxFactsService.TextMatch(t.ValueText, identifierName),
                cancellationToken).ConfigureAwait(false);

            var aliasReferences = await FindAliasReferencesAsync(nonAliasReferences, symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            var suppressionReferences = ShouldFindReferencesInGlobalSuppressions(symbol, out var docCommentId)
                ? await FindReferencesInDocumentInsideGlobalSuppressionsAsync(document, semanticModel,
                    syntaxFactsService, docCommentId, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            return nonAliasReferences.Concat(aliasReferences).Concat(suppressionReferences);
        }
    }
}
