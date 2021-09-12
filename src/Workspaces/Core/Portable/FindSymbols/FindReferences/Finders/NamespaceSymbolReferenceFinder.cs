// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class NamespaceSymbolReferenceFinder : AbstractReferenceFinder<INamespaceSymbol>
    {
        private static readonly SymbolDisplayFormat s_globalNamespaceFormat = new(SymbolDisplayGlobalNamespaceStyle.Included);

        protected override bool CanFind(INamespaceSymbol symbol)
            => true;

        protected override Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(INamespaceSymbol symbol, Project project, CancellationToken cancellationToken)
        {
            return GetAllMatchingGlobalAliasNamesAsync(project, symbol.Name, arity: 0, cancellationToken);
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            INamespaceSymbol symbol,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            // Namespaces might be referenced through global aliases, which themselves are then referenced elsewhere.
            var allMatchingGlobalAliasNames = await GetAllMatchingGlobalAliasNamesAsync(project, symbol.Name, arity: 0, cancellationToken).ConfigureAwait(false);
            foreach (var globalAliasName in allMatchingGlobalAliasNames)
                result.AddRange(await FindDocumentsAsync(project, documents, cancellationToken, globalAliasName).ConfigureAwait(false));

            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, GetNamespaceIdentifierName(symbol)).ConfigureAwait(false);
            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalAttributesAsync(project, documents, cancellationToken).ConfigureAwait(false);
            return documentsWithGlobalAttributes.Concat(documentsWithName);
        }

        private static string GetNamespaceIdentifierName(INamespaceSymbol symbol)
        {
            return symbol.IsGlobalNamespace
                ? symbol.ToDisplayString(s_globalNamespaceFormat)
                : symbol.Name;
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            INamespaceSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var identifierName = GetNamespaceIdentifierName(symbol);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var tokens = await GetIdentifierOrGlobalNamespaceTokensWithTextAsync(
                document, semanticModel, identifierName, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var initialReferences);
            initialReferences.AddRange(await FindReferencesInTokensAsync(
                symbol,
                document,
                semanticModel,
                tokens,
                t => syntaxFacts.TextMatch(t.ValueText, identifierName),
                cancellationToken).ConfigureAwait(false));

            var allMatchingGlobalAliasNames = await GetAllMatchingGlobalAliasNamesAsync(document.Project, symbol.Name, arity: 0, cancellationToken).ConfigureAwait(false);
            foreach (var globalAliasName in allMatchingGlobalAliasNames)
            {
                tokens = await GetIdentifierOrGlobalNamespaceTokensWithTextAsync(
                    document, semanticModel, identifierName, cancellationToken).ConfigureAwait(false);

                initialReferences.AddRange(await FindReferencesInTokensAsync(
                    symbol,
                    document,
                    semanticModel,
                    tokens,
                    t => syntaxFacts.TextMatch(t.ValueText, identifierName),
                    cancellationToken).ConfigureAwait(false));
            }

            var suppressionReferences = await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
                document, semanticModel, symbol, cancellationToken).ConfigureAwait(false);

            var aliasReferences = await FindLocalAliasReferencesAsync(
                initialReferences, symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            initialReferences.AddRange(suppressionReferences);
            initialReferences.AddRange(aliasReferences);

            return initialReferences.ToImmutable();
        }
    }
}
