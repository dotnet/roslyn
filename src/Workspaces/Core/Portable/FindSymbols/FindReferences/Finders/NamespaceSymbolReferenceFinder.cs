// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class NamespaceSymbolReferenceFinder : AbstractReferenceFinder<INamespaceSymbol>
    {
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

            result.AddRange(!symbol.IsGlobalNamespace
                ? await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false)
                : await FindDocumentsAsync(project, documents, async (d, c) =>
                {
                    var index = await d.GetSyntaxTreeIndexAsync(c).ConfigureAwait(false);
                    return index.ContainsGlobalKeyword;
                }, cancellationToken).ConfigureAwait(false));

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                {
                    result.AddRange(await FindDocumentsAsync(
                        project, documents, cancellationToken, globalAlias).ConfigureAwait(false));
                }
            }

            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, cancellationToken).ConfigureAwait(false);
            result.AddRange(documentsWithGlobalAttributes);

            return result.ToImmutable();
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
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var initialReferences);

            if (symbol.IsGlobalNamespace)
            {
                await AddGlobalNamespaceReferencesAsync(
                    symbol, document, semanticModel,
                    initialReferences, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var namespaceName = symbol.Name;
                await AddNamedReferencesAsync(
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

                        await AddNamedReferencesAsync(
                            symbol, globalAlias, document, semanticModel,
                            initialReferences, cancellationToken).ConfigureAwait(false);
                    }
                }

                initialReferences.AddRange(await FindLocalAliasReferencesAsync(
                    initialReferences, symbol, document, semanticModel, cancellationToken).ConfigureAwait(false));

                initialReferences.AddRange(await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
                    document, semanticModel, symbol, cancellationToken).ConfigureAwait(false));
            }

            return initialReferences.ToImmutable();
        }

        /// <summary>
        /// Finds references to <paramref name="symbol"/> in this <paramref name="document"/>, but
        /// only if it referenced though <paramref name="name"/> (which might be the actual name
        /// of the type, or a global alias to it).
        /// </summary>
        private static async Task AddNamedReferencesAsync(
            INamespaceSymbol symbol,
            string name,
            Document document,
            SemanticModel semanticModel,
            ArrayBuilder<FinderLocation> initialReferences,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var tokens = await GetIdentifierTokensWithTextAsync(
                document, semanticModel, name, cancellationToken).ConfigureAwait(false);

            initialReferences.AddRange(await FindReferencesInTokensAsync(
                symbol,
                document,
                semanticModel,
                tokens,
                t =>
                {
                    Debug.Assert(syntaxFacts.TextMatch(t.ValueText, name));
                    return true;
                },
                cancellationToken).ConfigureAwait(false));
        }

        private static async Task AddGlobalNamespaceReferencesAsync(
            INamespaceSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            ArrayBuilder<FinderLocation> initialReferences,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var tokens = root.DescendantTokens().Where(syntaxFacts.IsGlobalNamespaceKeyword);

            initialReferences.AddRange(await FindReferencesInTokensAsync(
                symbol,
                document,
                semanticModel,
                tokens,
                t =>
                {
                    Debug.Assert(syntaxFacts.IsGlobalNamespaceKeyword(t));
                    return true;
                },
                cancellationToken).ConfigureAwait(false));
        }
    }
}
