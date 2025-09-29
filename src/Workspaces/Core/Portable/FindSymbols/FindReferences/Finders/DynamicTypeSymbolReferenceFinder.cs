// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class DynamicTypeSymbolReferenceFinder : AbstractReferenceFinder<IDynamicTypeSymbol>
{
    private const string DynamicIdentifier = "dynamic";
    public static readonly DynamicTypeSymbolReferenceFinder Instance = new();

    private DynamicTypeSymbolReferenceFinder()
    {
    }

    protected override bool CanFind(IDynamicTypeSymbol symbol)
        => true;

    protected override Task DetermineDocumentsToSearchAsync<TData>(
        IDynamicTypeSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // For now, we're just looking for 'dynamic' itself, not an aliases to it.
        return FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, DynamicIdentifier);
    }

    protected override void FindReferencesInDocument<TData>(
        IDynamicTypeSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocumentUsingIdentifier(symbol, DynamicIdentifier, state, processResult, processResultData, cancellationToken);
    }
}
