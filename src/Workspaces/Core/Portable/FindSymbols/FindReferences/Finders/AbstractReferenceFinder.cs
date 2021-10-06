// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract partial class AbstractReferenceFinder : IReferenceFinder
    {
        public const string ContainingTypeInfoPropertyName = "ContainingTypeInfo";
        public const string ContainingMemberInfoPropertyName = "ContainingMemberInfo";

        public abstract Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken);

        public abstract Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            ISymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken);

        public abstract Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ISymbol symbol, HashSet<string>? globalAliases, Project project, IImmutableSet<Document>? documents, FindReferencesSearchOptions options, CancellationToken cancellationToken);

        public abstract ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ISymbol symbol, HashSet<string>? globalAliases, Document document, SemanticModel semanticModel, FindReferencesSearchOptions options, CancellationToken cancellationToken);

        protected static bool TryGetNameWithoutAttributeSuffix(
            string name,
            ISyntaxFactsService syntaxFacts,
            [NotNullWhen(returnValue: true)] out string? result)
        {
            return name.TryGetWithoutAttributeSuffix(syntaxFacts.IsCaseSensitive, out result);
        }

        protected static async Task<ImmutableArray<Document>> FindDocumentsAsync(Project project, IImmutableSet<Document>? scope, Func<Document, CancellationToken, Task<bool>> predicateAsync, CancellationToken cancellationToken)
        {
            // special case for highlight references
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
            foreach (var document in await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
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
        protected static Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document>? documents,
            CancellationToken cancellationToken,
            params string[] values)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeIndex.GetRequiredIndexAsync(d, c).ConfigureAwait(false);
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

        /// <summary>
        /// Finds all the documents in the provided project that contain a global attribute in them.
        /// </summary>
        protected static Task<ImmutableArray<Document>> FindDocumentsWithGlobalAttributesAsync(
            Project project,
            IImmutableSet<Document>? documents,
            CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeIndex.GetRequiredIndexAsync(d, c).ConfigureAwait(false);
                return info.ContainsGlobalAttributes;
            }, cancellationToken);
        }

        protected static Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document>? documents,
            PredefinedType predefinedType,
            CancellationToken cancellationToken)
        {
            if (predefinedType == PredefinedType.None)
            {
                return SpecializedTasks.EmptyImmutableArray<Document>();
            }

            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeIndex.GetRequiredIndexAsync(d, c).ConfigureAwait(false);
                return info.ContainsPredefinedType(predefinedType);
            }, cancellationToken);
        }

        protected static Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document>? documents,
            PredefinedOperator op,
            CancellationToken cancellationToken)
        {
            if (op == PredefinedOperator.None)
                return SpecializedTasks.EmptyImmutableArray<Document>();

            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeIndex.GetRequiredIndexAsync(d, c).ConfigureAwait(false);
                return info.ContainsPredefinedOperator(op);
            }, cancellationToken);
        }

        protected static bool IdentifiersMatch(ISyntaxFactsService syntaxFacts, string name, SyntaxToken token)
            => syntaxFacts.IsIdentifier(token) && syntaxFacts.TextMatch(token.ValueText, name);

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol symbol,
            string identifier,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, identifier, document, semanticModel, findParentNode: null, cancellationToken);
        }

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol symbol,
            string identifier,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, identifier, document, semanticModel, symbolsMatch, cancellationToken);
        }

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        protected static async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol _,
            string identifier,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            CancellationToken cancellationToken)
        {
            var tokens = await GetIdentifierOrGlobalNamespaceTokensWithTextAsync(document, semanticModel, identifier, cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            return await FindReferencesInTokensAsync(
                document,
                semanticModel,
                tokens,
                t => IdentifiersMatch(syntaxFacts, identifier, t),
                symbolsMatchAsync,
                cancellationToken).ConfigureAwait(false);
        }

        protected static async Task<ImmutableArray<SyntaxToken>> GetIdentifierOrGlobalNamespaceTokensWithTextAsync(Document document, SemanticModel semanticModel, string identifier, CancellationToken cancellationToken)
        {
            // It's very costly to walk an entire tree.  So if the tree is simple and doesn't contain
            // any unicode escapes in it, then we do simple string matching to find the tokens.
            var info = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
            if (!info.ProbablyContainsIdentifier(identifier))
                return ImmutableArray<SyntaxToken>.Empty;

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts == null)
                return ImmutableArray<SyntaxToken>.Empty;

            var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            SourceText? text = null;
            if (!info.ProbablyContainsEscapedIdentifier(identifier))
                text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return FindReferenceCache.GetIdentifierOrGlobalNamespaceTokensWithText(
                syntaxFacts, semanticModel, root, text, identifier, cancellationToken);
        }

        protected static Func<SyntaxToken, SyntaxNode>? GetNamedTypeOrConstructorFindParentNodeFunction(Document document, ISymbol searchSymbol)
        {
            // delegates don't have exposed symbols for their constructors.  so when you do `new MyDel()`, that's only a
            // reference to a type (as we don't have any real constructor symbols that can actually cascade to).  So
            // don't do any special finding in that case.
            if (searchSymbol.IsDelegateType())
                return null;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return t => syntaxFacts.TryGetBindableParent(t) ?? t.Parent!;
        }

        protected static Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> GetStandardSymbolsMatchFunction(
            ISymbol symbol, Func<SyntaxToken, SyntaxNode>? findParentNode, Solution solution, CancellationToken cancellationToken)
        {
            var nodeMatchAsync = GetStandardSymbolsNodeMatchFunction(symbol, solution, cancellationToken);
            findParentNode ??= t => t.Parent!;
            return (token, model) => nodeMatchAsync(findParentNode(token), model);
        }

        protected static Func<SyntaxNode, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> GetStandardSymbolsNodeMatchFunction(
            ISymbol searchSymbol, Solution solution, CancellationToken cancellationToken)
        {
            return async (node, model) =>
            {
                var symbolInfo = FindReferenceCache.GetSymbolInfo(model, node, cancellationToken);

                if (await SymbolFinder.OriginalSymbolsMatchAsync(solution, searchSymbol, symbolInfo.Symbol, cancellationToken).ConfigureAwait(false))
                    return (matched: true, CandidateReason.None);

                foreach (var candidate in symbolInfo.CandidateSymbols)
                {
                    if (await SymbolFinder.OriginalSymbolsMatchAsync(solution, searchSymbol, candidate, cancellationToken).ConfigureAwait(false))
                        return (matched: true, symbolInfo.CandidateReason);
                }

                return default;
            };
        }

        protected static async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync(
            Document document,
            SemanticModel semanticModel,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

            var locations = ArrayBuilder<FinderLocation>.GetInstance();
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (tokensMatch(token))
                {
                    var (matched, reason) = await symbolsMatchAsync(token, semanticModel).ConfigureAwait(false);
                    if (matched)
                    {
                        RoslynDebug.Assert(token.Parent != null);

                        var alias = FindReferenceCache.GetAliasInfo(semanticFacts, semanticModel, token, cancellationToken);

                        var location = token.GetLocation();
                        var symbolUsageInfo = GetSymbolUsageInfo(token.Parent, semanticModel, syntaxFacts, semanticFacts, cancellationToken);

                        locations.Add(new FinderLocation(token.Parent, new ReferenceLocation(
                            document, alias, location, isImplicit: false,
                            symbolUsageInfo, GetAdditionalFindUsagesProperties(token.Parent, semanticModel, syntaxFacts), candidateReason: reason)));
                    }
                }
            }

            return locations.ToImmutableAndFree();
        }

        private static IAliasSymbol? GetAliasSymbol(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsRightOfQualifiedName(node))
                node = node.GetRequiredParent();

            if (syntaxFacts.IsUsingDirectiveName(node))
            {
                var directive = node.GetRequiredParent();

                // In the case of a same-named alias.  i.e. `using Console = System.Console;` we don't actually want
                // search for the alias.  We'll already be checking any references called 'Console' and will find them
                // as matches.
                if (semanticModel.GetDeclaredSymbol(directive, cancellationToken) is IAliasSymbol aliasSymbol &&
                    !syntaxFacts.StringComparer.Equals(aliasSymbol.Name, aliasSymbol.Target.Name))
                {
                    return aliasSymbol;
                }
            }

            return null;
        }

        protected static Task<ImmutableArray<FinderLocation>> FindLocalAliasReferencesAsync(
            ArrayBuilder<FinderLocation> initialReferences,
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindLocalAliasReferencesAsync(
                initialReferences, symbol, document, semanticModel, findParentNode: null, cancellationToken);
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindLocalAliasReferencesAsync(
            ArrayBuilder<FinderLocation> initialReferences,
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = GetLocalAliasSymbols(document, semanticModel, initialReferences, cancellationToken);
            return aliasSymbols.IsDefaultOrEmpty
                ? ImmutableArray<FinderLocation>.Empty
                : await FindReferencesThroughLocalAliasSymbolsAsync(symbol, document, semanticModel, aliasSymbols, findParentNode, cancellationToken).ConfigureAwait(false);
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindLocalAliasReferencesAsync(
            ArrayBuilder<FinderLocation> initialReferences,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = GetLocalAliasSymbols(document, semanticModel, initialReferences, cancellationToken);
            return aliasSymbols.IsDefaultOrEmpty
                ? ImmutableArray<FinderLocation>.Empty
                : await FindReferencesThroughLocalAliasSymbolsAsync(document, semanticModel, aliasSymbols, symbolsMatchAsync, cancellationToken).ConfigureAwait(false);
        }

        private static ImmutableArray<IAliasSymbol> GetLocalAliasSymbols(
            Document document,
            SemanticModel semanticModel,
            ArrayBuilder<FinderLocation> initialReferences,
            CancellationToken cancellationToken)
        {
            using var aliasSymbols = TemporaryArray<IAliasSymbol>.Empty;
            foreach (var reference in initialReferences)
            {
                var symbol = GetAliasSymbol(document, semanticModel, reference.Node, cancellationToken);
                if (symbol != null)
                    aliasSymbols.Add(symbol);
            }

            return aliasSymbols.ToImmutableAndClear();
        }

        private static async Task<ImmutableArray<FinderLocation>> FindReferencesThroughLocalAliasSymbolsAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            ImmutableArray<IAliasSymbol> localAliasSymbols,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var allAliasReferences);
            foreach (var localAliasSymbol in localAliasSymbols)
            {
                var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                    symbol, localAliasSymbol.Name, document, semanticModel, findParentNode, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);
                // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
                // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
                if (TryGetNameWithoutAttributeSuffix(localAliasSymbol.Name, syntaxFactsService, out var simpleName))
                {
                    aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                        symbol, simpleName, document, semanticModel, cancellationToken).ConfigureAwait(false);
                    allAliasReferences.AddRange(aliasReferences);
                }
            }

            return allAliasReferences.ToImmutable();
        }

        private static async Task<ImmutableArray<FinderLocation>> FindReferencesThroughLocalAliasSymbolsAsync(
            Document document,
            SemanticModel semanticModel,
            ImmutableArray<IAliasSymbol> localAliasSymbols,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var allAliasReferences);
            foreach (var aliasSymbol in localAliasSymbols)
            {
                var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                    aliasSymbol, aliasSymbol.Name, document, semanticModel, symbolsMatchAsync, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);
                // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
                // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
                if (TryGetNameWithoutAttributeSuffix(aliasSymbol.Name, syntaxFactsService, out var simpleName))
                {
                    aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                        aliasSymbol, simpleName, document, semanticModel, symbolsMatchAsync, cancellationToken).ConfigureAwait(false);
                    allAliasReferences.AddRange(aliasReferences);
                }
            }

            return allAliasReferences.ToImmutable();
        }

        protected static Task<ImmutableArray<Document>> FindDocumentsWithPredicateAsync(Project project, IImmutableSet<Document>? documents, Func<SyntaxTreeIndex, bool> predicate, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeIndex.GetRequiredIndexAsync(d, c).ConfigureAwait(false);
                return predicate(info);
            }, cancellationToken);
        }

        protected static Task<ImmutableArray<Document>> FindDocumentsWithForEachStatementsAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, predicate: sti => sti.ContainsForEachStatement, cancellationToken);

        protected static Task<ImmutableArray<Document>> FindDocumentsWithDeconstructionAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, predicate: sti => sti.ContainsDeconstruction, cancellationToken);

        protected static Task<ImmutableArray<Document>> FindDocumentsWithAwaitExpressionAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, predicate: sti => sti.ContainsAwait, cancellationToken);

        protected static Task<ImmutableArray<Document>> FindDocumentsWithImplicitObjectCreationExpressionAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
            => FindDocumentsWithPredicateAsync(project, documents, predicate: sti => sti.ContainsImplicitObjectCreation, cancellationToken);

        /// <summary>
        /// If the `node` implicitly matches the `symbol`, then it will be added to `locations`.
        /// </summary>
        protected delegate void CollectMatchingReferences(
            SyntaxNode node, ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations);

        protected static async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            Document document,
            Func<SyntaxTreeIndex, bool> isRelevantDocument,
            CollectMatchingReferences collectMatchingReferences,
            CancellationToken cancellationToken)
        {
            var syntaxTreeInfo = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
            if (isRelevantDocument(syntaxTreeInfo))
            {
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var locations);

                foreach (var node in syntaxRoot.DescendantNodesAndSelf())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    collectMatchingReferences(node, syntaxFacts, semanticFacts, locations);
                }

                return locations.ToImmutable();
            }

            return ImmutableArray<FinderLocation>.Empty;
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInForEachStatementsAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(document, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

            static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsForEachStatement;

            void CollectMatchingReferences(
                SyntaxNode node, ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations)
            {
                var info = semanticFacts.GetForEachSymbols(semanticModel, node);

                if (Matches(info.GetEnumeratorMethod, symbol) ||
                    Matches(info.MoveNextMethod, symbol) ||
                    Matches(info.CurrentProperty, symbol) ||
                    Matches(info.DisposeMethod, symbol))
                {
                    var location = node.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);

                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document,
                        alias: null,
                        location: location,
                        isImplicit: true,
                        symbolUsageInfo,
                        GetAdditionalFindUsagesProperties(node, semanticModel, syntaxFacts),
                        candidateReason: CandidateReason.None)));
                }
            }
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInDeconstructionAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(document, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

            static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsDeconstruction;

            void CollectMatchingReferences(
                SyntaxNode node, ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations)
            {
                var deconstructMethods = semanticFacts.GetDeconstructionAssignmentMethods(semanticModel, node);
                if (deconstructMethods.IsEmpty)
                {
                    // This was not a deconstruction assignment, it may still be a deconstruction foreach
                    deconstructMethods = semanticFacts.GetDeconstructionForEachMethods(semanticModel, node);
                }

                if (deconstructMethods.Any(m => Matches(m, symbol)))
                {
                    var location = syntaxFacts.GetDeconstructionReferenceLocation(node);
                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);

                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document, alias: null, location, isImplicit: true, symbolUsageInfo, GetAdditionalFindUsagesProperties(node, semanticModel, syntaxFacts), CandidateReason.None)));
                }
            }
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInAwaitExpressionAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(document, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

            static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsAwait;

            void CollectMatchingReferences(
                SyntaxNode node, ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations)
            {
                var awaitExpressionMethod = semanticFacts.GetGetAwaiterMethod(semanticModel, node);

                if (Matches(awaitExpressionMethod, symbol))
                {
                    var location = node.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);

                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document, alias: null, location, isImplicit: true, symbolUsageInfo, GetAdditionalFindUsagesProperties(node, semanticModel, syntaxFacts), CandidateReason.None)));
                }
            }
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInImplicitObjectCreationExpressionAsync(
            ISymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(document, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

            static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsImplicitObjectCreation;

            void CollectMatchingReferences(
                SyntaxNode node, ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, ArrayBuilder<FinderLocation> locations)
            {
                // Avoid binding unrelated nodes
                if (!syntaxFacts.IsImplicitObjectCreationExpression(node))
                    return;

                var constructor = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;

                if (Matches(constructor, symbol))
                {
                    var location = node.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);

                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document, alias: null, location, isImplicit: true, symbolUsageInfo, GetAdditionalFindUsagesProperties(node, semanticModel, syntaxFacts), CandidateReason.None)));
                }
            }
        }

        protected static bool Matches(ISymbol? symbol1, ISymbol notNullOriginalUnreducedSymbol2)
        {
            Contract.ThrowIfFalse(notNullOriginalUnreducedSymbol2.GetOriginalUnreducedDefinition().Equals(notNullOriginalUnreducedSymbol2));
            return symbol1 != null && SymbolEquivalenceComparer.Instance.Equals(
                symbol1.GetOriginalUnreducedDefinition(),
                notNullOriginalUnreducedSymbol2);
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
                else if (node.FirstAncestorOrSelf<SyntaxNode, ISyntaxFactsService>((node, syntaxFacts) => syntaxFacts.IsUsingOrExternOrImport(node), syntaxFacts) != null)
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
                else if (syntaxFacts.IsTypeOfObjectCreationExpression(node))
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

            if (syntaxFacts.IsRightOfQualifiedName(node) ||
                syntaxFacts.IsNameOfSimpleMemberAccessExpression(node) ||
                syntaxFacts.IsNameOfMemberBindingExpression(node))
            {
                return syntaxFacts.IsLeftSideOfDot(node.Parent);
            }

            return false;
        }

        internal static ImmutableDictionary<string, string> GetAdditionalFindUsagesProperties(SyntaxNode node, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts)
        {
            var additionalProperties = ImmutableDictionary.CreateBuilder<string, string>();

            if (TryGetAdditionalProperty(
                    syntaxFacts.GetContainingTypeDeclaration(node, node.SpanStart),
                    ContainingTypeInfoPropertyName,
                    semanticModel,
                    out var containingTypeProperty))
            {
                additionalProperties.Add(containingTypeProperty);
            }

            if (TryGetAdditionalProperty(
                    syntaxFacts.GetContainingMemberDeclaration(node, node.SpanStart),
                    ContainingMemberInfoPropertyName,
                    semanticModel,
                    out var containingMemberProperty))
            {
                additionalProperties.Add(containingMemberProperty);
            }

            return additionalProperties.ToImmutable();
        }

        internal static ImmutableDictionary<string, string> GetAdditionalFindUsagesProperties(ISymbol definition)
        {
            var additionalProperties = ImmutableDictionary.CreateBuilder<string, string>();

            var containingType = definition.ContainingType;
            if (containingType != null &&
                TryGetAdditionalProperty(ContainingTypeInfoPropertyName, containingType, out var containingTypeProperty))
            {
                additionalProperties.Add(containingTypeProperty);
            }

            var containingSymbol = definition.ContainingSymbol;

            // Containing member should only include fields, properties, methods, or events.  Since ContainingSymbol can return other types, use the return value of GetMemberType to restrict to members only.)
            if (containingSymbol != null &&
                containingSymbol.GetMemberType() != null &&
                TryGetAdditionalProperty(ContainingMemberInfoPropertyName, containingSymbol, out var containingMemberProperty))
            {
                additionalProperties.Add(containingMemberProperty);
            }

            return additionalProperties.ToImmutable();
        }

        protected static bool TryGetAdditionalProperty(SyntaxNode? node, string name, SemanticModel semanticModel, out KeyValuePair<string, string> additionalProperty)
        {
            if (node != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(node);
                if (symbol != null &&
                    TryGetAdditionalProperty(name, symbol, out additionalProperty))
                {
                    return true;
                }
            }

            additionalProperty = default;
            return false;
        }

        private static bool TryGetAdditionalProperty(string propertyName, ISymbol symbol, out KeyValuePair<string, string> additionalProperty)
        {
            if (symbol == null)
            {
                additionalProperty = default;
                return false;
            }

            additionalProperty = new KeyValuePair<string, string>(propertyName, symbol.Name);
            return true;
        }
    }

    internal abstract partial class AbstractReferenceFinder<TSymbol> : AbstractReferenceFinder
        where TSymbol : ISymbol
    {
        protected abstract bool CanFind(TSymbol symbol);

        protected abstract Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            TSymbol symbol, HashSet<string>? globalAliases, Project project, IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        protected abstract ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol, HashSet<string>? globalAliases, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        protected virtual Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(
            TSymbol symbol, Project project, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<string>();
        }

        public sealed override Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            return symbol is TSymbol typedSymbol && CanFind(typedSymbol)
                ? DetermineGlobalAliasesAsync(typedSymbol, project, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<string>();
        }

        public sealed override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ISymbol symbol, HashSet<string>? globalAliases, Project project,
            IImmutableSet<Document>? documents, FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return symbol is TSymbol typedSymbol && CanFind(typedSymbol)
                ? DetermineDocumentsToSearchAsync(typedSymbol, globalAliases, project, documents, options, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<Document>();
        }

        public sealed override ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ISymbol symbol, HashSet<string>? globalAliases, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return symbol is TSymbol typedSymbol && CanFind(typedSymbol)
                ? FindReferencesInDocumentAsync(typedSymbol, globalAliases, document, semanticModel, options, cancellationToken)
                : new ValueTask<ImmutableArray<FinderLocation>>(ImmutableArray<FinderLocation>.Empty);
        }

        public sealed override Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            ISymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            if (options.Cascade &&
                symbol is TSymbol typedSymbol &&
                CanFind(typedSymbol))
            {
                return DetermineCascadedSymbolsAsync(typedSymbol, solution, options, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

        protected virtual Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            TSymbol symbol,
            Solution solution,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingSymbolNameAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, symbol.Name, document, semanticModel, cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            CancellationToken cancellationToken)
        {
            return FindReferencesInTokensAsync(
                symbol, document, semanticModel, tokens, tokensMatch, findParentNode: null, cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatchAsync = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);

            return FindReferencesInTokensAsync(
                document,
                semanticModel,
                tokens,
                tokensMatch,
                symbolsMatchAsync,
                cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, bool> tokensMatch,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(
                symbol, document, semanticModel, tokensMatch, findParentNode: null, cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatchAsync = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);
            return FindReferencesInDocumentAsync(document, semanticModel, tokensMatch, symbolsMatchAsync, cancellationToken);
        }

        protected static async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, bool> tokensMatch,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Now that we have Doc Comments in place, We are searching for References in the Trivia as well by setting descendIntoTrivia: true
            var tokens = root.DescendantTokens(descendIntoTrivia: true);
            return await FindReferencesInTokensAsync(document, semanticModel, tokens, tokensMatch, symbolsMatchAsync, cancellationToken).ConfigureAwait(false);
        }

        protected static async Task<ImmutableArray<string>> GetAllMatchingGlobalAliasNamesAsync(
            Project project, string name, int arity, CancellationToken cancellationToken)
        {
            using var result = TemporaryArray<string>.Empty;

            foreach (var document in await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
            {
                var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                foreach (var alias in index.GetGlobalAliases(name, arity))
                    result.Add(alias);
            }

            return result.ToImmutableAndClear();
        }
    }
}
