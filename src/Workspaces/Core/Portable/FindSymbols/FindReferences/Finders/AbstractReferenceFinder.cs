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
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
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

    public abstract Task DetermineDocumentsToSearchAsync<TData>(
        ISymbol symbol, HashSet<string>? globalAliases, Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, FindReferencesSearchOptions options, CancellationToken cancellationToken);

    public abstract void FindReferencesInDocument<TData>(
        ISymbol symbol, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData, FindReferencesSearchOptions options, CancellationToken cancellationToken);

    private static (bool matched, CandidateReason reason) SymbolsMatch(
        ISymbol symbol, FindReferencesDocumentState state, SyntaxToken token, CancellationToken cancellationToken)
    {
        // delegates don't have exposed symbols for their constructors.  so when you do `new MyDel()`, that's only a
        // reference to a type (as we don't have any real constructor symbols that can actually cascade to).  So
        // don't do any special finding in that case.
        var parent = symbol.IsDelegateType()
            ? token.Parent
            : state.SyntaxFacts.TryGetBindableParent(token);
        parent ??= token.Parent!;

        return SymbolsMatch(symbol, state, parent, cancellationToken);
    }

    protected static (bool matched, CandidateReason reason) SymbolsMatch(
        ISymbol searchSymbol, FindReferencesDocumentState state, SyntaxNode node, CancellationToken cancellationToken)
    {
        var symbolInfo = state.Cache.GetSymbolInfo(node, cancellationToken);
        return Matches(searchSymbol, state, symbolInfo);
    }

    protected static (bool matched, CandidateReason reason) Matches(
        ISymbol searchSymbol, FindReferencesDocumentState state, SymbolInfo symbolInfo)
    {
        if (SymbolFinder.OriginalSymbolsMatch(state.Solution, searchSymbol, symbolInfo.Symbol))
            return (matched: true, CandidateReason.None);

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (SymbolFinder.OriginalSymbolsMatch(state.Solution, searchSymbol, candidate))
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

    protected static async Task FindDocumentsAsync<T, TData>(
        Project project,
        IImmutableSet<Document>? scope,
        Func<Document, T, CancellationToken, ValueTask<bool>> predicateAsync,
        T value,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        // special case for highlight references
        if (scope != null && scope.Count == 1)
        {
            var document = scope.First();
            if (document.Project == project)
                processResult(document, processResultData);

            return;
        }

        await foreach (var document in project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (scope != null && !scope.Contains(document))
                continue;

            if (await predicateAsync(document, value, cancellationToken).ConfigureAwait(false))
                processResult(document, processResultData);
        }
    }

    /// <summary>
    /// Finds all the documents in the provided project that contain the requested string
    /// values
    /// </summary>
    protected static Task FindDocumentsAsync<TData>(
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken,
        params ImmutableArray<string> values)
    {
        return FindDocumentsWithPredicateAsync(project, documents, static (index, values) =>
        {
            foreach (var value in values)
            {
                if (!index.ProbablyContainsIdentifier(value))
                    return false;
            }

            return true;
        }, values, processResult, processResultData, cancellationToken);
    }

    /// <summary>
    /// Finds all the documents in the provided project that contain a global attribute in them.
    /// </summary>
    protected static Task FindDocumentsWithGlobalSuppressMessageAttributeAsync<TData>(
        Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
    {
        return FindDocumentsWithPredicateAsync(
            project, documents, static index => index.ContainsGlobalSuppressMessageAttribute, processResult, processResultData, cancellationToken);
    }

    protected static async Task FindDocumentsAsync<TData>(
        Project project,
        IImmutableSet<Document>? documents,
        PredefinedType predefinedType,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        if (predefinedType == PredefinedType.None)
            return;

        await FindDocumentsWithPredicateAsync(
            project, documents, static (index, predefinedType) => index.ContainsPredefinedType(predefinedType), predefinedType, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    protected static bool IdentifiersMatch(ISyntaxFactsService syntaxFacts, string name, SyntaxToken token)
        => syntaxFacts.IsIdentifier(token) && syntaxFacts.TextMatch(token.ValueText, name);

    protected static void FindReferencesInDocumentUsingIdentifier<TData>(
        ISymbol symbol,
        string identifier,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var tokens = FindMatchingIdentifierTokens(state, identifier, cancellationToken);
        FindReferencesInTokens(symbol, state, tokens, processResult, processResultData, cancellationToken);
    }

    public static ImmutableArray<SyntaxToken> FindMatchingIdentifierTokens(FindReferencesDocumentState state, string identifier, CancellationToken cancellationToken)
        => state.Cache.FindMatchingIdentifierTokens(identifier, cancellationToken);

    protected static void FindReferencesInTokens<TData>(
        ISymbol symbol,
        FindReferencesDocumentState state,
        ImmutableArray<SyntaxToken> tokens,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        if (tokens.IsEmpty)
            return;

        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (matched, reason) = SymbolsMatch(symbol, state, token, cancellationToken);
            if (matched)
            {
                var finderLocation = CreateFinderLocation(state, token, reason, cancellationToken);
                processResult(finderLocation, processResultData);
            }
        }
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

    protected static void FindLocalAliasReferences<TData>(
        ArrayBuilder<FinderLocation> initialReferences,
        ISymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var aliasSymbols = GetLocalAliasSymbols(state, initialReferences, cancellationToken);
        if (!aliasSymbols.IsDefaultOrEmpty)
            FindReferencesThroughLocalAliasSymbols(symbol, state, aliasSymbols, processResult, processResultData, cancellationToken);
    }

    protected static void FindLocalAliasReferences<TData>(
        ArrayBuilder<FinderLocation> initialReferences,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var aliasSymbols = GetLocalAliasSymbols(state, initialReferences, cancellationToken);
        if (!aliasSymbols.IsDefaultOrEmpty)
            FindReferencesThroughLocalAliasSymbols(state, aliasSymbols, processResult, processResultData, cancellationToken);
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

    private static void FindReferencesThroughLocalAliasSymbols<TData>(
        ISymbol symbol,
        FindReferencesDocumentState state,
        ImmutableArray<IAliasSymbol> localAliasSymbols,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        foreach (var localAliasSymbol in localAliasSymbols)
        {
            FindReferencesInDocumentUsingIdentifier(
                symbol, localAliasSymbol.Name, state, processResult, processResultData, cancellationToken);

            // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
            // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
            if (TryGetNameWithoutAttributeSuffix(localAliasSymbol.Name, state.SyntaxFacts, out var simpleName))
            {
                FindReferencesInDocumentUsingIdentifier(
                    symbol, simpleName, state, processResult, processResultData, cancellationToken);
            }
        }
    }

    private static void FindReferencesThroughLocalAliasSymbols<TData>(
        FindReferencesDocumentState state,
        ImmutableArray<IAliasSymbol> localAliasSymbols,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        foreach (var aliasSymbol in localAliasSymbols)
        {
            FindReferencesInDocumentUsingIdentifier(
                aliasSymbol, aliasSymbol.Name, state, processResult, processResultData, cancellationToken);

            // the alias may reference an attribute and the alias name may end with an "Attribute" suffix. In this case search for the
            // shortened name as well (e.g. using GooAttribute = MyNamespace.GooAttribute; [Goo] class C1 {})
            if (TryGetNameWithoutAttributeSuffix(aliasSymbol.Name, state.SyntaxFacts, out var simpleName))
            {
                FindReferencesInDocumentUsingIdentifier(
                    aliasSymbol, simpleName, state, processResult, processResultData, cancellationToken);
            }
        }
    }

    protected static Task FindDocumentsWithPredicateAsync<T, TData>(
        Project project,
        IImmutableSet<Document>? documents,
        Func<SyntaxTreeIndex, T, bool> predicate,
        T value,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        return FindDocumentsAsync(project, documents, static async (d, t, c) =>
        {
            var info = await SyntaxTreeIndex.GetRequiredIndexAsync(d, c).ConfigureAwait(false);
            return t.predicate(info, t.value);
        }, (predicate, value), processResult, processResultData, cancellationToken);
    }

    protected static Task FindDocumentsWithPredicateAsync<TData>(
        Project project,
        IImmutableSet<Document>? documents,
        Func<SyntaxTreeIndex, bool> predicate,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        return FindDocumentsWithPredicateAsync(
            project, documents,
            static (info, predicate) => predicate(info),
            predicate,
            processResult,
            processResultData,
            cancellationToken);
    }

    protected static Task FindDocumentsWithForEachStatementsAsync<TData>(Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsForEachStatement, processResult, processResultData, cancellationToken);

    protected static Task FindDocumentsWithUsingStatementsAsync<TData>(Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsUsingStatement, processResult, processResultData, cancellationToken);

    protected static Task FindDocumentsWithCollectionExpressionsAsync<TData>(Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsCollectionExpression, processResult, processResultData, cancellationToken);

    /// <summary>
    /// If the `node` implicitly matches the `symbol`, then it will be added to `locations`.
    /// </summary>
    protected delegate void CollectMatchingReferences<TData>(
        SyntaxNode node, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData);

    protected static void FindReferencesInDocument<TData>(
        FindReferencesDocumentState state,
        Func<SyntaxTreeIndex, bool> isRelevantDocument,
        CollectMatchingReferences<TData> collectMatchingReferences,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var syntaxTreeInfo = state.Cache.SyntaxTreeIndex;
        if (isRelevantDocument(syntaxTreeInfo))
        {
            foreach (var node in state.Root.DescendantNodesAndSelf())
            {
                cancellationToken.ThrowIfCancellationRequested();
                collectMatchingReferences(node, state, processResult, processResultData);
            }
        }
    }

    protected void FindReferencesInForEachStatements<TData>(
        ISymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocument(state, static index => index.ContainsForEachStatement, CollectMatchingReferences, processResult, processResultData, cancellationToken);
        return;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData)
        {
            var info = state.SemanticFacts.GetForEachSymbols(state.SemanticModel, node);

            if (Matches(info.GetEnumeratorMethod, symbol) ||
                Matches(info.MoveNextMethod, symbol) ||
                Matches(info.CurrentProperty, symbol) ||
                Matches(info.DisposeMethod, symbol))
            {
                var location = node.GetFirstToken().GetLocation();
                var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                var result = new FinderLocation(node, new ReferenceLocation(
                    state.Document,
                    alias: null,
                    location: location,
                    isImplicit: true,
                    symbolUsageInfo,
                    GetAdditionalFindUsagesProperties(node, state),
                    candidateReason: CandidateReason.None));
                processResult(result, processResultData);
            }
        }
    }

    protected void FindReferencesInCollectionInitializer<TData>(
        ISymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocument(state, IsRelevantDocument, CollectMatchingReferences, processResult, processResultData, cancellationToken);
        return;

        static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
            => syntaxTreeInfo.ContainsCollectionInitializer;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData)
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

                    var result = new FinderLocation(expression, new ReferenceLocation(
                        state.Document,
                        alias: null,
                        location: location,
                        isImplicit: true,
                        symbolUsageInfo,
                        GetAdditionalFindUsagesProperties(expression, state),
                        candidateReason: CandidateReason.None));
                    processResult(result, processResultData);
                }
            }
        }
    }

    protected void FindReferencesInCollectionExpressions<TData>(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocument(state, static index => index.ContainsCollectionExpression, CollectMatchingReferences, processResult, processResultData, cancellationToken);

        void CollectMatchingReferences(
            SyntaxNode node,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData)
        {
            if (!state.SyntaxFacts.IsCollectionExpression(node))
                return;

            if (state.SemanticModel.GetOperation(node, cancellationToken) is not ICollectionExpressionOperation collectionExpression)
                return;

            if (!Equals(symbol, collectionExpression.ConstructMethod?.OriginalDefinition))
                return;

            var result = new FinderLocation(node, new ReferenceLocation(
                state.Document,
                alias: null,
                location: node.GetLocation(),
                isImplicit: true,
                GetSymbolUsageInfo(node, state, cancellationToken),
                GetAdditionalFindUsagesProperties(node, state),
                candidateReason: CandidateReason.None));
            processResult(result, processResultData);
        }
    }

    protected void FindReferencesInDeconstruction<TData>(
        ISymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocument(state, IsRelevantDocument, CollectMatchingReferences, processResult, processResultData, cancellationToken);
        return;

        static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
            => syntaxTreeInfo.ContainsDeconstruction;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData)
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

                var result = new FinderLocation(node, new ReferenceLocation(
                    state.Document, alias: null, location, isImplicit: true, symbolUsageInfo,
                    GetAdditionalFindUsagesProperties(node, state), CandidateReason.None));
                processResult(result, processResultData);
            }
        }
    }

    protected void FindReferencesInAwaitExpression<TData>(
        ISymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocument(state, IsRelevantDocument, CollectMatchingReferences, processResult, processResultData, cancellationToken);
        return;

        static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
            => syntaxTreeInfo.ContainsAwait;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData)
        {
            var awaitExpressionMethod = state.SemanticFacts.GetGetAwaiterMethod(state.SemanticModel, node);

            if (Matches(awaitExpressionMethod, symbol))
            {
                var location = node.GetFirstToken().GetLocation();
                var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                var result = new FinderLocation(node, new ReferenceLocation(
                    state.Document, alias: null, location, isImplicit: true, symbolUsageInfo,
                    GetAdditionalFindUsagesProperties(node, state), CandidateReason.None));
                processResult(result, processResultData);
            }
        }
    }

    protected void FindReferencesInImplicitObjectCreationExpression<TData>(
        ISymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocument(state, IsRelevantDocument, CollectMatchingReferences, processResult, processResultData, cancellationToken);
        return;

        static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
            => syntaxTreeInfo.ContainsImplicitObjectCreation;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData)
        {
            // Avoid binding unrelated nodes
            if (!state.SyntaxFacts.IsImplicitObjectCreationExpression(node))
                return;

            var constructor = state.SemanticModel.GetSymbolInfo(node, cancellationToken).Symbol;

            if (Matches(constructor, symbol))
            {
                var location = node.GetFirstToken().GetLocation();
                var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

                var result = new FinderLocation(node, new ReferenceLocation(
                    state.Document, alias: null, location, isImplicit: true, symbolUsageInfo,
                    GetAdditionalFindUsagesProperties(node, state), CandidateReason.None));
                processResult(result, processResultData);
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
        return SymbolUsageInfo.GetSymbolUsageInfo(
            state.SemanticFacts, state.SemanticModel, node, cancellationToken);
    }

    internal static ImmutableArray<(string key, string value)> GetAdditionalFindUsagesProperties(
        SyntaxNode node, FindReferencesDocumentState state)
    {
        using var additionalProperties = TemporaryArray<(string key, string value)>.Empty;

        var syntaxFacts = state.SyntaxFacts;
        var semanticModel = state.SemanticModel;

        TryAddAdditionalProperty(
            syntaxFacts.GetContainingTypeDeclaration(node, node.SpanStart),
            ContainingTypeInfoPropertyName);

        TryAddAdditionalProperty(
            syntaxFacts.GetContainingMemberDeclaration(node, node.SpanStart),
            ContainingMemberInfoPropertyName);

        return additionalProperties.ToImmutableAndClear();

        void TryAddAdditionalProperty(SyntaxNode? node, string key)
        {
            if (node != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(node);
                if (symbol != null)
                    additionalProperties.Add((key, symbol.Name));
            }
        }
    }

    internal static ImmutableArray<(string key, string value)> GetAdditionalFindUsagesProperties(ISymbol definition)
    {
        using var additionalProperties = TemporaryArray<(string key, string value)>.Empty;

        var containingType = definition.ContainingType;
        if (containingType != null)
            additionalProperties.Add((ContainingTypeInfoPropertyName, containingType.Name));

        // Containing member should only include fields, properties, methods, or events.  Since ContainingSymbol can
        // return other types, use the return value of GetMemberType to restrict to members only.)
        var containingSymbol = definition.ContainingSymbol;
        if (containingSymbol != null && containingSymbol.GetMemberType() != null)
            additionalProperties.Add((ContainingMemberInfoPropertyName, containingSymbol.Name));

        return additionalProperties.ToImmutableAndClear();
    }
}

