// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
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
            if (symbol.PartialDefinitionPart != null)
                return ImmutableArray.Create<ISymbol>(symbol.PartialDefinitionPart);

            if (symbol.PartialImplementationPart != null)
                return ImmutableArray.Create<ISymbol>(symbol.PartialImplementationPart);

            return ImmutableArray<ISymbol>.Empty;
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol methodSymbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
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

            var ordinaryDocuments = await FindDocumentsAsync(project, documents, cancellationToken, methodSymbol.Name).ConfigureAwait(false);
            var forEachDocuments = IsForEachMethod(methodSymbol)
                ? await FindDocumentsWithForEachStatementsAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            var deconstructDocuments = IsDeconstructMethod(methodSymbol)
                ? await FindDocumentsWithDeconstructionAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            var awaitExpressionDocuments = IsGetAwaiterMethod(methodSymbol)
                ? await FindDocumentsWithAwaitExpressionAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalSuppressMessageAttributeAsync(
                project, documents, cancellationToken).ConfigureAwait(false);

            var documentsWithCollectionInitializers = IsAddMethod(methodSymbol)
                ? await FindDocumentsWithCollectionInitializersAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            return ordinaryDocuments.Concat(
                forEachDocuments, deconstructDocuments, awaitExpressionDocuments, documentsWithGlobalAttributes, documentsWithCollectionInitializers);
        }

        private static Task<ImmutableArray<Document>> FindDocumentsWithDeconstructionAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsDeconstruction, cancellationToken);

        private static Task<ImmutableArray<Document>> FindDocumentsWithAwaitExpressionAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsAwait, cancellationToken);

        private static Task<ImmutableArray<Document>> FindDocumentsWithCollectionInitializersAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsCollectionInitializer, cancellationToken);

        private static bool IsForEachMethod(IMethodSymbol methodSymbol)
            => methodSymbol.Name is WellKnownMemberNames.GetEnumeratorMethodName or
                                    WellKnownMemberNames.MoveNextMethodName;

        private static bool IsDeconstructMethod(IMethodSymbol methodSymbol)
            => methodSymbol.Name == WellKnownMemberNames.DeconstructMethodName;

        private static bool IsGetAwaiterMethod(IMethodSymbol methodSymbol)
            => methodSymbol.Name == WellKnownMemberNames.GetAwaiter;

        private static bool IsAddMethod(IMethodSymbol methodSymbol)
            => methodSymbol.Name == WellKnownMemberNames.CollectionInitializerAddMethodName;

        protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            FindReferencesDocumentState state,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var nameMatches = await FindReferencesInDocumentUsingSymbolNameAsync(
                symbol, state, cancellationToken).ConfigureAwait(false);

            var forEachMatches = IsForEachMethod(symbol)
                ? await FindReferencesInForEachStatementsAsync(symbol, state, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            var deconstructMatches = IsDeconstructMethod(symbol)
                ? await FindReferencesInDeconstructionAsync(symbol, state, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            var getAwaiterMatches = IsGetAwaiterMethod(symbol)
                ? await FindReferencesInAwaitExpressionAsync(symbol, state, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            var suppressionReferences = await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
                symbol, state, cancellationToken).ConfigureAwait(false);

            var addMatches = IsAddMethod(symbol)
                ? await FindReferencesInCollectionInitializerAsync(symbol, state, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            return nameMatches.Concat(forEachMatches, deconstructMatches, getAwaiterMatches, suppressionReferences, addMatches);
        }
    }
}
