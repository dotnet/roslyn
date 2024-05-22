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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal abstract partial class AbstractReferenceFinder : IReferenceFinder
{
    public const string ContainingTypeInfoPropertyName = "ContainingTypeInfo";
    public const string ContainingMemberInfoPropertyName = "ContainingMemberInfo";

    public abstract Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(
        ISymbol symbol, Project project, CancellationToken cancellationToken);

    public abstract ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        ISymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken);

    public abstract Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
        ISymbol symbol, HashSet<string>? globalAliases, Project project, IImmutableSet<Document>? documents, FindReferencesSearchOptions options, CancellationToken cancellationToken);

    public abstract ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
        ISymbol symbol, FindReferencesDocumentState state, FindReferencesSearchOptions options, CancellationToken cancellationToken);

    private static ValueTask<(bool matched, CandidateReason reason)> SymbolsMatchAsync(
        ISymbol symbol, FindReferencesDocumentState state, SyntaxToken token, CancellationToken cancellationToken)
    {
        // delegates don't have exposed symbols for their constructors.  so when you do `new MyDel()`, that's only a
        // reference to a type (as we don't have any real constructor symbols that can actually cascade to).  So
        // don't do any special finding in that case.
        var parent = symbol.IsDelegateType()
            ? token.Parent
            : state.SyntaxFacts.TryGetBindableParent(token);
        parent ??= token.Parent!;

        return SymbolsMatchAsync(symbol, state, parent, cancellationToken);
    }

    protected static ValueTask<(bool matched, CandidateReason reason)> SymbolsMatchAsync(
        ISymbol searchSymbol, FindReferencesDocumentState state, SyntaxNode node, CancellationToken cancellationToken)
    {
        var symbolInfo = state.Cache.GetSymbolInfo(node, cancellationToken);

        return MatchesAsync(searchSymbol, state, symbolInfo, cancellationToken);
    }

    protected static async ValueTask<(bool matched, CandidateReason reason)> MatchesAsync(
        ISymbol searchSymbol, FindReferencesDocumentState state, SymbolInfo symbolInfo, CancellationToken cancellationToken)
    {
        if (await SymbolFinder.OriginalSymbolsMatchAsync(state.Solution, searchSymbol, symbolInfo.Symbol, cancellationToken).ConfigureAwait(false))
            return (matched: true, CandidateReason.None);

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (await SymbolFinder.OriginalSymbolsMatchAsync(state.Solution, searchSymbol, candidate, cancellationToken).ConfigureAwait(false))
                return (matched: true, symbolInfo.CandidateReason);
        }

        return default;
    }

    protected static bool TryGetNameWithoutAttributeSuffix(
        string name,
        ISyntaxFactsService syntaxFacts,
        [NotNullWhen(returnValue: true)] out string? result)
    {
        return name.TryGetWithoutAttributeSuffix(syntaxFacts.IsCaseSensitive, out result);
    }

    protected static async Task<ImmutableArray<Document>> FindDocumentsAsync<T>(
        Project project,
        IImmutableSet<Document>? scope,
        Func<Document, T, CancellationToken, ValueTask<bool>> predicateAsync,
        T value,
        CancellationToken cancellationToken)
    {
        // special case for highlight references
        if (scope != null && scope.Count == 1)
        {
            var document = scope.First();
            if (document.Project == project)
                return scope.ToImmutableArray();

            return [];
        }

        using var _ = ArrayBuilder<Document>.GetInstance(out var documents);
        foreach (var document in await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (scope != null && !scope.Contains(document))
                continue;

            if (await predicateAsync(document, value, cancellationToken).ConfigureAwait(false))
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
        return FindDocumentsWithPredicateAsync(project, documents, static (index, values) =>
        {
            foreach (var value in values)
            {
                if (!index.ProbablyContainsIdentifier(value))
                    return false;
            }

            return true;
        }, values, cancellationToken);
    }

    /// <summary>
    /// Finds all the documents in the provided project that contain a global attribute in them.
    /// </summary>
    protected static Task<ImmutableArray<Document>> FindDocumentsWithGlobalSuppressMessageAttributeAsync(
        Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
    {
        return FindDocumentsWithPredicateAsync(
            project, documents, static index => index.ContainsGlobalSuppressMessageAttribute, cancellationToken);
    }

    protected static Task<ImmutableArray<Document>> FindDocumentsAsync(
        Project project,
        IImmutableSet<Document>? documents,
        PredefinedType predefinedType,
        CancellationToken cancellationToken)
    {
        if (predefinedType == PredefinedType.None)
            return SpecializedTasks.EmptyImmutableArray<Document>();

        return FindDocumentsWithPredicateAsync(
            project, documents, static (index, predefinedType) => index.ContainsPredefinedType(predefinedType), predefinedType, cancellationToken);
    }

    protected static bool IdentifiersMatch(ISyntaxFactsService syntaxFacts, string name, SyntaxToken token)
        => syntaxFacts.IsIdentifier(token) && syntaxFacts.TextMatch(token.ValueText, name);

    [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
    protected static async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingIdentifierAsync(
        ISymbol symbol,
        string identifier,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        var tokens = await FindMatchingIdentifierTokensAsync(state, identifier, cancellationToken).ConfigureAwait(false);
        return await FindReferencesInTokensAsync(symbol, state, tokens, cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask<ImmutableArray<SyntaxToken>> FindMatchingIdentifierTokensAsync(FindReferencesDocumentState state, string identifier, CancellationToken cancellationToken)
        => state.Cache.FindMatchingIdentifierTokensAsync(state.Document, identifier, cancellationToken);

    protected static async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInTokensAsync(
        ISymbol symbol,
        FindReferencesDocumentState state,
        ImmutableArray<SyntaxToken> tokens,
        CancellationToken cancellationToken)
    {
        if (tokens.IsEmpty)
            return [];

        using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var locations);
        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (matched, reason) = await SymbolsMatchAsync(
                symbol, state, token, cancellationToken).ConfigureAwait(false);
            if (matched)
            {
                var finderLocation = CreateFinderLocation(state, token, reason, cancellationToken);

                locations.Add(finderLocation);
            }
        }

        return locations.ToImmutable();
    }

    protected static FinderLocation CreateFinderLocation(FindReferencesDocumentState state, SyntaxToken token, CandidateReason reason, CancellationToken cancellationToken)
        => new(token.GetRequiredParent(), CreateReferenceLocation(state, token, reason, cancellationToken));

    public static ReferenceLocation CreateReferenceLocation(FindReferencesDocumentState state, SyntaxToken token, CandidateReason reason, CancellationToken cancellationToken)
        => new(
            state.Document,
            state.Cache.GetAliasInfo(state.SemanticFacts, token, cancellationToken),
            token.GetLocation(),
            isImplicit: false,
            GetSymbolUsageInfo(token.GetRequiredParent(), state, cancellationToken),
            GetAdditionalFindUsagesProperties(token.GetRequiredParent(), state),
            reason);

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

    protected static async Task<ImmutableArray<FinderLocation>> FindLocalAliasReferencesAsync(
        ArrayBuilder<FinderLocation> initialReferences,
        ISymbol symbol,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        var aliasSymbols = GetLocalAliasSymbols(state, initialReferences, cancellationToken);
        return aliasSymbols.IsDefaultOrEmpty
            ? []
            : await FindReferencesThroughLocalAliasSymbolsAsync(symbol, state, aliasSymbols, cancellationToken).ConfigureAwait(false);
    }

    protected static async Task<ImmutableArray<FinderLocation>> FindLocalAliasReferencesAsync(
        ArrayBuilder<FinderLocation> initialReferences,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        var aliasSymbols = GetLocalAliasSymbols(state, initialReferences, cancellationToken);
        return aliasSymbols.IsDefaultOrEmpty
            ? []
            : await FindReferencesThroughLocalAliasSymbolsAsync(state, aliasSymbols, cancellationToken).ConfigureAwait(false);
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
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var allAliasReferences);
        foreach (var localAliasSymbol in localAliasSymbols)
        {
            var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                symbol, localAliasSymbol.Name, state, cancellationToken).ConfigureAwait(false);
            allAliasReferences.AddRange(aliasReferences);
            // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
            // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
            if (TryGetNameWithoutAttributeSuffix(localAliasSymbol.Name, state.SyntaxFacts, out var simpleName))
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
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var allAliasReferences);
        foreach (var aliasSymbol in localAliasSymbols)
        {
            var aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                aliasSymbol, aliasSymbol.Name, state, cancellationToken).ConfigureAwait(false);
            allAliasReferences.AddRange(aliasReferences);
            // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
            // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
            if (TryGetNameWithoutAttributeSuffix(aliasSymbol.Name, state.SyntaxFacts, out var simpleName))
            {
                aliasReferences = await FindReferencesInDocumentUsingIdentifierAsync(
                    aliasSymbol, simpleName, state, cancellationToken).ConfigureAwait(false);
                allAliasReferences.AddRange(aliasReferences);
            }
        }

        return allAliasReferences.ToImmutable();
    }

    protected static Task<ImmutableArray<Document>> FindDocumentsWithPredicateAsync<T>(
        Project project,
        IImmutableSet<Document>? documents,
        Func<SyntaxTreeIndex, T, bool> predicate,
        T value,
        CancellationToken cancellationToken)
    {
        return FindDocumentsAsync(project, documents, static async (d, t, c) =>
        {
            var info = await SyntaxTreeIndex.GetRequiredIndexAsync(d, c).ConfigureAwait(false);
            return t.predicate(info, t.value);
        }, (predicate, value), cancellationToken);
    }

    protected static Task<ImmutableArray<Document>> FindDocumentsWithPredicateAsync(
        Project project,
        IImmutableSet<Document>? documents,
        Func<SyntaxTreeIndex, bool> predicate,
        CancellationToken cancellationToken)
    {
        return FindDocumentsWithPredicateAsync(
            project, documents,
            static (info, predicate) => predicate(info),
            predicate,
            cancellationToken);
    }

    protected static Task<ImmutableArray<Document>> FindDocumentsWithForEachStatementsAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsForEachStatement, cancellationToken);

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

        return [];
    }

    protected Task<ImmutableArray<FinderLocation>> FindReferencesInForEachStatementsAsync(
        ISymbol symbol,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        return FindReferencesInDocumentAsync(state, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

        static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
            => syntaxTreeInfo.ContainsForEachStatement;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations)
        {
            var info = state.SemanticFacts.GetForEachSymbols(state.SemanticModel, node);

            if (Matches(info.GetEnumeratorMethod, symbol) ||
                Matches(info.MoveNextMethod, symbol) ||
                Matches(info.CurrentProperty, symbol) ||
                Matches(info.DisposeMethod, symbol))
            {
                var location = node.GetFirstToken().GetLocation();
                var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                locations.Add(new FinderLocation(node, new ReferenceLocation(
                    state.Document,
                    alias: null,
                    location: location,
                    isImplicit: true,
                    symbolUsageInfo,
                    GetAdditionalFindUsagesProperties(node, state),
                    candidateReason: CandidateReason.None)));
            }
        }
    }

    protected Task<ImmutableArray<FinderLocation>> FindReferencesInCollectionInitializerAsync(
        ISymbol symbol,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        return FindReferencesInDocumentAsync(state, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

        static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
            => syntaxTreeInfo.ContainsCollectionInitializer;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations)
        {
            if (!state.SyntaxFacts.IsObjectCollectionInitializer(node))
                return;

            var expressions = state.SyntaxFacts.GetExpressionsOfObjectCollectionInitializer(node);
            foreach (var expression in expressions)
            {
                var info = state.SemanticFacts.GetCollectionInitializerSymbolInfo(state.SemanticModel, expression, cancellationToken);

                if (Matches(info, symbol))
                {
                    var location = expression.GetFirstToken().GetLocation();
                    var symbolUsageInfo = GetSymbolUsageInfo(expression, state, cancellationToken);

                    locations.Add(new FinderLocation(expression, new ReferenceLocation(
                        state.Document,
                        alias: null,
                        location: location,
                        isImplicit: true,
                        symbolUsageInfo,
                        GetAdditionalFindUsagesProperties(expression, state),
                        candidateReason: CandidateReason.None)));
                }
            }
        }
    }

    protected Task<ImmutableArray<FinderLocation>> FindReferencesInDeconstructionAsync(
        ISymbol symbol,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        return FindReferencesInDocumentAsync(state, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

        static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
            => syntaxTreeInfo.ContainsDeconstruction;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations)
        {
            var semanticModel = state.SemanticModel;
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
                    state.Document, alias: null, location, isImplicit: true, symbolUsageInfo,
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
            var awaitExpressionMethod = state.SemanticFacts.GetGetAwaiterMethod(state.SemanticModel, node);

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
            // Avoid binding unrelated nodes
            if (!state.SyntaxFacts.IsImplicitObjectCreationExpression(node))
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

    protected static bool Matches(SymbolInfo info, ISymbol notNullOriginalUnreducedSymbol2)
    {
        if (Matches(info.Symbol, notNullOriginalUnreducedSymbol2))
            return true;

        foreach (var symbol in info.CandidateSymbols)
        {
            if (Matches(symbol, notNullOriginalUnreducedSymbol2))
                return true;
        }

        return false;
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

        return IsInNamespaceOrTypeContext()
            ? SymbolUsageInfo.Create(GetTypeOrNamespaceUsageInfo())
            : GetSymbolUsageInfoCommon();

        bool IsInNamespaceOrTypeContext()
        {
            var current = node;
            while (syntaxFacts.IsQualifiedName(current.Parent))
                current = current.Parent;

            return syntaxFacts.IsInNamespaceOrTypeContext(current);
        }

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
                node = node.Parent;

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

    private static bool TryGetAdditionalProperty(SyntaxNode? node, string name, SemanticModel semanticModel, out KeyValuePair<string, string> additionalProperty)
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
            : new ValueTask<ImmutableArray<FinderLocation>>([]);
    }

    public sealed override ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        ISymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken)
    {
        if (options.Cascade &&
            symbol is TSymbol typedSymbol &&
            CanFind(typedSymbol))
        {
            return DetermineCascadedSymbolsAsync(typedSymbol, solution, options, cancellationToken);
        }

        return new([]);
    }

    protected virtual ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        TSymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken)
    {
        return new([]);
    }

    protected static ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentUsingSymbolNameAsync(
        TSymbol symbol, FindReferencesDocumentState state, CancellationToken cancellationToken)
    {
        return FindReferencesInDocumentUsingIdentifierAsync(
            symbol, symbol.Name, state, cancellationToken);
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
