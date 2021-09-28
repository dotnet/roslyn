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
        {
            return
                symbol.MethodKind is MethodKind.Ordinary or
                MethodKind.DelegateInvoke or
                MethodKind.DeclareMethod or
                MethodKind.ReducedExtension or
                MethodKind.LocalFunction;
        }

        protected override Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            IMethodSymbol symbol,
            Solution solution,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // If it's a delegate method, then cascade to the type as well.  These guys are
            // practically equivalent for users.
            if (symbol.ContainingType.TypeKind == TypeKind.Delegate)
            {
                return Task.FromResult(ImmutableArray.Create<ISymbol>(symbol.ContainingType));
            }
            else
            {
                return Task.FromResult(GetOtherPartsOfPartial(symbol));
            }
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

            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalAttributesAsync(project, documents, cancellationToken).ConfigureAwait(false);
            return ordinaryDocuments.Concat(forEachDocuments, deconstructDocuments, awaitExpressionDocuments, documentsWithGlobalAttributes);
        }

        private static bool IsForEachMethod(IMethodSymbol methodSymbol)
        {
            return
                methodSymbol.Name is WellKnownMemberNames.GetEnumeratorMethodName or
                WellKnownMemberNames.MoveNextMethodName;
        }

        private static bool IsDeconstructMethod(IMethodSymbol methodSymbol)
            => methodSymbol.Name == WellKnownMemberNames.DeconstructMethodName;

        private static bool IsGetAwaiterMethod(IMethodSymbol methodSymbol)
            => methodSymbol.Name == WellKnownMemberNames.GetAwaiter;

        protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            HashSet<string>? globalAliases,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var nameMatches = await FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            var forEachMatches = IsForEachMethod(symbol)
                ? await FindReferencesInForEachStatementsAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            var deconstructMatches = IsDeconstructMethod(symbol)
                ? await FindReferencesInDeconstructionAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            var getAwaiterMatches = IsGetAwaiterMethod(symbol)
                ? await FindReferencesInAwaitExpressionAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            var suppressionReferences = await FindReferencesInDocumentInsideGlobalSuppressionsAsync(document, semanticModel, symbol, cancellationToken).ConfigureAwait(false);
            return nameMatches.Concat(forEachMatches, deconstructMatches, getAwaiterMatches, suppressionReferences);
        }
    }
}
