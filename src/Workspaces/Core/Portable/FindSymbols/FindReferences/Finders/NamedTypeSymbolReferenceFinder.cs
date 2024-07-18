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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class NamedTypeSymbolReferenceFinder : AbstractReferenceFinder<INamedTypeSymbol>
{
    protected override bool CanFind(INamedTypeSymbol symbol)
        => symbol.TypeKind != TypeKind.Error;

    protected override Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(INamedTypeSymbol symbol, Project project, CancellationToken cancellationToken)
    {
        return GetAllMatchingGlobalAliasNamesAsync(project, symbol.Name, symbol.Arity, cancellationToken);
    }

    protected override ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        INamedTypeSymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);

        if (symbol.AssociatedSymbol != null)
            Add(result, ImmutableArray.Create(symbol.AssociatedSymbol));

        // cascade to constructors
        Add(result, symbol.Constructors);

        // cascade to destructor
        Add(result, symbol.GetMembers(WellKnownMemberNames.DestructorName));

        return new(result.ToImmutable());
    }

    private static void Add<TSymbol>(ArrayBuilder<ISymbol> result, ImmutableArray<TSymbol> enumerable) where TSymbol : ISymbol
    {
        result.AddRange(enumerable.Cast<ISymbol>());
    }

    protected override async Task DetermineDocumentsToSearchAsync<TData>(
        INamedTypeSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        await AddDocumentsToSearchAsync(symbol.Name, project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
        if (globalAliases != null)
        {
            foreach (var alias in globalAliases)
                await AddDocumentsToSearchAsync(alias, project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
        }

        await FindDocumentsAsync(
            project, documents, symbol.SpecialType.ToPredefinedType(), processResult, processResultData, cancellationToken).ConfigureAwait(false);

        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(
            project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Looks for documents likely containing <paramref name="throughName"/> in them.  That name will either be the actual
    /// name of the named type we're looking for, or it might be a global alias to it.
    /// </summary>
    private static async Task AddDocumentsToSearchAsync<TData>(
        string throughName,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = project.Services.GetRequiredService<ISyntaxFactsService>();

        await FindDocumentsAsync(
            project, documents, processResult, processResultData, cancellationToken, throughName).ConfigureAwait(false);

        if (TryGetNameWithoutAttributeSuffix(throughName, syntaxFacts, out var simpleName))
            await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, simpleName).ConfigureAwait(false);
    }

    private static bool IsPotentialReference(
        PredefinedType predefinedType,
        ISyntaxFactsService syntaxFacts,
        SyntaxToken token)
    {
        return
            syntaxFacts.TryGetPredefinedType(token, out var actualType) &&
            predefinedType == actualType;
    }

    protected override void FindReferencesInDocument<TData>(
        INamedTypeSymbol namedType,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var initialReferences);

        // First find all references to this type, either with it's actual name, or through potential
        // global alises to it.
        AddReferencesToTypeOrGlobalAliasToIt(
            namedType, state, StandardCallbacks<FinderLocation>.AddToArrayBuilder, initialReferences, cancellationToken);

        // The items in initialReferences need to be both reported and used later to calculate additional results.
        foreach (var location in initialReferences)
            processResult(location, processResultData);

        // This named type may end up being locally aliased as well.  If so, now find all the references
        // to the local alias.

        FindLocalAliasReferences(
            initialReferences, state, processResult, processResultData, cancellationToken);

        FindPredefinedTypeReferences(
            namedType, state, processResult, processResultData, cancellationToken);

        FindReferencesInDocumentInsideGlobalSuppressions(
            namedType, state, processResult, processResultData, cancellationToken);
    }

    internal static void AddReferencesToTypeOrGlobalAliasToIt<TData>(
        INamedTypeSymbol namedType,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        AddNonAliasReferences(
            namedType, namedType.Name, state, processResult, processResultData, cancellationToken);

        foreach (var globalAlias in state.GlobalAliases)
            FindReferenceToAlias(namedType, state, processResult, processResultData, globalAlias, cancellationToken);

        foreach (var localAlias in state.Cache.SyntaxTreeIndex.GetAliases(namedType.Name, namedType.Arity))
            FindReferenceToAlias(namedType, state, processResult, processResultData, localAlias, cancellationToken);
    }

    private static void FindReferenceToAlias<TData>(
        INamedTypeSymbol namedType, FindReferencesDocumentState state, Action<FinderLocation, TData> processResult, TData processResultData, string alias, CancellationToken cancellationToken)
    {
        // ignore the cases where the global alias might match the type name (i.e.
        // global alias Console = System.Console).  We'll already find those references
        // above.
        if (state.SyntaxFacts.StringComparer.Equals(namedType.Name, alias))
            return;

        AddNonAliasReferences(
            namedType, alias, state, processResult, processResultData, cancellationToken);
    }

    /// <summary>
    /// Finds references to <paramref name="symbol"/> in this <paramref name="state"/>, but
    /// only if it referenced though <paramref name="name"/> (which might be the actual name
    /// of the type, or a global alias to it).
    /// </summary>
    private static void AddNonAliasReferences<TData>(
        INamedTypeSymbol symbol,
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
        INamedTypeSymbol namedType,
        string name,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        // Get the parent node that best matches what this token represents.  For example, if we have `new a.b()`
        // then the parent node of `b` won't be `a.b`, but rather `new a.b()`.  This will actually cause us to bind
        // to the constructor not the type.  That's a good thing as we don't want these object-creations to
        // associate with the type, but rather with the constructor itself.

        FindReferencesInDocumentUsingIdentifier(
            namedType, name, state, processResult, processResultData, cancellationToken);
    }

    private static void FindPredefinedTypeReferences<TData>(
        INamedTypeSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var predefinedType = symbol.SpecialType.ToPredefinedType();
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
        INamedTypeSymbol namedType,
        string name,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        if (TryGetNameWithoutAttributeSuffix(name, state.SyntaxFacts, out var nameWithoutSuffix))
            FindReferencesInDocumentUsingIdentifier(namedType, nameWithoutSuffix, state, processResult, processResultData, cancellationToken);
    }
}
