// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class OperatorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
{
    protected override bool CanFind(IMethodSymbol symbol)
        => symbol.MethodKind is MethodKind.UserDefinedOperator or MethodKind.BuiltinOperator;

    protected sealed override async Task DetermineDocumentsToSearchAsync<TData>(
        IMethodSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var op = symbol.GetPredefinedOperator();
        await FindDocumentsAsync(project, documents, op, processResult, processResultData, cancellationToken).ConfigureAwait(false);
        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    private static Task FindDocumentsAsync<TData>(
        Project project,
        IImmutableSet<Document>? documents,
        PredefinedOperator op,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        if (op == PredefinedOperator.None)
            return Task.CompletedTask;

        return FindDocumentsWithPredicateAsync(
            project, documents, static (index, op) => index.ContainsPredefinedOperator(op), op, processResult, processResultData, cancellationToken);
    }

    protected sealed override void FindReferencesInDocument<TData>(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var op = symbol.GetPredefinedOperator();
        var tokens = state.Root
            .DescendantTokens(descendIntoTrivia: true)
            .WhereAsArray(
                static (token, tuple) => IsPotentialReference(tuple.state.SyntaxFacts, tuple.op, token),
                (state, op));

        FindReferencesInTokens(
            symbol, state, tokens, processResult, processResultData, cancellationToken);
        FindReferencesInDocumentInsideGlobalSuppressions(
            symbol, state, processResult, processResultData, cancellationToken);
    }

    private static bool IsPotentialReference(
        ISyntaxFactsService syntaxFacts,
        PredefinedOperator op,
        SyntaxToken token)
    {
        return syntaxFacts.TryGetPredefinedOperator(token, out var actualOperator) && actualOperator == op;
    }
}
