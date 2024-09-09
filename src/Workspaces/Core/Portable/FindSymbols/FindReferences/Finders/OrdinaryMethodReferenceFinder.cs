﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class OrdinaryMethodReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
{
    protected override bool CanFind(IMethodSymbol symbol)
        => symbol.MethodKind is MethodKind.Ordinary or
                                MethodKind.DelegateInvoke or
                                MethodKind.DeclareMethod or
                                MethodKind.ReducedExtension or
                                MethodKind.LocalFunction;

    protected override ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        IMethodSymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // If it's a delegate method, then cascade to the type as well.  These guys are
        // practically equivalent for users.
        return symbol.ContainingType.TypeKind == TypeKind.Delegate
            ? new(ImmutableArray.Create<ISymbol>(symbol.ContainingType))
            : new(GetOtherPartsOfPartial(symbol));
    }

    private static ImmutableArray<ISymbol> GetOtherPartsOfPartial(IMethodSymbol symbol)
    {
        // https://github.com/dotnet/roslyn/issues/73772: define/use a similar helper for PropertySymbolReferenceFinder+PropertyAccessorSymbolReferenceFinder?
        if (symbol.PartialDefinitionPart != null)
            return [symbol.PartialDefinitionPart];

        if (symbol.PartialImplementationPart != null)
            return [symbol.PartialImplementationPart];

        return [];
    }

    protected override async Task DetermineDocumentsToSearchAsync<TData>(
        IMethodSymbol methodSymbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // TODO(cyrusn): Handle searching for IDisposable.Dispose (or an implementation
        // thereof).  in that case, we need to look at documents that have a using in them
        // and see if that using binds to this dispose method.  We also need to look at
        // 'foreach's as the will call 'Dispose' afterwards.

        // TODO(cyrusn): Handle searching for linq methods.  If the user searches for 'Cast',
        // 'Where', 'Select', 'SelectMany', 'Join', 'GroupJoin', 'OrderBy',
        // 'OrderByDescending', 'GroupBy', 'ThenBy' or 'ThenByDescending', then we want to
        // search in files that have query expressions and see if any query clause binds to
        // these methods.

        // TODO(cyrusn): Handle searching for Monitor.Enter and Monitor.Exit.  If a user
        // searches for these, then we should find usages of 'lock(goo)' or 'synclock(goo)'
        // since they implicitly call those methods.

        await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, methodSymbol.Name).ConfigureAwait(false);

        if (IsForEachMethod(methodSymbol))
            await FindDocumentsWithForEachStatementsAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        if (IsDeconstructMethod(methodSymbol))
            await FindDocumentsWithDeconstructionAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        if (IsGetAwaiterMethod(methodSymbol))
            await FindDocumentsWithAwaitExpressionAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(
            project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        if (IsAddMethod(methodSymbol))
            await FindDocumentsWithCollectionInitializersAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    private static Task FindDocumentsWithDeconstructionAsync<TData>(Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsDeconstruction, processResult, processResultData, cancellationToken);

    private static Task FindDocumentsWithAwaitExpressionAsync<TData>(Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsAwait, processResult, processResultData, cancellationToken);

    private static Task FindDocumentsWithCollectionInitializersAsync<TData>(Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsCollectionInitializer, processResult, processResultData, cancellationToken);

    private static bool IsForEachMethod(IMethodSymbol methodSymbol)
        => methodSymbol.Name is WellKnownMemberNames.GetEnumeratorMethodName or
                                WellKnownMemberNames.MoveNextMethodName;

    private static bool IsDeconstructMethod(IMethodSymbol methodSymbol)
        => methodSymbol.Name == WellKnownMemberNames.DeconstructMethodName;

    private static bool IsGetAwaiterMethod(IMethodSymbol methodSymbol)
        => methodSymbol.Name == WellKnownMemberNames.GetAwaiter;

    private static bool IsAddMethod(IMethodSymbol methodSymbol)
        => methodSymbol.Name == WellKnownMemberNames.CollectionInitializerAddMethodName;

    protected sealed override void FindReferencesInDocument<TData>(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocumentUsingSymbolName(
            symbol, state, processResult, processResultData, cancellationToken);

        if (IsForEachMethod(symbol))
            FindReferencesInForEachStatements(symbol, state, processResult, processResultData, cancellationToken);

        if (IsDeconstructMethod(symbol))
            FindReferencesInDeconstruction(symbol, state, processResult, processResultData, cancellationToken);

        if (IsGetAwaiterMethod(symbol))
            FindReferencesInAwaitExpression(symbol, state, processResult, processResultData, cancellationToken);

        FindReferencesInDocumentInsideGlobalSuppressions(
            symbol, state, processResult, processResultData, cancellationToken);

        if (IsAddMethod(symbol))
            FindReferencesInCollectionInitializer(symbol, state, processResult, processResultData, cancellationToken);
    }
}