internal abstract partial class AbstractReferenceFinder<TSymbol> : AbstractReferenceFinder
    where TSymbol : ISymbol
{
    protected abstract bool CanFind(TSymbol symbol);

    protected abstract Task DetermineDocumentsToSearchAsync<TData>(
        TSymbol symbol, HashSet<string>? globalAliases, Project project, IImmutableSet<Document>? documents,
        Action<Document, TData> processResult, TData processResultData,
        FindReferencesSearchOptions options, CancellationToken cancellationToken);

    protected abstract void FindReferencesInDocument<TData>(
        TSymbol symbol, FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult, TData processResultData,
        FindReferencesSearchOptions options, CancellationToken cancellationToken);

    protected virtual async Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(
        TSymbol symbol, Project project, CancellationToken cancellationToken)
    {
        return [];
    }

    public sealed override Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(
        ISymbol symbol, Project project, CancellationToken cancellationToken)
    {
        return symbol is TSymbol typedSymbol && CanFind(typedSymbol)
            ? DetermineGlobalAliasesAsync(typedSymbol, project, cancellationToken)
            : SpecializedTasks.EmptyImmutableArray<string>();
    }

    public sealed override async Task DetermineDocumentsToSearchAsync<TData>(
        ISymbol symbol, HashSet<string>? globalAliases, Project project,
        IImmutableSet<Document>? documents, Action<Document, TData> processResult,
        TData processResultData, FindReferencesSearchOptions options, CancellationToken cancellationToken)
    {
        if (symbol is TSymbol typedSymbol && CanFind(typedSymbol))
            await DetermineDocumentsToSearchAsync(typedSymbol, globalAliases, project, documents, processResult, processResultData, options, cancellationToken).ConfigureAwait(false);
    }

    public sealed override void FindReferencesInDocument<TData>(
        ISymbol symbol, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData, FindReferencesSearchOptions options, CancellationToken cancellationToken)
    {
        if (symbol is TSymbol typedSymbol && CanFind(typedSymbol))
            FindReferencesInDocument(typedSymbol, state, processResult, processResultData, options, cancellationToken);
    }

    public sealed override async ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        ISymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken)
    {
        if (options.Cascade &&
            symbol is TSymbol typedSymbol &&
            CanFind(typedSymbol))
        {
            return await DetermineCascadedSymbolsAsync(typedSymbol, solution, options, cancellationToken).ConfigureAwait(false);
        }

        return [];
    }

    protected virtual async ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        TSymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken)
    {
        return [];
    }

    protected static void FindReferencesInDocumentUsingSymbolName<TData>(
        TSymbol symbol, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData, CancellationToken cancellationToken)
    {
        FindReferencesInDocumentUsingIdentifier(
            symbol, symbol.Name, state, processResult, processResultData, cancellationToken);
    }

    protected static async Task<ImmutableArray<string>> GetAllMatchingGlobalAliasNamesAsync(
        Project project, string name, int arity, CancellationToken cancellationToken)
    {
        using var result = TemporaryArray<string>.Empty;

        await foreach (var document in project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
        {
            var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
            foreach (var alias in index.GetGlobalAliases(name, arity))
                result.Add(alias);
        }

        return result.ToImmutableAndClear();
    }
}
