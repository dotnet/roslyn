// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class PropertyAccessorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
{
    protected override bool CanFind(IMethodSymbol symbol)
        => symbol.MethodKind.IsPropertyAccessor();

    protected override ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        IMethodSymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // If we've been asked to search for specific accessors, then do not cascade.
        // We don't want to produce results for the associated property.
        return options.AssociatePropertyReferencesWithSpecificAccessor || symbol.AssociatedSymbol == null
            ? new(ImmutableArray<ISymbol>.Empty)
            : new(ImmutableArray.Create(symbol.AssociatedSymbol));
    }

    protected override async Task DetermineDocumentsToSearchAsync<TData>(
        IMethodSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // First, find any documents with the full name of the accessor (i.e. get_Goo).
        // This will find explicit calls to the method (which can happen when C# references
        // a VB parameterized property).
        await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, symbol.Name).ConfigureAwait(false);

        if (symbol.AssociatedSymbol is IPropertySymbol property &&
            options.AssociatePropertyReferencesWithSpecificAccessor)
        {
            // we want to associate normal property references with the specific accessor being
            // referenced.  So we also need to include documents with our property's name. Just
            // defer to the Property finder to find these docs and combine them with the result.
            await PropertySymbolReferenceFinder.Instance.DetermineDocumentsToSearchAsync(
                property, globalAliases, project, documents,
                processResult, processResultData,
                options with { AssociatePropertyReferencesWithSpecificAccessor = false },
                cancellationToken).ConfigureAwait(false);
        }

        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    protected override void FindReferencesInDocument<TData>(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocumentUsingSymbolName(
            symbol, state, processResult, processResultData, cancellationToken);

        if (symbol.AssociatedSymbol is not IPropertySymbol property ||
            !options.AssociatePropertyReferencesWithSpecificAccessor)
        {
            return;
        }

        PropertySymbolReferenceFinder.Instance.FindReferencesInDocument(
            property,
            state,
            static (loc, data) =>
            {
                var accessors = GetReferencedAccessorSymbols(
                    data.state, data.property, loc.Node, data.cancellationToken);
                if (accessors.Contains(data.symbol))
                    data.processResult(loc, data.processResultData);
            },
            (property, symbol, state, processResult, processResultData, cancellationToken),
            options with { AssociatePropertyReferencesWithSpecificAccessor = false },
            cancellationToken);
    }
}
