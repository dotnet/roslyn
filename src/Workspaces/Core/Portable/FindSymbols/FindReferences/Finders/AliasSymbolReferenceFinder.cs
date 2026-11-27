// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class AliasSymbolReferenceFinder : AbstractReferenceFinder<IAliasSymbol>
{
    public static readonly AliasSymbolReferenceFinder Instance = new();

    private AliasSymbolReferenceFinder()
    {
    }

    protected override bool CanFind(IAliasSymbol symbol)
        => true;

    protected override async Task DetermineDocumentsToSearchAsync<TData>(
        IAliasSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        if (symbol.DeclaringSyntaxReferences is [var reference])
        {
            var document = project.Solution.GetDocument(reference.SyntaxTree);
            if (document?.Project == project)
                processResult(document, processResultData);
        }
    }

    protected override void FindReferencesInDocument<TData>(
        IAliasSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var tokens = FindMatchingIdentifierTokens(state, symbol.Name, cancellationToken);
        foreach (var token in tokens)
        {
            var parent = state.SyntaxFacts.TryGetBindableParent(token);
            if (parent == null)
                continue;

            var aliasInfo = state.SemanticModel.GetAliasInfo(parent, cancellationToken);
            if (Equals(aliasInfo, symbol))
            {
                var finderLocation = CreateFinderLocation(state, token, CandidateReason.None, cancellationToken);
                processResult(finderLocation, processResultData);
            }
        }
    }
}
