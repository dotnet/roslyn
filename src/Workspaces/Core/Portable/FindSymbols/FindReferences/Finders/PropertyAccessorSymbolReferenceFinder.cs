// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class PropertyAccessorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
        {
            return symbol.MethodKind.IsPropertyAccessor();
        }

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IMethodSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            var result = await base.DetermineCascadedSymbolsAsync(
                symbolAndProjectId, solution, projects, cancellationToken).ConfigureAwait(false);

            var symbol = symbolAndProjectId.Symbol;
            if (symbol.AssociatedSymbol != null)
            {
                //result = result.Add(symbolAndProjectId.WithSymbol(symbol.AssociatedSymbol));
            }

            return result;
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(IMethodSymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            var result = await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);
            result = result.Concat(await FindDocumentsAsync(project, documents, cancellationToken, symbol.AssociatedSymbol.Name).ConfigureAwait(false));

            return result;
        }

        protected override async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(IMethodSymbol symbol, Document document, CancellationToken cancellationToken)
        {
            var result = await FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, cancellationToken).ConfigureAwait(false);

            var propertySymbol = symbol.AssociatedSymbol;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var isSetter = symbol.MethodKind == MethodKind.PropertySet;

            bool tokensMatchAndAppropriate(SyntaxToken t)
            {
                if (!IdentifiersMatch(syntaxFacts, propertySymbol.Name, t))
                {
                    return false;
                }

                if (isSetter)
                {
                    return syntaxFacts.IsAssignedTo(t.Parent);
                }

                return true;
            }

            result = result.Concat(await FindReferencesForAccessorsAsync(propertySymbol, tokensMatchAndAppropriate, document, cancellationToken).ConfigureAwait(false));
            return result;
        }

        private async Task<ImmutableArray<ReferenceLocation>> FindReferencesForAccessorsAsync(
            ISymbol propertySymbol,
            Func<SyntaxToken, bool> tokensMatchAndAppropriate,
            Document document,
            CancellationToken cancellationToken)
        {
            var syntaxTreeInfo = await SyntaxTreeIndex.GetIndexAsync(document, cancellationToken).ConfigureAwait(false);
            string name = propertySymbol.Name;
            if (!syntaxTreeInfo.ProbablyContainsIdentifier(name))
            {
                return ImmutableArray<ReferenceLocation>.Empty;
            }

            var tokens = await document.GetIdentifierOrGlobalNamespaceTokensWithTextAsync(name, cancellationToken).ConfigureAwait(false);
            SyntaxNode findParentNode(SyntaxToken t) => t.Parent;

            var symbolMatch = GetStandardSymbolsMatchFunction(propertySymbol, findParentNode, document.Project.Solution, cancellationToken);
            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();

            var result = await FindReferencesInTokensAsync(document, tokens, tokensMatchAndAppropriate, symbolMatch, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}
