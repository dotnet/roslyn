// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract partial class AbstractReferenceFinder : IReferenceFinder
    {
        public abstract Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(SymbolAndProjectId symbolAndProject, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken);
        public abstract Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(ISymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken);
        public abstract Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken);
        public abstract Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(SymbolAndProjectId symbolAndProjectId, Document document, CancellationToken cancellationToken);

        protected static bool TryGetNameWithoutAttributeSuffix(
            string name,
            ISyntaxFactsService syntaxFacts,
            out string result)
        {
            return name.TryGetWithoutAttributeSuffix(syntaxFacts.IsCaseSensitive, out result);
        }

        protected async Task<ImmutableArray<Document>> FindDocumentsAsync(Project project, IImmutableSet<Document> scope, Func<Document, CancellationToken, Task<bool>> predicateAsync, CancellationToken cancellationToken)
        {
            // special case for HR
            if (scope != null && scope.Count == 1)
            {
                var document = scope.First();
                if (document.Project == project)
                {
                    return scope.ToImmutableArray();
                }

                return ImmutableArray<Document>.Empty;
            }

            var documents = ArrayBuilder<Document>.GetInstance();
            foreach (var document in project.Documents)
            {
                if (scope != null && !scope.Contains(document))
                {
                    continue;
                }

                if (await predicateAsync(document, cancellationToken).ConfigureAwait(false))
                {
                    documents.Add(document);
                }
            }

            return documents.ToImmutableAndFree();
        }

        /// <summary>
        /// Finds all the documents in the provided project that contain the requested string
        /// values
        /// </summary>
        protected Task<ImmutableArray<Document>> FindDocumentsAsync(Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken, params string[] values)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeInfo.GetIdentifierInfoAsync(d, c).ConfigureAwait(false);
                foreach (var value in values)
                {
                    if (!info.ProbablyContainsIdentifier(value))
                    {
                        return false;
                    }
                }

                return true;
            }, cancellationToken);
        }

        protected Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document> documents,
            PredefinedType predefinedType,
            CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeInfo.GetContextInfoAsync(d, c).ConfigureAwait(false);
                return info.ContainsPredefinedType(predefinedType);
            }, cancellationToken);
        }

        protected async Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document> documents,
            PredefinedOperator op,
            CancellationToken cancellationToken)
        {
            if (op == PredefinedOperator.None)
            {
                return ImmutableArray<Document>.Empty;
            }

            return await FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeInfo.GetContextInfoAsync(d, c).ConfigureAwait(false);
                return info.ContainsPredefinedOperator(op);
            }, cancellationToken).ConfigureAwait(false);
        }

        protected static bool IdentifiersMatch(ISyntaxFactsService syntaxFacts, string name, SyntaxToken token)
        {
            return syntaxFacts.IsIdentifier(token) && syntaxFacts.TextMatch(token.ValueText, name);
        }

        protected static Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol symbol,
            string identifier,
            Document document,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, identifier, document, findParentNode: null,
                cancellationToken: cancellationToken);
        }

        protected static Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol symbol,
            string identifier,
            Document document,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);

            return FindReferencesInDocumentUsingIdentifierAsync(
                identifier, document, symbolsMatch, cancellationToken);
        }

        protected static async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            string identifier,
            Document document,
            Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var tokens = await document.GetIdentifierOrGlobalNamespaceTokensWithTextAsync(identifier, cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            Func<SyntaxToken, bool> tokensMatch = t => IdentifiersMatch(syntaxFacts, identifier, t);

            return await FindReferencesInTokensAsync(
                document,
                tokens,
                tokensMatch,
                symbolsMatch,
                cancellationToken).ConfigureAwait(false);
        }

        protected static Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> GetStandardSymbolsMatchFunction(
            ISymbol symbol, Func<SyntaxToken, SyntaxNode> findParentNode, Solution solution, CancellationToken cancellationToken)
        {
            var nodeMatch = GetStandardSymbolsNodeMatchFunction(symbol, solution, cancellationToken);
            findParentNode = findParentNode ?? (t => t.Parent);
            Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> symbolsMatch =
                (token, model) => nodeMatch(findParentNode(token), model);

            return symbolsMatch;
        }

        protected static Func<SyntaxNode, SemanticModel, ValueTuple<bool, CandidateReason>> GetStandardSymbolsNodeMatchFunction(
            ISymbol searchSymbol, Solution solution, CancellationToken cancellationToken)
        {
            Func<SyntaxNode, SemanticModel, ValueTuple<bool, CandidateReason>> symbolsMatch =
                (node, model) =>
                {
                    var symbolInfoToMatch = FindReferenceCache.GetSymbolInfo(model, node, cancellationToken);

                    var symbolToMatch = symbolInfoToMatch.Symbol;
                    var symbolToMatchCompilation = model.Compilation;

                    if (SymbolFinder.OriginalSymbolsMatch(searchSymbol, symbolInfoToMatch.Symbol, solution, null, symbolToMatchCompilation, cancellationToken))
                    {
                        return ValueTuple.Create(true, CandidateReason.None);
                    }
                    else if (symbolInfoToMatch.CandidateSymbols.Any(s => SymbolFinder.OriginalSymbolsMatch(searchSymbol, s, solution, null, symbolToMatchCompilation, cancellationToken)))
                    {
                        return ValueTuple.Create(true, symbolInfoToMatch.CandidateReason);
                    }
                    else
                    {
                        return ValueTuple.Create(false, CandidateReason.None);
                    }
                };

            return symbolsMatch;
        }

        protected static async Task<ImmutableArray<ReferenceLocation>> FindReferencesInTokensAsync(
            Document document,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var semanticFacts = document.Project.LanguageServices.GetService<ISemanticFactsService>();

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var locations = ArrayBuilder<ReferenceLocation>.GetInstance();
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (tokensMatch(token))
                {
                    var match = symbolsMatch(token, semanticModel);
                    if (match.Item1)
                    {
                        var alias = FindReferenceCache.GetAliasInfo(semanticFacts, semanticModel, token, cancellationToken);

                        var location = token.GetLocation();
                        var isWrittenTo = semanticFacts.IsWrittenTo(semanticModel, token.Parent, cancellationToken);
                        locations.Add(new ReferenceLocation(document, alias, location, isImplicit: false, isWrittenTo: isWrittenTo, candidateReason: match.Item2));
                    }
                }
            }

            return locations.ToImmutableAndFree();
        }

        private static async Task<IAliasSymbol> GetAliasSymbolAsync(
            Document document,
            ReferenceLocation location,
            CancellationToken cancellationToken)
        {
            if (location.Location.IsInSource)
            {
                var tree = location.Location.SourceTree;
                var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var token = root.FindToken(location.Location.SourceSpan.Start);
                var node = token.Parent;

                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                if (syntaxFacts.IsRightSideOfQualifiedName(node))
                {
                    node = node.Parent;
                }

                if (syntaxFacts.IsUsingDirectiveName(node))
                {
                    var directive = node.Parent;
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var aliasSymbol = semanticModel.GetDeclaredSymbol(directive, cancellationToken) as IAliasSymbol;
                    if (aliasSymbol != null)
                    {
                        return aliasSymbol;
                    }
                }
            }

            return null;
        }

        protected static Task<ImmutableArray<ReferenceLocation>> FindAliasReferencesAsync(
            ImmutableArray<ReferenceLocation> nonAliasReferences,
            ISymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            return FindAliasReferencesAsync(
                nonAliasReferences, symbol, document,
                findParentNode: null, cancellationToken: cancellationToken);
        }

        protected static async Task<ImmutableArray<ReferenceLocation>> FindAliasReferencesAsync(
            ImmutableArray<ReferenceLocation> nonAliasReferences,
            ISymbol symbol,
            Document document,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = await GetAliasSymbolsAsync(document, nonAliasReferences, cancellationToken).ConfigureAwait(false);
            if (aliasSymbols == null)
            {
                return ImmutableArray<ReferenceLocation>.Empty;
            }

            return await FindReferencesThroughAliasSymbolsAsync(symbol, document, aliasSymbols, findParentNode, cancellationToken).ConfigureAwait(false);
        }

        protected static async Task<ImmutableArray<ReferenceLocation>> FindAliasReferencesAsync(
            ImmutableArray<ReferenceLocation> nonAliasReferences,
            ISymbol symbol,
            Document document,
            Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = await GetAliasSymbolsAsync(document, nonAliasReferences, cancellationToken).ConfigureAwait(false);
            if (aliasSymbols == null)
            {
                return ImmutableArray<ReferenceLocation>.Empty;
            }

            return await FindReferencesThroughAliasSymbolsAsync(symbol, document, aliasSymbols, symbolsMatch, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ImmutableArray<IAliasSymbol>> GetAliasSymbolsAsync(
            Document document,
            ImmutableArray<ReferenceLocation> nonAliasReferences,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = ArrayBuilder<IAliasSymbol>.GetInstance();
            foreach (var r in nonAliasReferences)
            {
                var symbol = await GetAliasSymbolAsync(document, r, cancellationToken).ConfigureAwait(false);
                if (symbol != null)
                {
                    aliasSymbols.Add(symbol);
                }
            }

            return aliasSymbols.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<ReferenceLocation>> FindReferencesThroughAliasSymbolsAsync(
            ISymbol symbol,
            Document document,
            ImmutableArray<IAliasSymbol> aliasSymbols,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var allAliasReferences = ArrayBuilder<ReferenceLocation>.GetInstance();
            foreach (var aliasSymbol in aliasSymbols)
            {
                var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                    symbol, aliasSymbol.Name, document, findParentNode, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);

                // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
                // shortened name as well (e.g. using FooAttribute = MyNamespace.FooAttribute; [Foo] class C1 {})
                string simpleName;
                if (TryGetNameWithoutAttributeSuffix(aliasSymbol.Name, syntaxFactsService, out simpleName))
                {
                    aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                        symbol, simpleName, document, cancellationToken).ConfigureAwait(false);
                    allAliasReferences.AddRange(aliasReferences);
                }
            }

            return allAliasReferences.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<ReferenceLocation>> FindReferencesThroughAliasSymbolsAsync(
            ISymbol symbol,
            Document document,
            ImmutableArray<IAliasSymbol> aliasSymbols,
            Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var allAliasReferences = ArrayBuilder<ReferenceLocation>.GetInstance();
            foreach (var aliasSymbol in aliasSymbols)
            {
                var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(aliasSymbol.Name, document, symbolsMatch, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);

                // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
                // shortened name as well (e.g. using FooAttribute = MyNamespace.FooAttribute; [Foo] class C1 {})
                string simpleName;
                if (TryGetNameWithoutAttributeSuffix(aliasSymbol.Name, syntaxFactsService, out simpleName))
                {
                    aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(simpleName, document, symbolsMatch, cancellationToken).ConfigureAwait(false);
                    allAliasReferences.AddRange(aliasReferences);
                }
            }

            return allAliasReferences.ToImmutableAndFree();
        }

        protected Task<ImmutableArray<Document>> FindDocumentsWithForEachStatementsAsync(Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeInfo.GetContextInfoAsync(d, c).ConfigureAwait(false);
                return info.ContainsForEachStatement;
            }, cancellationToken);
        }

        protected async Task<ImmutableArray<ReferenceLocation>> FindReferencesInForEachStatementsAsync(
            ISymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var syntaxTreeInfo = await SyntaxTreeInfo.GetContextInfoAsync(document, cancellationToken).ConfigureAwait(false);
            if (syntaxTreeInfo.ContainsForEachStatement)
            {
                var semanticFacts = document.Project.LanguageServices.GetService<ISemanticFactsService>();
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var locations = ArrayBuilder<ReferenceLocation>.GetInstance();

                var originalUnreducedSymbolDefinition = symbol.GetOriginalUnreducedDefinition();

                foreach (var node in syntaxRoot.DescendantNodesAndSelf())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var info = semanticFacts.GetForEachSymbols(semanticModel, node);

                    if (Matches(info.GetEnumeratorMethod, originalUnreducedSymbolDefinition) ||
                        Matches(info.MoveNextMethod, originalUnreducedSymbolDefinition) ||
                        Matches(info.CurrentProperty, originalUnreducedSymbolDefinition) ||
                        Matches(info.DisposeMethod, originalUnreducedSymbolDefinition))
                    {
                        var location = node.GetFirstToken().GetLocation();
                        locations.Add(new ReferenceLocation(
                            document, alias: null, location: location, isImplicit: true, isWrittenTo: false, candidateReason: CandidateReason.None));
                    }
                }

                return locations.ToImmutableAndFree();
            }
            else
            {
                return ImmutableArray<ReferenceLocation>.Empty;
            }
        }

        private static bool Matches(ISymbol symbol1, ISymbol notNulloriginalUnreducedSymbol2)
        {
            return symbol1 != null && SymbolEquivalenceComparer.Instance.Equals(
                symbol1.GetOriginalUnreducedDefinition(),
                notNulloriginalUnreducedSymbol2);
        }
    }

    internal abstract partial class AbstractReferenceFinder<TSymbol> : AbstractReferenceFinder
        where TSymbol : ISymbol
    {
        protected abstract bool CanFind(TSymbol symbol);
        protected abstract Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(TSymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken);
        protected abstract Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(TSymbol symbol, Document document, CancellationToken cancellationToken);

        public override Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(ISymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            return symbol is TSymbol && CanFind((TSymbol)symbol)
                ? DetermineProjectsToSearchAsync((TSymbol)symbol, solution, projects, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<Project>();
        }

        public override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return symbol is TSymbol && CanFind((TSymbol)symbol)
                ? DetermineDocumentsToSearchAsync((TSymbol)symbol, project, documents, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<Document>();
        }

        public override Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            SymbolAndProjectId symbolAndProjectId, Document document, CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            return symbol is TSymbol && CanFind((TSymbol)symbol)
                ? FindReferencesInDocumentAsync((TSymbol)symbol, document, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<ReferenceLocation>();
        }

        public override Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            if (symbol is TSymbol && CanFind((TSymbol)symbol))
            {
                return DetermineCascadedSymbolsAsync(
                    symbolAndProjectId.WithSymbol((TSymbol)symbol),
                    solution, projects, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<SymbolAndProjectId>();
        }

        protected virtual Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(
            TSymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            return DependentProjectsFinder.GetDependentProjectsAsync(
                symbol, solution, projects, cancellationToken);
        }

        protected virtual Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<TSymbol> symbolAndProject, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<SymbolAndProjectId>();
        }

        protected static Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentUsingSymbolNameAsync(
            TSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, symbol.Name, document, cancellationToken: cancellationToken);
        }

        protected Task<ImmutableArray<ReferenceLocation>> FindReferencesInTokensAsync(
            TSymbol symbol,
            Document document,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            CancellationToken cancellationToken)
        {
            return FindReferencesInTokensAsync(
                symbol, document, tokens, tokensMatch,
                findParentNode: null, cancellationToken: cancellationToken);
        }

        protected Task<ImmutableArray<ReferenceLocation>> FindReferencesInTokensAsync(
            TSymbol symbol,
            Document document,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);

            return FindReferencesInTokensAsync(
                document,
                tokens,
                tokensMatch,
                symbolsMatch,
                cancellationToken);
        }

        protected static Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            Func<SyntaxToken, bool> tokensMatch,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(
                symbol, document, tokensMatch,
                findParentNode: null, cancellationToken: cancellationToken);
        }

        protected static Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);
            return FindReferencesInDocumentAsync(symbol, document, tokensMatch, symbolsMatch, cancellationToken);
        }

        protected static async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Now that we have Doc Comments in place, We are searching for References in the Trivia as well by setting descendIntoTrivia: true
            var tokens = root.DescendantTokens(descendIntoTrivia: true);
            return await FindReferencesInTokensAsync(document, tokens, tokensMatch, symbolsMatch, cancellationToken).ConfigureAwait(false);
        }
    }
}