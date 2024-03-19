// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

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
            : await FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsGlobalKeyword, cancellationToken).ConfigureAwait(false));

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
        FindReferencesDocumentState state,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var initialReferences);

        if (symbol.IsGlobalNamespace)
        {
            await AddGlobalNamespaceReferencesAsync(
                symbol, state, initialReferences, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var namespaceName = symbol.Name;
            await AddNamedReferencesAsync(
                symbol, namespaceName, state, initialReferences, cancellationToken).ConfigureAwait(false);

            foreach (var globalAlias in state.GlobalAliases)
            {
                // ignore the cases where the global alias might match the namespace name (i.e.
                // global alias Collections = System.Collections).  We'll already find those references
                // above.
                if (state.SyntaxFacts.StringComparer.Equals(namespaceName, globalAlias))
                    continue;

                await AddNamedReferencesAsync(
                    symbol, globalAlias, state, initialReferences, cancellationToken).ConfigureAwait(false);
            }

            initialReferences.AddRange(await FindLocalAliasReferencesAsync(
                initialReferences, symbol, state, cancellationToken).ConfigureAwait(false));

            initialReferences.AddRange(await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
                symbol, state, cancellationToken).ConfigureAwait(false));
        }

        return initialReferences.ToImmutable();
    }

    /// <summary>
    /// Finds references to <paramref name="symbol"/> in this <paramref name="state"/>, but only if it referenced
    /// though <paramref name="name"/> (which might be the actual name of the type, or a global alias to it).
    /// </summary>
    private static async ValueTask AddNamedReferencesAsync(
        INamespaceSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        ArrayBuilder<FinderLocation> initialReferences,
        CancellationToken cancellationToken)
    {
        var tokens = await FindMatchingIdentifierTokensAsync(
            state, name, cancellationToken).ConfigureAwait(false);

        initialReferences.AddRange(await FindReferencesInTokensAsync(
            symbol, state, tokens, cancellationToken).ConfigureAwait(false));
    }

    private static async Task AddGlobalNamespaceReferencesAsync(
        INamespaceSymbol symbol,
        FindReferencesDocumentState state,
        ArrayBuilder<FinderLocation> initialReferences,
        CancellationToken cancellationToken)
    {
        var tokens = state.Root
            .DescendantTokens()
            .WhereAsArray(
                static (token, state) => state.SyntaxFacts.IsGlobalNamespaceKeyword(token),
                state);

        initialReferences.AddRange(await FindReferencesInTokensAsync(
            symbol, state, tokens, cancellationToken).ConfigureAwait(false));
    }
}
