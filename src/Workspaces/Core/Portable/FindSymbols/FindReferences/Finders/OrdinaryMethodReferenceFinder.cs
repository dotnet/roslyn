// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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

        protected override async Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsAsync(
            IMethodSymbol symbol,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            // If it's a delegate method, then cascade to the type as well.  These guys are
            // practically equivalent for users.
            if (symbol.ContainingType.TypeKind == TypeKind.Delegate)
            {
                return SpecializedCollections.SingletonEnumerable((ISymbol)symbol.ContainingType);
            }
            else
            {
                var otherPartsOfPartial = GetOtherPartsOfPartial(symbol);
                var baseCascadedSymbols = await base.DetermineCascadedSymbolsAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);

                if (otherPartsOfPartial == null && baseCascadedSymbols == null)
                {
                    return null;
                }

                otherPartsOfPartial = otherPartsOfPartial ?? SpecializedCollections.EmptyEnumerable<ISymbol>();
                baseCascadedSymbols = baseCascadedSymbols ?? SpecializedCollections.EmptyEnumerable<ISymbol>();

                return otherPartsOfPartial.Concat(baseCascadedSymbols);
            }
        }

        private IEnumerable<ISymbol> GetOtherPartsOfPartial(IMethodSymbol symbol)
        {
            if (symbol.PartialDefinitionPart != null)
            {
                return SpecializedCollections.SingletonEnumerable(symbol.PartialDefinitionPart);
            }

            if (symbol.PartialImplementationPart != null)
            {
                return SpecializedCollections.SingletonEnumerable(symbol.PartialImplementationPart);
            }

            return null;
        }

        protected override async Task<IEnumerable<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol methodSymbol,
            Project project,
            IImmutableSet<Document> documents,
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
            // searches for these, then we should find usages of 'lock(foo)' or 'synclock(foo)'
            // since they implicitly call those methods.

            var ordinaryDocuments = await FindDocumentsAsync(project, documents, cancellationToken, methodSymbol.Name).ConfigureAwait(false);
            var forEachDocuments = IsForEachMethod(methodSymbol)
                ? await FindDocumentsWithForEachStatementsAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<Document>();

            return ordinaryDocuments.Concat(forEachDocuments);
        }

        private bool IsForEachMethod(IMethodSymbol methodSymbol)
        {
            return
                methodSymbol.Name == WellKnownMemberNames.GetEnumeratorMethodName ||
                methodSymbol.Name == WellKnownMemberNames.MoveNextMethodName;
        }

        protected override async Task<IEnumerable<ReferenceLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var nameMatches = await FindReferencesInDocumentUsingSymbolNameAsync(
                symbol,
                document,
                cancellationToken).ConfigureAwait(false);

            var forEachMatches = IsForEachMethod(symbol)
                ? await FindReferencesInForEachStatementsAsync(symbol, document, cancellationToken).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<ReferenceLocation>();

            return nameMatches.Concat(forEachMatches);
        }
    }
}
