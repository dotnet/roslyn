// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class ConstructorSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
{
    public static readonly ConstructorSymbolReferenceFinder Instance = new();

    private ConstructorSymbolReferenceFinder()
    {
    }

    protected override bool CanFind(IMethodSymbol symbol)
        => symbol.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor;

    protected override Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(IMethodSymbol symbol, Project project, CancellationToken cancellationToken)
    {
        var containingType = symbol.ContainingType;
        return GetAllMatchingGlobalAliasNamesAsync(project, containingType.Name, containingType.Arity, cancellationToken);
    }

    protected override async Task DetermineDocumentsToSearchAsync<TData>(
        IMethodSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var containingType = symbol.ContainingType;
        var typeName = symbol.ContainingType.Name;

        await AddDocumentsAsync(
            project, documents, typeName, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        if (globalAliases != null)
        {
            foreach (var globalAlias in globalAliases)
            {
                await AddDocumentsAsync(
                    project, documents, globalAlias, processResult, processResultData, cancellationToken).ConfigureAwait(false);
            }
        }

        await FindDocumentsAsync(
            project, documents, containingType.SpecialType.ToPredefinedType(), processResult, processResultData, cancellationToken).ConfigureAwait(false);

        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(
            project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        if (symbol.MethodKind == MethodKind.Constructor)
        {
            await FindDocumentsWithImplicitObjectCreationExpressionAsync(
                project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Task FindDocumentsWithImplicitObjectCreationExpressionAsync<TData>(Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsImplicitObjectCreation, processResult, processResultData, cancellationToken);

    private static async Task AddDocumentsAsync<TData>(
        Project project,
        IImmutableSet<Document>? documents,
        string typeName,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, typeName).ConfigureAwait(false);

        if (TryGetNameWithoutAttributeSuffix(typeName, project.Services.GetRequiredService<ISyntaxFactsService>(), out var simpleName))
            await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, simpleName).ConfigureAwait(false);
    }

    private static bool IsPotentialReference(PredefinedType predefinedType, ISyntaxFactsService syntaxFacts, SyntaxToken token)
        => syntaxFacts.TryGetPredefinedType(token, out var actualType) &&
           predefinedType == actualType;

    protected override void FindReferencesInDocument<TData>(
        IMethodSymbol methodSymbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // First just look for this normal constructor references using the name of it's containing type.
        var containingType = methodSymbol.ContainingType;
        var containingTypeName = containingType.Name;
        AddReferencesInDocumentWorker(
            methodSymbol, containingTypeName, state, processResult, processResultData, cancellationToken);

        // Next, look for constructor references through a global alias to our containing type.
        foreach (var globalAlias in state.GlobalAliases)
            FindReferenceToAlias(methodSymbol, state, processResult, processResultData, containingTypeName, globalAlias, cancellationToken);

        foreach (var localAlias in state.Cache.SyntaxTreeIndex.GetAliases(containingTypeName, containingType.Arity))
            FindReferenceToAlias(methodSymbol, state, processResult, processResultData, containingTypeName, localAlias, cancellationToken);

        // Finally, look for constructor references to predefined types (like `new int()`),
        // implicit object references, and inside global suppression attributes.
        FindPredefinedTypeReferences(
            methodSymbol, state, processResult, processResultData, cancellationToken);

        FindReferencesInImplicitObjectCreationExpression(
            methodSymbol, state, processResult, processResultData, cancellationToken);

        FindReferencesInDocumentInsideGlobalSuppressions(
            methodSymbol, state, processResult, processResultData, cancellationToken);
    }

    private static void FindReferenceToAlias<TData>(
        IMethodSymbol methodSymbol, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData, string name, string alias, CancellationToken cancellationToken)
    {
        // ignore the cases where the global alias might match the type name (i.e.
        // global alias Console = System.Console).  We'll already find those references
        // above.
        if (state.SyntaxFacts.StringComparer.Equals(name, alias))
            return;

        AddReferencesInDocumentWorker(
            methodSymbol, alias, state, processResult, processResultData, cancellationToken);
    }

    /// <summary>
    /// Finds references to <paramref name="symbol"/> in this <paramref name="state"/>, but only if it referenced
    /// though <paramref name="name"/> (which might be the actual name of the type, or a global alias to it).
    /// </summary>
    private static void AddReferencesInDocumentWorker<TData>(
        IMethodSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindOrdinaryReferences(
            symbol, name, state, processResult, processResultData, cancellationToken);
        FindAttributeReferences(
            symbol, name, state, processResult, processResultData, cancellationToken);
    }

    private static void FindOrdinaryReferences<TData>(
        IMethodSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocumentUsingIdentifier(
            symbol, name, state, processResult, processResultData, cancellationToken);
    }

    private static void FindPredefinedTypeReferences<TData>(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var predefinedType = symbol.ContainingType.SpecialType.ToPredefinedType();
        if (predefinedType == PredefinedType.None)
            return;

        var tokens = state.Root
            .DescendantTokens(descendIntoTrivia: true)
            .WhereAsArray(
                static (token, tuple) => IsPotentialReference(tuple.predefinedType, tuple.state.SyntaxFacts, token),
                (state, predefinedType));

        FindReferencesInTokens(symbol, state, tokens, processResult, processResultData, cancellationToken);
    }

    private static void FindAttributeReferences<TData>(
        IMethodSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        if (TryGetNameWithoutAttributeSuffix(name, state.SyntaxFacts, out var simpleName))
            FindReferencesInDocumentUsingIdentifier(symbol, simpleName, state, processResult, processResultData, cancellationToken);
    }

    private static void FindReferencesInImplicitObjectCreationExpression<TData>(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        if (!state.Cache.SyntaxTreeIndex.ContainsImplicitObjectCreation)
            return;

        // Note: only C# supports implicit object creation.  So we don't need to do anything special around case
        // insensitive matching here.
        //
        // Search for all the `new` tokens in the file.
        var newKeywordTokens = state.Cache.GetNewKeywordTokens(cancellationToken);
        if (newKeywordTokens.IsEmpty)
            return;

        // Only check `new (...)` calls that supply enough arguments to match all the required parameters for the constructor.
        var minimumArgumentCount = symbol.Parameters.Count(static p => !p.IsOptional && !p.IsParams);
        var maximumArgumentCount = symbol.Parameters is [.., { IsParams: true }]
            ? int.MaxValue
            : symbol.Parameters.Length;

        var exactArgumentCount = symbol.Parameters.Any(static p => p.IsOptional || p.IsParams)
            ? -1
            : symbol.Parameters.Length;

        var implicitObjectKind = state.SyntaxFacts.SyntaxKinds.ImplicitObjectCreationExpression;
        foreach (var newKeywordToken in newKeywordTokens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = newKeywordToken.Parent;
            if (node is null || node.RawKind != implicitObjectKind)
                continue;

            // if there are too few or too many arguments, then don't bother checking.
            var actualArgumentCount = state.SyntaxFacts.GetArgumentsOfObjectCreationExpression(node).Count;
            if (actualArgumentCount < minimumArgumentCount || actualArgumentCount > maximumArgumentCount)
                continue;

            // if we need an exact count then make sure that the count we have fits the count we need.
            if (exactArgumentCount != -1 && exactArgumentCount != actualArgumentCount)
                continue;

            var constructor = state.SemanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
            if (Matches(constructor, symbol))
            {
                var result = new FinderLocation(node, new ReferenceLocation(
                    state.Document,
                    alias: null,
                    newKeywordToken.GetLocation(),
                    isImplicit: true,
                    GetSymbolUsageInfo(node, state, cancellationToken),
                    GetAdditionalFindUsagesProperties(node, state), CandidateReason.None));
                processResult(result, processResultData);
            }
        }
    }
}
