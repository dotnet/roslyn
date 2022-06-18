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
            ISymbol symbol, FindReferencesDocumentState state, FindReferencesSearchOptions options, CancellationToken cancellationToken);

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
                    return scope.ToImmutableArray();

                return ImmutableArray<Document>.Empty;
            }

            using var _ = ArrayBuilder<Document>.GetInstance(out var documents);
            foreach (var document in await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
            {
                if (scope != null && !scope.Contains(document))
                    continue;

                if (await predicateAsync(document, cancellationToken).ConfigureAwait(false))
                    documents.Add(document);
            }

            return documents.ToImmutable();
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
            return FindDocumentsWithPredicateAsync(project, documents, index =>
            {
                foreach (var value in values)
                {
                    if (!index.ProbablyContainsIdentifier(value))
                        return false;
                }

                return true;
            }, cancellationToken);
        }

        /// <summary>
        /// Finds all the documents in the provided project that contain a global attribute in them.
        /// </summary>
        protected static Task<ImmutableArray<Document>> FindDocumentsWithGlobalSuppressMessageAttributeAsync(
            Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
        {
            return FindDocumentsWithPredicateAsync(project, documents, index => index.ContainsGlobalSuppressMessageAttribute, cancellationToken);
        }

        protected static Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document>? documents,
            PredefinedType predefinedType,
            CancellationToken cancellationToken)
        {
            if (predefinedType == PredefinedType.None)
                return SpecializedTasks.EmptyImmutableArray<Document>();

            return FindDocumentsWithPredicateAsync(project, documents, index => index.ContainsPredefinedType(predefinedType), cancellationToken);
        }

        protected static Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document>? documents,
            PredefinedOperator op,
            CancellationToken cancellationToken)
        {
            if (op == PredefinedOperator.None)
                return SpecializedTasks.EmptyImmutableArray<Document>();

            return FindDocumentsWithPredicateAsync(project, documents, index => index.ContainsPredefinedOperator(op), cancellationToken);
        }

        protected static bool IdentifiersMatch(ISyntaxFactsService syntaxFacts, string name, SyntaxToken token)
            => syntaxFacts.IsIdentifier(token) && syntaxFacts.TextMatch(token.ValueText, name);

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol symbol,
            string identifier,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, identifier, state, findParentNode: null, cancellationToken);
        }

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol symbol,
            string identifier,
            FindReferencesDocumentState state,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, state, cancellationToken);
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, identifier, state, symbolsMatch, cancellationToken);
        }

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        protected static async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
            ISymbol _,
            string identifier,
            FindReferencesDocumentState state,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            CancellationToken cancellationToken)
        {
            var tokens = await FindMatchingIdentifierTokensAsync(state, identifier, cancellationToken).ConfigureAwait(false);

            var syntaxFacts = state.SyntaxFacts;

            return await FindReferencesInTokensAsync(
                state,
                tokens,
                static (state, token, identifier) => IdentifiersMatch(state.SyntaxFacts, identifier, token),
                symbolsMatchAsync,
                identifier,
                cancellationToken).ConfigureAwait(false);
        }

        protected static Task<ImmutableArray<SyntaxToken>> FindMatchingIdentifierTokensAsync(FindReferencesDocumentState state, string identifier, CancellationToken cancellationToken)
            => state.Cache.FindMatchingIdentifierTokensAsync(state.Document, identifier, cancellationToken);

        protected static Func<SyntaxToken, SyntaxNode>? GetNamedTypeOrConstructorFindParentNodeFunction(
            FindReferencesDocumentState state, ISymbol searchSymbol)
        {
            // delegates don't have exposed symbols for their constructors.  so when you do `new MyDel()`, that's only a
            // reference to a type (as we don't have any real constructor symbols that can actually cascade to).  So
            // don't do any special finding in that case.
            if (searchSymbol.IsDelegateType())
                return null;

            var syntaxFacts = state.SyntaxFacts;
            return t => syntaxFacts.TryGetBindableParent(t) ?? t.Parent!;
        }

        protected static Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> GetStandardSymbolsMatchFunction(
            ISymbol symbol,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            var nodeMatchAsync = GetStandardSymbolsNodeMatchFunction(symbol, state, cancellationToken);
            findParentNode ??= t => t.Parent!;
            return (token, model) => nodeMatchAsync(findParentNode(token), model);
        }

        protected static Func<SyntaxNode, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> GetStandardSymbolsNodeMatchFunction(
            ISymbol searchSymbol, FindReferencesDocumentState state, CancellationToken cancellationToken)
        {
            return async (node, model) =>
            {
                var symbolInfo = state.Cache.GetSymbolInfo(node, cancellationToken);

                if (await SymbolFinder.OriginalSymbolsMatchAsync(state.Solution, searchSymbol, symbolInfo.Symbol, cancellationToken).ConfigureAwait(false))
                    return (matched: true, CandidateReason.None);

                foreach (var candidate in symbolInfo.CandidateSymbols)
                {
                    if (await SymbolFinder.OriginalSymbolsMatchAsync(state.Solution, searchSymbol, candidate, cancellationToken).ConfigureAwait(false))
                        return (matched: true, symbolInfo.CandidateReason);
                }

                return default;
            };
        }

        protected static async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync<T>(
            FindReferencesDocumentState state,
            IEnumerable<SyntaxToken> tokens,
            Func<FindReferencesDocumentState, SyntaxToken, T, bool> tokensMatch,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            T value,
            CancellationToken cancellationToken)
        {
            var semanticFacts = state.SemanticFacts;

            var locations = ArrayBuilder<FinderLocation>.GetInstance();
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (tokensMatch(state, token, value))
                {
                    var semanticModel = state.SemanticModel;
                    var (matched, reason) = await symbolsMatchAsync(token, semanticModel).ConfigureAwait(false);
                    if (matched)
                    {
                        RoslynDebug.Assert(token.Parent != null);

                        var alias = state.Cache.GetAliasInfo(semanticFacts, token, cancellationToken);

                        var location = token.GetLocation();
                        var symbolUsageInfo = GetSymbolUsageInfo(token.Parent, state, cancellationToken);

                        locations.Add(new FinderLocation(token.Parent, new ReferenceLocation(
                            state.Document, alias, location, isImplicit: false,
                            symbolUsageInfo, GetAdditionalFindUsagesProperties(token.Parent, state), reason)));
                    }
                }
            }

            return locations.ToImmutableAndFree();
        }

        private static IAliasSymbol? GetAliasSymbol(
            FindReferencesDocumentState state,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = state.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsRightOfQualifiedName(node))
                node = node.GetRequiredParent();

            if (syntaxFacts.IsUsingDirectiveName(node))
            {
                var directive = node.GetRequiredParent();

                // In the case of a same-named alias.  i.e. `using Console = System.Console;` we don't actually want
                // search for the alias.  We'll already be checking any references called 'Console' and will find them
                // as matches.
                if (state.SemanticModel.GetDeclaredSymbol(directive, cancellationToken) is IAliasSymbol aliasSymbol &&
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
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            return FindLocalAliasReferencesAsync(
                initialReferences, symbol, state, findParentNode: null, cancellationToken);
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindLocalAliasReferencesAsync(
            ArrayBuilder<FinderLocation> initialReferences,
            ISymbol symbol,
            FindReferencesDocumentState state,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = GetLocalAliasSymbols(state, initialReferences, cancellationToken);
            return aliasSymbols.IsDefaultOrEmpty
                ? ImmutableArray<FinderLocation>.Empty
                : await FindReferencesThroughLocalAliasSymbolsAsync(symbol, state, aliasSymbols, findParentNode, cancellationToken).ConfigureAwait(false);
        }

        protected static async Task<ImmutableArray<FinderLocation>> FindLocalAliasReferencesAsync(
            ArrayBuilder<FinderLocation> initialReferences,
            FindReferencesDocumentState state,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            CancellationToken cancellationToken)
        {
            var aliasSymbols = GetLocalAliasSymbols(state, initialReferences, cancellationToken);
            return aliasSymbols.IsDefaultOrEmpty
                ? ImmutableArray<FinderLocation>.Empty
                : await FindReferencesThroughLocalAliasSymbolsAsync(state, aliasSymbols, symbolsMatchAsync, cancellationToken).ConfigureAwait(false);
        }

        private static ImmutableArray<IAliasSymbol> GetLocalAliasSymbols(
            FindReferencesDocumentState state,
            ArrayBuilder<FinderLocation> initialReferences,
            CancellationToken cancellationToken)
        {
            using var aliasSymbols = TemporaryArray<IAliasSymbol>.Empty;
            foreach (var reference in initialReferences)
            {
                var symbol = GetAliasSymbol(state, reference.Node, cancellationToken);
                if (symbol != null)
                    aliasSymbols.Add(symbol);
            }

            return aliasSymbols.ToImmutableAndClear();
        }

        private static async Task<ImmutableArray<FinderLocation>> FindReferencesThroughLocalAliasSymbolsAsync(
            ISymbol symbol,
            FindReferencesDocumentState state,
            ImmutableArray<IAliasSymbol> localAliasSymbols,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = state.SyntaxFacts;
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var allAliasReferences);
            foreach (var localAliasSymbol in localAliasSymbols)
            {
                var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                    symbol, localAliasSymbol.Name, state, findParentNode, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);
                // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
                // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
                if (TryGetNameWithoutAttributeSuffix(localAliasSymbol.Name, syntaxFacts, out var simpleName))
                {
                    aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                        symbol, simpleName, state, cancellationToken).ConfigureAwait(false);
                    allAliasReferences.AddRange(aliasReferences);
                }
            }

            return allAliasReferences.ToImmutable();
        }

        private static async Task<ImmutableArray<FinderLocation>> FindReferencesThroughLocalAliasSymbolsAsync(
            FindReferencesDocumentState state,
            ImmutableArray<IAliasSymbol> localAliasSymbols,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = state.SyntaxFacts;
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var allAliasReferences);
            foreach (var aliasSymbol in localAliasSymbols)
            {
                var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                    aliasSymbol, aliasSymbol.Name, state, symbolsMatchAsync, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);
                // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
                // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
                if (TryGetNameWithoutAttributeSuffix(aliasSymbol.Name, syntaxFacts, out var simpleName))
                {
                    aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                        aliasSymbol, simpleName, state, symbolsMatchAsync, cancellationToken).ConfigureAwait(false);
                    allAliasReferences.AddRange(aliasReferences);
                }
            }

            return allAliasReferences.ToImmutable();
        }

        protected static Task<ImmutableArray<Document>> FindDocumentsWithPredicateAsync(
            Project project, IImmutableSet<Document>? documents, Func<SyntaxTreeIndex, bool> predicate, CancellationToken cancellationToken)
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
            SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations);

        protected static async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            FindReferencesDocumentState state,
            Func<SyntaxTreeIndex, bool> isRelevantDocument,
            CollectMatchingReferences collectMatchingReferences,
            CancellationToken cancellationToken)
        {
            var document = state.Document;
            var syntaxTreeInfo = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
            if (isRelevantDocument(syntaxTreeInfo))
            {
                using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var locations);

                foreach (var node in state.Root.DescendantNodesAndSelf())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    collectMatchingReferences(node, state, locations);
                }

                return locations.ToImmutable();
            }

            return ImmutableArray<FinderLocation>.Empty;
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInForEachStatementsAsync(
            ISymbol symbol,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            var document = state.Document;
            var semanticModel = state.SemanticModel;
            return FindReferencesInDocumentAsync(state, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

            static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsForEachStatement;

            void CollectMatchingReferences(
                SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations)
            {
                var semanticFacts = state.SemanticFacts;
                var info = semanticFacts.GetForEachSymbols(semanticModel, node);

                if (Matches(info.GetEnumeratorMethod, symbol) ||
                    Matches(info.MoveNextMethod, symbol) ||
                    Matches(info.CurrentProperty, symbol) ||
                    Matches(info.DisposeMethod, symbol))
                {
                    var location = node.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document,
                        alias: null,
                        location: location,
                        isImplicit: true,
                        symbolUsageInfo,
                        GetAdditionalFindUsagesProperties(node, state),
                        candidateReason: CandidateReason.None)));
                }
            }
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInDeconstructionAsync(
            ISymbol symbol,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            var document = state.Document;
            var semanticModel = state.SemanticModel;
            return FindReferencesInDocumentAsync(state, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

            static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsDeconstruction;

            void CollectMatchingReferences(
                SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations)
            {
                var semanticFacts = state.SemanticFacts;
                var deconstructMethods = semanticFacts.GetDeconstructionAssignmentMethods(semanticModel, node);
                if (deconstructMethods.IsEmpty)
                {
                    // This was not a deconstruction assignment, it may still be a deconstruction foreach
                    deconstructMethods = semanticFacts.GetDeconstructionForEachMethods(semanticModel, node);
                }

                if (deconstructMethods.Any(static (m, symbol) => Matches(m, symbol), symbol))
                {
                    var location = state.SyntaxFacts.GetDeconstructionReferenceLocation(node);
                    var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        document, alias: null, location, isImplicit: true, symbolUsageInfo,
                        GetAdditionalFindUsagesProperties(node, state), CandidateReason.None)));
                }
            }
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInAwaitExpressionAsync(
            ISymbol symbol,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(state, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

            static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsAwait;

            void CollectMatchingReferences(
                SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations)
            {
                var semanticModel = state.SemanticModel;
                var awaitExpressionMethod = state.SemanticFacts.GetGetAwaiterMethod(semanticModel, node);

                if (Matches(awaitExpressionMethod, symbol))
                {
                    var location = node.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        state.Document, alias: null, location, isImplicit: true, symbolUsageInfo,
                        GetAdditionalFindUsagesProperties(node, state), CandidateReason.None)));
                }
            }
        }

        protected Task<ImmutableArray<FinderLocation>> FindReferencesInImplicitObjectCreationExpressionAsync(
            ISymbol symbol,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(state, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

            static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
                => syntaxTreeInfo.ContainsImplicitObjectCreation;

            void CollectMatchingReferences(
                SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations)
            {
                var syntaxFacts = state.SyntaxFacts;

                // Avoid binding unrelated nodes
                if (!syntaxFacts.IsImplicitObjectCreationExpression(node))
                    return;

                var constructor = state.SemanticModel.GetSymbolInfo(node, cancellationToken).Symbol;

                if (Matches(constructor, symbol))
                {
                    var location = node.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                    locations.Add(new FinderLocation(node, new ReferenceLocation(
                        state.Document, alias: null, location, isImplicit: true, symbolUsageInfo,
                        GetAdditionalFindUsagesProperties(node, state), CandidateReason.None)));
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
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = state.SyntaxFacts;
            var semanticFacts = state.SemanticFacts;
            var semanticModel = state.SemanticModel;

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

        internal static ImmutableDictionary<string, string> GetAdditionalFindUsagesProperties(
            SyntaxNode node, FindReferencesDocumentState state)
        {
            var additionalProperties = ImmutableDictionary.CreateBuilder<string, string>();

            var syntaxFacts = state.SyntaxFacts;
            var semanticModel = state.SemanticModel;

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
            TSymbol symbol, FindReferencesDocumentState state, FindReferencesSearchOptions options, CancellationToken cancellationToken);

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
            ISymbol symbol, FindReferencesDocumentState state, FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return symbol is TSymbol typedSymbol && CanFind(typedSymbol)
                ? FindReferencesInDocumentAsync(typedSymbol, state, options, cancellationToken)
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
            TSymbol symbol, FindReferencesDocumentState state, CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, symbol.Name, state, cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync<T>(
            TSymbol symbol,
            FindReferencesDocumentState state,
            IEnumerable<SyntaxToken> tokens,
            Func<FindReferencesDocumentState, SyntaxToken, T, bool> tokensMatch,
            T value,
            CancellationToken cancellationToken)
        {
            return FindReferencesInTokensAsync(
                symbol, state, tokens, tokensMatch, findParentNode: null, value, cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync<T>(
            TSymbol symbol,
            FindReferencesDocumentState state,
            IEnumerable<SyntaxToken> tokens,
            Func<FindReferencesDocumentState, SyntaxToken, T, bool> tokensMatch,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            T value,
            CancellationToken cancellationToken)
        {
            var symbolsMatchAsync = GetStandardSymbolsMatchFunction(
                symbol, findParentNode, state, cancellationToken);

            return FindReferencesInTokensAsync(
                state,
                tokens,
                tokensMatch,
                symbolsMatchAsync,
                value,
                cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync<T>(
            TSymbol symbol,
            FindReferencesDocumentState state,
            Func<FindReferencesDocumentState, SyntaxToken, T, bool> tokensMatch,
            T value,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentAsync(
                symbol, state, tokensMatch, findParentNode: null, value, cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync<T>(
            TSymbol symbol,
            FindReferencesDocumentState state,
            Func<FindReferencesDocumentState, SyntaxToken, T, bool> tokensMatch,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            T value,
            CancellationToken cancellationToken)
        {
            var symbolsMatchAsync = GetStandardSymbolsMatchFunction(symbol, findParentNode, state, cancellationToken);
            return FindReferencesInDocumentAsync(state, tokensMatch, symbolsMatchAsync, value, cancellationToken);
        }

        protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync<T>(
            FindReferencesDocumentState state,
            Func<FindReferencesDocumentState, SyntaxToken, T, bool> tokensMatch,
            Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> symbolsMatchAsync,
            T value,
            CancellationToken cancellationToken)
        {
            var root = state.Root;

            // Now that we have Doc Comments in place, We are searching for References in the Trivia as well by setting descendIntoTrivia: true
            var tokens = root.DescendantTokens(descendIntoTrivia: true);
            return FindReferencesInTokensAsync(
                state, tokens, tokensMatch, symbolsMatchAsync, value, cancellationToken);
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
