// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class PreprocessingSymbolReferenceFinder : AbstractReferenceFinder<IPreprocessingSymbol>
{
    protected override bool CanFind(IPreprocessingSymbol symbol)
        => true;

    protected override async Task DetermineDocumentsToSearchAsync<TData>(
        IPreprocessingSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        await FindDocumentsWithPredicateAsync(project, documents, DetermineDocument, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        bool DetermineDocument(SyntaxTreeIndex syntaxTreeIndex)
        {
            return syntaxTreeIndex.ContainsDirective && syntaxTreeIndex.ProbablyContainsIdentifier(symbol.Name);
        }
    }

    protected override void FindReferencesInDocument<TData>(
        IPreprocessingSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var tokens = FindMatchingIdentifierTokens(state, symbol.Name, cancellationToken);

        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetSymbol = state.SemanticModel.GetPreprocessingSymbolInfo(token.GetRequiredParent()).Symbol;
            var matched = SymbolFinder.OriginalSymbolsMatch(state.Solution, symbol, targetSymbol);

            if (matched)
            {
                var location = CreateFinderLocation(state, token, CandidateReason.None, cancellationToken);
                processResult(location, processResultData);
            }
        }
    }
}
