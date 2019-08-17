// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract partial class AbstractReferenceFinder : IReferenceFinder
    {
        public abstract Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId symbolAndProject, Solution solution, IImmutableSet<Project> projects,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        public abstract Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(ISymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken);

        public abstract Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ISymbol symbol, Project project, IImmutableSet<Document> documents, FindReferencesSearchOptions options, CancellationToken cancellationToken);

        public abstract Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            SymbolAndProjectId symbolAndProjectId, Document document, SemanticModel semanticModel, FindReferencesSearchOptions options, CancellationToken cancellationToken);

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
                var info = await SyntaxTreeIndex.GetIndexAsync(d, c).ConfigureAwait(false);
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
            if (predefinedType == PredefinedType.None)
            {
                return SpecializedTasks.EmptyImmutableArray<Document>();
            }

            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeIndex.GetIndexAsync(d, c).ConfigureAwait(false);
                return info.ContainsPredefinedType(predefinedType);
            }, cancellationToken);
        }

        protected Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document> documents,
            PredefinedOperator op,
            CancellationToken cancellationToken)
        {
            if (op == PredefinedOperator.None)
            {
                return SpecializedTasks.EmptyImmutableArray<Document>();
            }

            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeIndex.GetIndexAsync(d, c).ConfigureAwait(false);
                return info.ContainsPredefinedOperator(op);
            }, cancellationToken);
        }

        protected static bool IdentifiersMatch(ISyntaxFactsService syntaxFacts, string name, SyntaxToken token)
        {
            return syntaxFacts.IsIdentifier(token) && syntaxFacts.TextMatch(token.ValueText, name);
        }

        protected static Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol symbol,
            string identifier,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, identifier, document, semanticModel, findParentNode: null,
                cancellationToken: cancellationToken);
        }

        protected static Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol symbol,
            string identifier,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);

            return FindReferencesInDocumentUsingIdentifierAsync(
                identifier, document, semanticModel, symbolsMatch, cancellationToken);
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            string identifier,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SemanticModel, (bool matched, CandidateReason reason)> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var tokens = await document.GetIdentifierOrGlobalNamespaceTokensWithTextAsync(semanticModel, identifier, cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            bool tokensMatch(SyntaxToken t) => IdentifiersMatch(syntaxFacts, identifier, t);

            return await FindReferencesInTokensAsync(
                document,
                semanticModel,
                tokens,
                tokensMatch,
                symbolsMatch,
                cancellationToken).ConfigureAwait(false);
        }

        protected static Func<SyntaxToken, SemanticModel, (bool matched, CandidateReason reason)> GetStandardSymbolsMatchFunction(
            ISymbol symbol, Func<SyntaxToken, SyntaxNode> findParentNode, Solution solution, CancellationToken cancellationToken)
        {
            var nodeMatch = GetStandardSymbolsNodeMatchFunction(symbol, solution, cancellationToken);
            findParentNode ??= (t => t.Parent);
            (bool matched, CandidateReason reason) symbolsMatch(SyntaxToken token, SemanticModel model)
                => nodeMatch(findParentNode(token), model);

            return symbolsMatch;
        }

        protected static Func<SyntaxNode, SemanticModel, (bool matched, CandidateReason reason)> GetStandardSymbolsNodeMatchFunction(
            ISymbol searchSymbol, Solution solution, CancellationToken cancellationToken)
        {
            (bool matched, CandidateReason reason) symbolsMatch(SyntaxNode node, SemanticModel model)
            {
                var symbolInfoToMatch = FindReferenceCache.GetSymbolInfo(model, node, cancellationToken);

                var symbolToMatch = symbolInfoToMatch.Symbol;
                var symbolToMatchCompilation = model.Compilation;

                if (SymbolFinder.OriginalSymbolsMatch(searchSymbol, symbolInfoToMatch.Symbol, solution, null, symbolToMatchCompilation, cancellationToken))
                {
                    return (matched: true, CandidateReason.None);
                }
                else if (symbolInfoToMatch.CandidateSymbols.Any(s => SymbolFinder.OriginalSymbolsMatch(searchSymbol, s, solution, null, symbolToMatchCompilation, cancellationToken)))
                {
                    return (matched: true, symbolInfoToMatch.CandidateReason);
                }
                else
                {
                    return (matched: false, CandidateReason.None);
                }
            }

            return symbolsMatch;
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync(
            Document document,
            SemanticModel semanticModel,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SemanticModel, (bool matched, CandidateReason reason)> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var locations = ArrayBuilder<FinderLocation>.GetInstance();
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (tokensMatch(token))
                {
                    var match = symbolsMatch(token, semanticModel);
                    if (match.matched)
                    {
                        var alias = FindReferenceCache.GetAliasInfo(semanticFacts, semanticModel, token, cancellationToken);

                        var location = token.GetLocation();
                        var symbolUsageInfo = GetSymbolUsageInfo(token.Parent, semanticModel, syntaxFacts, semanticFacts, cancellationToken);
                        var isWrittenTo = symbolUsageInfo.IsWrittenTo();
                        locations.Add(new FinderLocation(token.Parent, new ReferenceLocation(
                            document, alias, location, isImplicit: false,
                            symbolUsageInfo, candidateReason: match.reason)));
                    }
                }
            }

            return locations.ToImmutableAndFree();
        }

        private static IAliasSymbol GetAliasSymbol(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsRightSideOfQualifiedName(node))
            {
                node = node.Parent;
            }

            if (syntaxFacts.IsUsingDirectiveName(node))
            {
                var directive = node.Parent;
                if (semanticModel.GetDeclaredSymbol(directive, cancellationToken) is IAliasSymbol aliasSymbol)
                {
                    return aliasSymbol;
                }
            }

            return null;
        }

        protected static Task<ImmutableArray<FinderLocation>> FindAliasReferencesAsync(
            ImmutableArray<FinderLocation> nonAliasReferences,
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindAliasReferencesAsync(
                nonAliasReferences, symbol, document, semanticModel,
                findParentNode: null, cancellationToken: cancellationToken);
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindAliasReferencesAsync(
            ImmutableArray<FinderLocation> nonAliasReferences,
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = GetAliasSymbols(document, semanticModel, nonAliasReferences, cancellationToken);
            if (aliasSymbols == null)
            {
                return ImmutableArray<FinderLocation>.Empty;
            }

            return await FindReferencesThroughAliasSymbolsAsync(symbol, document, semanticModel, aliasSymbols, findParentNode, cancellationToken).ConfigureAwait(false);
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindAliasReferencesAsync(
            ImmutableArray<FinderLocation> nonAliasReferences,
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SemanticModel, (bool matched, CandidateReason reason)> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = GetAliasSymbols(document, semanticModel, nonAliasReferences, cancellationToken);
            if (aliasSymbols == null)
            {
                return ImmutableArray<FinderLocation>.Empty;
            }

            return await FindReferencesThroughAliasSymbolsAsync(symbol, document, semanticModel, aliasSymbols, symbolsMatch, cancellationToken).ConfigureAwait(false);
        }

        private static ImmutableArray<IAliasSymbol> GetAliasSymbols(
            Document document,
            SemanticModel semanticModel,
            ImmutableArray<FinderLocation> nonAliasReferences,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = ArrayBuilder<IAliasSymbol>.GetInstance();
            foreach (var r in nonAliasReferences)
            {
                var symbol = GetAliasSymbol(document, semanticModel, r.Node, cancellationToken);
                if (symbol != null)
                {
                    aliasSymbols.Add(symbol);
                }
            }

            return aliasSymbols.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<FinderLocation>> FindReferencesThroughAliasSymbolsAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            ImmutableArray<IAliasSymbol> aliasSymbols,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var allAliasReferences = ArrayBuilder<FinderLocation>.GetInstance();
            foreach (var aliasSymbol in aliasSymbols)
            {
                var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                    symbol, aliasSymbol.Name, document, semanticModel, findParentNode, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);
                // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
                // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
                if (TryGetNameWithoutAttributeSuffix(aliasSymbol.Name, syntaxFactsService, out var simpleName))
                {
                    aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                        symbol, simpleName, document, semanticModel, cancellationToken).ConfigureAwait(false);
                    allAliasReferences.AddRange(aliasReferences);
                }
            }

            return allAliasReferences.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<FinderLocation>> FindReferencesThroughAliasSymbolsAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            ImmutableArray<IAliasSymbol> aliasSymbols,
            Func<SyntaxToken, SemanticModel, (bool matched, CandidateReason reason)> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var allAliasReferences = ArrayBuilder<FinderLocation>.GetInstance();
            foreach (var aliasSymbol in aliasSymbols)
            {
                var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(aliasSymbol.Name, document, semanticModel, symbolsMatch, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);
                // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
                // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
                if (TryGetNameWithoutAttributeSuffix(aliasSymbol.Name, syntaxFactsService, out var simpleName))
                {
                    aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(simpleName, document, semanticModel, symbolsMatch, cancellationToken).ConfigureAwait(false);
                    allAliasReferences.AddRange(aliasReferences);
                }
            }

            return allAliasReferences.ToImmutableAndFree();
        }

        protected Task<ImmutableArray<Document>> FindDocumentsWithPredicateAsync(Project project, IImmutableSet<Document> documents, Func<SyntaxTreeIndex, bool> predicate, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeIndex.GetIndexAsync(d, c).ConfigureAwait(false);
                return predicate(info);
            }, cancellationToken);
        }

        protected Task<ImmutableArray<Document>> FindDocumentsWithForEachStatementsAsync(Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, predicate: sti => sti.ContainsForEachStatement, cancellationToken);

        protected Task<ImmutableArray<Document>> FindDocumentsWithDeconstructionAsync(Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, predicate: sti => sti.ContainsDeconstruction, cancellationToken);

        protected Task<ImmutableArray<Document>> FindDocumentsWithAwaitExpressionAsync(Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, predicate: sti => sti.ContainsAwait, cancellationToken);

        /// <summary>
        /// If the `node` implicitly matches the `symbol`, then it will be added to `locations`.
        /// </summary>
        private delegate void CollectMatchingReferences(ISymbol symbol, SyntaxNode node,
            ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations);

        private async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ISymbol symbol,
            Document document,
            Func<SyntaxTreeIndex, bool> isRelevantDocument,
            CollectMatchingReferences collectMatchingReferences,
            CancellationToken cancellationToken)
        {
            var syntaxTreeInfo = await SyntaxTreeIndex.GetIndexAsync(document, cancellationToken).ConfigureAwait(false);
            if (isRelevantDocument(syntaxTreeInfo))
            {
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var locations = ArrayBuilder<FinderLocation>.GetInstance();

                var originalUnreducedSymbolDefinition = symbol.GetOriginalUnreducedDefinition();

                foreach (var node in syntaxRoot.DescendantNodesAndSelf())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    collectMatchingReferences(originalUnreducedSymbolDefinition, node, syntaxFacts, semanticFacts, locations);
                }

                return locations.ToImmutableAndFree();
            }

            return ImmutableArray<FinderLocation>.Empty;
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInForEachStatementsAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(symbol, document, isRelevantDocument, collectMatchingReferences, cancellationToken);

            bool isRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsForEachStatement;

            void collectMatchingReferences(ISymbol originalUnreducedSymbolDefinition, SyntaxNode node,
                ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations)
            {
                var info = semanticFacts.GetForEachSymbols(semanticModel, node);

                if (Matches(info.GetEnumeratorMethod, originalUnreducedSymbolDefinition) ||
                    Matches(info.MoveNextMethod, originalUnreducedSymbolDefinition) ||
                    Matches(info.CurrentProperty, originalUnreducedSymbolDefinition) ||
                    Matches(info.DisposeMethod, originalUnreducedSymbolDefinition))
                {
                    var location = node.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);
                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document, alias: null, location: location, isImplicit: true, symbolUsageInfo, candidateReason: CandidateReason.None)));
                }
            }
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInDeconstructionAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(symbol, document, isRelevantDocument, collectMatchingReferences, cancellationToken);

            bool isRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsDeconstruction;

            void collectMatchingReferences(ISymbol originalUnreducedSymbolDefinition, SyntaxNode node,
                ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations)
            {
                var deconstructMethods = semanticFacts.GetDeconstructionAssignmentMethods(semanticModel, node);
                if (deconstructMethods.IsEmpty)
                {
                    // This was not a deconstruction assignment, it may still be a deconstruction foreach
                    deconstructMethods = semanticFacts.GetDeconstructionForEachMethods(semanticModel, node);
                }

                if (deconstructMethods.Any(m => Matches(m, originalUnreducedSymbolDefinition)))
                {
                    var location = syntaxFacts.GetDeconstructionReferenceLocation(node);
                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);
                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document, alias: null, location, isImplicit: true, symbolUsageInfo, CandidateReason.None)));
                }
            }
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInAwaitExpressionAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(symbol, document, isRelevantDocument, collectMatchingReferences, cancellationToken);

            bool isRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsAwait;

            void collectMatchingReferences(ISymbol originalUnreducedSymbolDefinition, SyntaxNode node,
                ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations)
            {
                var awaitExpressionMethod = semanticFacts.GetGetAwaiterMethod(semanticModel, node);

                if (Matches(awaitExpressionMethod, originalUnreducedSymbolDefinition))
                {
                    var location = node.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);
                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document, alias: null, location, isImplicit: true, symbolUsageInfo, CandidateReason.None)));
                }
            }
        }

        private static bool Matches(ISymbol symbol1, ISymbol notNulloriginalUnreducedSymbol2)
        {
            return symbol1 != null && SymbolEquivalenceComparer.Instance.Equals(
                symbol1.GetOriginalUnreducedDefinition(),
                notNulloriginalUnreducedSymbol2);
        }

        protected static SymbolUsageInfo GetSymbolUsageInfo(
            SyntaxNode node,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            CancellationToken cancellationToken)
        {
            if (syntaxFacts.IsInNamespaceOrTypeContext(node))
            {
                var typeOrNamespaceUsageInfo = GetTypeOrNamespaceUsageInfo();
                return SymbolUsageInfo.Create(typeOrNamespaceUsageInfo);
            }

            return GetSymbolUsageInfoCommon();

            // Local functions.
            TypeOrNamespaceUsageInfo GetTypeOrNamespaceUsageInfo()
            {
                var usageInfo = IsNodeOrAnyAncestorLeftSideOfDot(node, syntaxFacts) || syntaxFacts.IsLeftSideOfExplicitInterfaceSpecifier(node)
                    ? TypeOrNamespaceUsageInfo.Qualified
                    : TypeOrNamespaceUsageInfo.None;

                if (semanticFacts.IsNamespaceDeclarationNameContext(semanticModel, node.SpanStart, cancellationToken))
                {
                    usageInfo |= TypeOrNamespaceUsageInfo.NamespaceDeclaration;
                }
                else if (node.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsUsingOrExternOrImport) != null)
                {
                    usageInfo |= TypeOrNamespaceUsageInfo.Import;
                }

                while (syntaxFacts.IsQualifiedName(node.Parent))
                {
                    node = node.Parent;
                }

                if (syntaxFacts.IsTypeArgumentList(node.Parent))
                {
                    usageInfo |= TypeOrNamespaceUsageInfo.TypeArgument;
                }
                else if (syntaxFacts.IsTypeConstraint(node.Parent))
                {
                    usageInfo |= TypeOrNamespaceUsageInfo.TypeConstraint;
                }
                else if (syntaxFacts.IsBaseTypeList(node.Parent) ||
                    syntaxFacts.IsBaseTypeList(node.Parent?.Parent))
                {
                    usageInfo |= TypeOrNamespaceUsageInfo.Base;
                }
                else if (syntaxFacts.IsObjectCreationExpressionType(node))
                {
                    usageInfo |= TypeOrNamespaceUsageInfo.ObjectCreation;
                }

                return usageInfo;
            }

            SymbolUsageInfo GetSymbolUsageInfoCommon()
            {
                if (semanticFacts.IsInOutContext(semanticModel, node, cancellationToken))
                {
                    return SymbolUsageInfo.Create(ValueUsageInfo.WritableReference);
                }
                else if (semanticFacts.IsInRefContext(semanticModel, node, cancellationToken))
                {
                    return SymbolUsageInfo.Create(ValueUsageInfo.ReadableWritableReference);
                }
                else if (semanticFacts.IsInInContext(semanticModel, node, cancellationToken))
                {
                    return SymbolUsageInfo.Create(ValueUsageInfo.ReadableReference);
                }
                else if (semanticFacts.IsOnlyWrittenTo(semanticModel, node, cancellationToken))
                {
                    return SymbolUsageInfo.Create(ValueUsageInfo.Write);
                }
                else
                {
                    var operation = semanticModel.GetOperation(node, cancellationToken);
                    switch (operation?.Parent)
                    {
                        case INameOfOperation _:
                        case ITypeOfOperation _:
                        case ISizeOfOperation _:
                            return SymbolUsageInfo.Create(ValueUsageInfo.Name);
                    }

                    if (node.IsPartOfStructuredTrivia())
                    {
                        return SymbolUsageInfo.Create(ValueUsageInfo.Name);
                    }

                    var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
                    if (symbolInfo.Symbol != null)
                    {
                        switch (symbolInfo.Symbol.Kind)
                        {
                            case SymbolKind.Namespace:
                                var namespaceUsageInfo = TypeOrNamespaceUsageInfo.None;
                                if (semanticFacts.IsNamespaceDeclarationNameContext(semanticModel, node.SpanStart, cancellationToken))
                                {
                                    namespaceUsageInfo |= TypeOrNamespaceUsageInfo.NamespaceDeclaration;
                                }

                                if (IsNodeOrAnyAncestorLeftSideOfDot(node, syntaxFacts))
                                {
                                    namespaceUsageInfo |= TypeOrNamespaceUsageInfo.Qualified;
                                }

                                return SymbolUsageInfo.Create(namespaceUsageInfo);

                            case SymbolKind.NamedType:
                                var typeUsageInfo = TypeOrNamespaceUsageInfo.None;
                                if (IsNodeOrAnyAncestorLeftSideOfDot(node, syntaxFacts))
                                {
                                    typeUsageInfo |= TypeOrNamespaceUsageInfo.Qualified;
                                }

                                return SymbolUsageInfo.Create(typeUsageInfo);

                            case SymbolKind.Method:
                            case SymbolKind.Property:
                            case SymbolKind.Field:
                            case SymbolKind.Event:
                            case SymbolKind.Parameter:
                            case SymbolKind.Local:
                                var valueUsageInfo = ValueUsageInfo.Read;
                                if (semanticFacts.IsWrittenTo(semanticModel, node, cancellationToken))
                                {
                                    valueUsageInfo |= ValueUsageInfo.Write;
                                }

                                return SymbolUsageInfo.Create(valueUsageInfo);
                        }
                    }

                    return SymbolUsageInfo.None;
                }
            }
        }

        private static bool IsNodeOrAnyAncestorLeftSideOfDot(SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
            if (syntaxFacts.IsLeftSideOfDot(node))
            {
                return true;
            }

            if (syntaxFacts.IsRightSideOfQualifiedName(node) || syntaxFacts.IsNameOfMemberAccessExpression(node))
            {
                return syntaxFacts.IsLeftSideOfDot(node.Parent);
            }

            return false;
        }
    }

    internal abstract partial class AbstractReferenceFinder<TSymbol> : AbstractReferenceFinder
        where TSymbol : ISymbol
    {
        protected abstract bool CanFind(TSymbol symbol);

        protected abstract Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            TSymbol symbol, Project project, IImmutableSet<Document> documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        protected abstract Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        public override Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(ISymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            return symbol is TSymbol && CanFind((TSymbol)symbol)
                ? DetermineProjectsToSearchAsync((TSymbol)symbol, solution, projects, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<Project>();
        }

        public async override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ISymbol symbol, Project project, IImmutableSet<Document> documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            if (symbol is TSymbol targetSymbol && CanFind(targetSymbol))
            {
                var documentsToSearch = await DetermineDocumentsToSearchAsync(targetSymbol, project, documents, options, cancellationToken).ConfigureAwait(false);

                if (options.IgnoreUnchangeableDocuments)
                {
                    documentsToSearch = documentsToSearch.WhereAsArray(d => d.CanApplyChange());
                }

                return documentsToSearch;
            }

            return ImmutableArray<Document>.Empty;
        }

        public override Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            SymbolAndProjectId symbolAndProjectId, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            return symbol is TSymbol && CanFind((TSymbol)symbol)
                ? FindReferencesInDocumentAsync((TSymbol)symbol, document, semanticModel, options, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<FinderLocation>();
        }

        public override Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, IImmutableSet<Project> projects,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            if (symbol is TSymbol typedSymbol && CanFind(typedSymbol))
            {
                return DetermineCascadedSymbolsAsync(
                    symbolAndProjectId.WithSymbol(typedSymbol),
                    solution, projects, options, cancellationToken);
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
            SymbolAndProjectId<TSymbol> symbolAndProject, Solution solution, IImmutableSet<Project> projects,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<SymbolAndProjectId>();
        }

        protected static Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingSymbolNameAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, symbol.Name, document, semanticModel, cancellationToken: cancellationToken);
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            CancellationToken cancellationToken)
        {
            return FindReferencesInTokensAsync(
                symbol, document, semanticModel, tokens, tokensMatch,
                findParentNode: null, cancellationToken: cancellationToken);
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);

            return FindReferencesInTokensAsync(
                document,
                semanticModel,
                tokens,
                tokensMatch,
                symbolsMatch,
                cancellationToken);
        }

        protected static Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, bool> tokensMatch,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(
                symbol, document, semanticModel, tokensMatch,
                findParentNode: null, cancellationToken: cancellationToken);
        }

        protected static Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);
            return FindReferencesInDocumentAsync(symbol, document, semanticModel, tokensMatch, symbolsMatch, cancellationToken);
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SemanticModel, (bool matched, CandidateReason reason)> symbolsMatch,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Now that we have Doc Comments in place, We are searching for References in the Trivia as well by setting descendIntoTrivia: true
            var tokens = root.DescendantTokens(descendIntoTrivia: true);
            return await FindReferencesInTokensAsync(document, semanticModel, tokens, tokensMatch, symbolsMatch, cancellationToken).ConfigureAwait(false);
        }
    }
}
