// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

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
        if (symbol.ContainingType.TypeKind == TypeKind.Delegate)
            return new([symbol.ContainingType]);

        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);

        result.AddRange(GetOtherPartsOfPartial(symbol));
        CascadeToExtensionImplementation(symbol, result);
        CascadeFromExtensionImplementation(symbol, result);

        return new(result.ToImmutableAndClear());
    }

    private static void CascadeToExtensionImplementation(IMethodSymbol symbol, ArrayBuilder<ISymbol> result)
    {
        // If the given symbol is an extension member, cascade to its implementation method
        if (symbol.AssociatedExtensionImplementation is { } associatedExtensionImplementation)
            result.Add(associatedExtensionImplementation);
    }

    private static void CascadeFromExtensionImplementation(IMethodSymbol symbol, ArrayBuilder<ISymbol> result)
    {
        // If the given symbol is an implementation method of an extension member, cascade to the extension member itself
        var containingType = symbol.ContainingType;
        if (containingType is null || !containingType.MightContainExtensionMethods || !symbol.IsStatic)
            return;

        var implementationDefinition = symbol.OriginalDefinition;
        foreach (var nestedType in containingType.GetTypeMembers())
        {
            if (!nestedType.IsExtension || nestedType.ExtensionParameter is null)
                continue;

            foreach (var member in nestedType.GetMembers())
            {
                if (member is IMethodSymbol method)
                {
                    var associated = method.AssociatedExtensionImplementation;
                    if (associated is null)
                        continue;

                    if (!object.ReferenceEquals(associated.OriginalDefinition, implementationDefinition))
                        continue;

                    result.Add(method);
                }
            }
        }
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

        if (IsDisposeMethod(methodSymbol))
            await FindDocumentsWithUsingStatementsAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
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

    private static bool IsDisposeMethod(IMethodSymbol methodSymbol)
        => methodSymbol.Name == nameof(IDisposable.Dispose);

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

        if (IsDisposeMethod(symbol))
            FindReferencesInUsingStatements(symbol, state, processResult, processResultData, cancellationToken);
    }

    private void FindReferencesInUsingStatements<TData>(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocument(state, static index => index.ContainsUsingStatement, CollectMatchingReferences, processResult, processResultData, cancellationToken);
        return;

        void CollectMatchingReferences(
            SyntaxNode node,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData)
        {
            var disposeMethod = state.SemanticFacts.TryGetDisposeMethod(state.SemanticModel, node, cancellationToken);

            if (Matches(disposeMethod, symbol))
            {
                var location = node.GetFirstToken().GetLocation();
                var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                var result = new FinderLocation(node, new ReferenceLocation(
                    state.Document,
                    alias: null,
                    location: location,
                    isImplicit: true,
                    symbolUsageInfo,
                    GetAdditionalFindUsagesProperties(node, state),
                    candidateReason: CandidateReason.None));
                processResult(result, processResultData);
            }
        }
    }
}
