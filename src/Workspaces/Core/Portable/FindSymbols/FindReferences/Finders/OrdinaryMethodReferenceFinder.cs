// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class OrdinaryMethodReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
        {
            return
                symbol.MethodKind == MethodKind.Ordinary ||
                symbol.MethodKind == MethodKind.DelegateInvoke ||
                symbol.MethodKind == MethodKind.DeclareMethod ||
                symbol.MethodKind == MethodKind.ReducedExtension ||
                symbol.MethodKind == MethodKind.LocalFunction;
        }

        protected override async Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            IMethodSymbol symbol,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // If it's a delegate method, then cascade to the type as well.  These guys are
            // practically equivalent for users.
            if (symbol.ContainingType.TypeKind == TypeKind.Delegate)
            {
                return ImmutableArray.Create<ISymbol>(symbol.ContainingType);
            }
            else
            {
                var otherPartsOfPartial = GetOtherPartsOfPartial(symbol);
                var baseCascadedSymbols = await base.DetermineCascadedSymbolsAsync(
                    symbol, solution, projects, options, cancellationToken).ConfigureAwait(false);

                if (otherPartsOfPartial == null && baseCascadedSymbols == null)
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                return otherPartsOfPartial.Concat(baseCascadedSymbols);
            }
        }

        private static ImmutableArray<ISymbol> GetOtherPartsOfPartial(IMethodSymbol symbol)
        {
            if (symbol.PartialDefinitionPart != null)
            {
                return ImmutableArray.Create<ISymbol>(symbol.PartialDefinitionPart);
            }

            if (symbol.PartialImplementationPart != null)
            {
                return ImmutableArray.Create<ISymbol>(symbol.PartialImplementationPart);
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol methodSymbol,
            Project project,
            IImmutableSet<Document> documents,
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

            var ordinaryDocuments = await FindDocumentsAsync(project, documents, findInGlobalSuppressions: true, cancellationToken, methodSymbol.Name).ConfigureAwait(false);
            var forEachDocuments = IsForEachMethod(methodSymbol)
                ? await FindDocumentsWithForEachStatementsAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            var deconstructDocuments = IsDeconstructMethod(methodSymbol)
                ? await FindDocumentsWithDeconstructionAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            var awaitExpressionDocuments = IsGetAwaiterMethod(methodSymbol)
                ? await FindDocumentsWithAwaitExpressionAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            return ordinaryDocuments.Concat(forEachDocuments).Concat(deconstructDocuments).Concat(awaitExpressionDocuments);
        }

        private static bool IsForEachMethod(IMethodSymbol methodSymbol)
        {
            return
                methodSymbol.Name == WellKnownMemberNames.GetEnumeratorMethodName ||
                methodSymbol.Name == WellKnownMemberNames.MoveNextMethodName;
        }

        private static bool IsDeconstructMethod(IMethodSymbol methodSymbol)
            => methodSymbol.Name == WellKnownMemberNames.DeconstructMethodName;

        private static bool IsGetAwaiterMethod(IMethodSymbol methodSymbol)
            => methodSymbol.Name == WellKnownMemberNames.GetAwaiter;

        protected override async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var nameMatches = await FindReferencesInDocumentUsingSymbolNameAsync(
                symbol,
                document,
                semanticModel,
                cancellationToken).ConfigureAwait(false);

            if (IsForEachMethod(symbol))
            {
                var forEachMatches = await FindReferencesInForEachStatementsAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
                nameMatches = nameMatches.Concat(forEachMatches);
            }

            if (IsDeconstructMethod(symbol))
            {
                var deconstructMatches = await FindReferencesInDeconstructionAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
                nameMatches = nameMatches.Concat(deconstructMatches);
            }

            if (IsGetAwaiterMethod(symbol))
            {
                var getAwaiterMatches = await FindReferencesInAwaitExpressionAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
                nameMatches = nameMatches.Concat(getAwaiterMatches);
            }

            return nameMatches;
        }
    }
}
