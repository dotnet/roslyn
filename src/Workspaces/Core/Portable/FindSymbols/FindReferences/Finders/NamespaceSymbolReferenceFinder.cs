// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class NamespaceSymbolReferenceFinder : AbstractReferenceFinder<INamespaceSymbol>
{
    protected override bool CanFind(INamespaceSymbol symbol)
        => true;

    protected override Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(INamespaceSymbol symbol, Project project, CancellationToken cancellationToken)
    {
        return GetAllMatchingGlobalAliasNamesAsync(project, symbol.Name, arity: 0, cancellationToken);
    }

    protected override async Task DetermineDocumentsToSearchAsync<TData>(
        INamespaceSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        if (!symbol.IsGlobalNamespace)
            await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, symbol.Name).ConfigureAwait(false);
        else
            await FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsGlobalKeyword, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        if (globalAliases != null)
        {
            foreach (var globalAlias in globalAliases)
            {
                await FindDocumentsAsync(
                    project, documents, processResult, processResultData, cancellationToken, globalAlias).ConfigureAwait(false);
            }
        }

        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    protected override void FindReferencesInDocument<TData>(
        INamespaceSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        if (symbol.IsGlobalNamespace)
        {
            AddGlobalNamespaceReferences(
                symbol, state, processResult, processResultData, cancellationToken);
        }
        else
        {
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var initialReferences);

            var namespaceName = symbol.Name;
            AddNamedReferences(
                symbol, namespaceName, state, StandardCallbacks<FinderLocation>.AddToArrayBuilder, initialReferences, cancellationToken);

            foreach (var globalAlias in state.GlobalAliases)
            {
                // ignore the cases where the global alias might match the namespace name (i.e.
                // global alias Collections = System.Collections).  We'll already find those references
                // above.
                if (state.SyntaxFacts.StringComparer.Equals(namespaceName, globalAlias))
                    continue;

                AddNamedReferences(
                    symbol, globalAlias, state, StandardCallbacks<FinderLocation>.AddToArrayBuilder, initialReferences, cancellationToken);
            }

            // The items in initialReferences need to be both reported and used later to calculate additional results.
            foreach (var location in initialReferences)
                processResult(location, processResultData);

            FindLocalAliasReferences(
                initialReferences, symbol, state, processResult, processResultData, cancellationToken);

            FindReferencesInDocumentInsideGlobalSuppressions(
                symbol, state, processResult, processResultData, cancellationToken);
        }
    }

    /// <summary>
    /// Finds references to <paramref name="symbol"/> in this <paramref name="state"/>, but only if it referenced
    /// though <paramref name="name"/> (which might be the actual name of the type, or a global alias to it).
    /// </summary>
    private static void AddNamedReferences<TData>(
        INamespaceSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var tokens = FindMatchingIdentifierTokens(state, name, cancellationToken);

        FindReferencesInTokens(
            symbol, state, tokens, processResult, processResultData, cancellationToken);
    }

    private static void AddGlobalNamespaceReferences<TData>(
        INamespaceSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var tokens = state.Root
            .DescendantTokens()
            .WhereAsArray(
                static (token, state) => state.SyntaxFacts.IsGlobalNamespaceKeyword(token),
                state);

        FindReferencesInTokens(
            symbol, state, tokens, processResult, processResultData, cancellationToken);
    }
}
