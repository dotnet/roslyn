// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal abstract class AbstractMemberScopedReferenceFinder<TSymbol> : AbstractReferenceFinder<TSymbol>
        where TSymbol : ISymbol
    {
        protected sealed override bool CanFind(TSymbol symbol)
        {
            return true;
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            TSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            var location = symbol.Locations.FirstOrDefault();
            if (location == null || !location.IsInSource)
            {
                return SpecializedTasks.EmptyImmutableArray<Document>();
            }

            var document = project.GetDocument(location.SourceTree);
            if (document == null)
            {
                return SpecializedTasks.EmptyImmutableArray<Document>();
            }

            if (documents != null && !documents.Contains(document))
            {
                return SpecializedTasks.EmptyImmutableArray<Document>();
            }

            return Task.FromResult(ImmutableArray.Create(document));
        }

        protected override async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var container = GetContainer(symbol);
            if (container != null)
            {
                return await FindReferencesInContainerAsync(symbol, container, document, cancellationToken).ConfigureAwait(false);
            }

            if (symbol.ContainingType != null && symbol.ContainingType.IsScriptClass)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
                var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var tokens = root.DescendantTokens();

                return await FindReferencesInTokensWithSymbolNameAsync(
                    symbol, document, tokens, cancellationToken).ConfigureAwait(false);
            }

            return ImmutableArray<ReferenceLocation>.Empty;
        }

        private static ISymbol GetContainer(ISymbol symbol)
        {
            for (var current = symbol; current != null; current = current.ContainingSymbol)
            {
                if (current is IFieldSymbol || current is IPropertySymbol)
                {
                    return current;
                }

                var method = current as IMethodSymbol;
                if (method != null && (method.MethodKind != MethodKind.AnonymousFunction && method.MethodKind != MethodKind.LocalFunction))
                {
                    return method;
                }
            }

            return null;
        }

        protected Task<ImmutableArray<ReferenceLocation>> FindReferencesInTokensWithSymbolNameAsync(
            TSymbol symbol,
            Document document,
            IEnumerable<SyntaxToken> tokens,
            CancellationToken cancellationToken)
        {
            return FindReferencesInTokensWithSymbolNameAsync(
                symbol, document, tokens,
                findParentNode: null, cancellationToken: cancellationToken);
        }

        protected Task<ImmutableArray<ReferenceLocation>> FindReferencesInTokensWithSymbolNameAsync(
            TSymbol symbol,
            Document document,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var name = symbol.Name;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);

            return FindReferencesInTokensAsync(
                document,
                tokens,
                t => IdentifiersMatch(syntaxFacts, name, t),
                symbolsMatch,
                cancellationToken);
        }

        private Task<ImmutableArray<ReferenceLocation>> FindReferencesInContainerAsync(
            TSymbol symbol,
            ISymbol container,
            Document document,
            CancellationToken cancellationToken)
        {
            return FindReferencesInContainerAsync(
                symbol, container, document,
                findParentNode: null, cancellationToken: cancellationToken);
        }

        private Task<ImmutableArray<ReferenceLocation>> FindReferencesInContainerAsync(
            TSymbol symbol,
            ISymbol container,
            Document document,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<ISymbolDeclarationService>();
            var declarations = service.GetDeclarations(container);
            var tokens = declarations.SelectMany(r => r.GetSyntax(cancellationToken).DescendantTokens());

            var name = symbol.Name;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);
            var tokensMatch = GetTokensMatchFunction(syntaxFacts, name);

            return FindReferencesInTokensAsync(
                document,
                tokens,
                tokensMatch,
                symbolsMatch,
                cancellationToken);
        }

        protected abstract Func<SyntaxToken, bool> GetTokensMatchFunction(ISyntaxFactsService syntaxFacts, string name);
    }
}
