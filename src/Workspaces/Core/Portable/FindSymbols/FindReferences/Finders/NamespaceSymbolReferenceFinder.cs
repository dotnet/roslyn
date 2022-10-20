// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            var namespaceName = GetNamespaceIdentifierName(symbol);
            result.AddRange(await FindDocumentsAsync(
                project, documents, cancellationToken, namespaceName).ConfigureAwait(false));

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                {
                    result.AddRange(await FindDocumentsAsync(
                        project, documents, cancellationToken, globalAlias).ConfigureAwait(false));
                }
            }

            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalAttributesAsync(project, documents, cancellationToken).ConfigureAwait(false);
            result.AddRange(documentsWithGlobalAttributes);

            return result.ToImmutable();
        }

        private static string GetNamespaceIdentifierName(INamespaceSymbol symbol)
        {
            return symbol.IsGlobalNamespace
                ? symbol.ToDisplayString(s_globalNamespaceFormat)
                : symbol.Name;
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            INamespaceSymbol symbol,
            HashSet<string>? globalAliases,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var namespaceName = GetNamespaceIdentifierName(symbol);

            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var initialReferences);

            await AddReferencesAsync(
                symbol, namespaceName, document, semanticModel,
                initialReferences, cancellationToken).ConfigureAwait(false);

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                {
                    // ignore the cases where the global alias might match the namespace name (i.e.
                    // global alias Collections = System.Collections).  We'll already find those references
                    // above.
                    if (syntaxFacts.StringComparer.Equals(namespaceName, globalAlias))
                        continue;

                    await AddReferencesAsync(
                        symbol, globalAlias, document, semanticModel,
                        initialReferences, cancellationToken).ConfigureAwait(false);
                }
            }

            initialReferences.AddRange(await FindLocalAliasReferencesAsync(
                initialReferences, symbol, document, semanticModel, cancellationToken).ConfigureAwait(false));

            initialReferences.AddRange(await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
                document, semanticModel, symbol, cancellationToken).ConfigureAwait(false));

            return initialReferences.ToImmutable();
        }

        /// <summary>
        /// Finds references to <paramref name="symbol"/> in this <paramref name="document"/>, but
        /// only if it referenced though <paramref name="name"/> (which might be the actual name
        /// of the type, or a global alias to it).
        /// </summary>
        private static async Task AddReferencesAsync(
            INamespaceSymbol symbol,
            string name,
            Document document,
            SemanticModel semanticModel,
            ArrayBuilder<FinderLocation> initialReferences,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var tokens = await GetIdentifierOrGlobalNamespaceTokensWithTextAsync(
                document, semanticModel, name, cancellationToken).ConfigureAwait(false);

            initialReferences.AddRange(await FindReferencesInTokensAsync(
                symbol,
                document,
                semanticModel,
                tokens,
                t => syntaxFacts.TextMatch(t.ValueText, name),
                cancellationToken).ConfigureAwait(false));
        }
    }
}
