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
using Roslyn.Utilities;

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

    protected override async ValueTask FindReferencesInDocumentAsync<TData>(
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
        await AddReferencesToTypeOrGlobalAliasToItAsync(
            namedType, state, StandardCallbacks<FinderLocation>.AddToArrayBuilder, initialReferences, cancellationToken).ConfigureAwait(false);

        // call processResult on items in initialReferences
        foreach (var location in initialReferences)
            processResult(location, processResultData);

        // This named type may end up being locally aliased as well.  If so, now find all the references
        // to the local alias.

        await FindLocalAliasReferencesAsync(
            initialReferences, state, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        await FindPredefinedTypeReferencesAsync(
            namedType, state, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
            namedType, state, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    internal static async ValueTask AddReferencesToTypeOrGlobalAliasToItAsync<TData>(
        INamedTypeSymbol namedType,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        await AddNonAliasReferencesAsync(
            namedType, namedType.Name, state, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        foreach (var globalAlias in state.GlobalAliases)
        {
            // ignore the cases where the global alias might match the type name (i.e.
            // global alias Console = System.Console).  We'll already find those references
            // above.
            if (state.SyntaxFacts.StringComparer.Equals(namedType.Name, globalAlias))
                continue;

            await AddNonAliasReferencesAsync(
                namedType, globalAlias, state, processResult, processResultData, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Finds references to <paramref name="symbol"/> in this <paramref name="state"/>, but
    /// only if it referenced though <paramref name="name"/> (which might be the actual name
    /// of the type, or a global alias to it).
    /// </summary>
    private static async ValueTask AddNonAliasReferencesAsync<TData>(
        INamedTypeSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        await FindOrdinaryReferencesAsync(
            symbol, name, state, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        await FindAttributeReferencesAsync(
            symbol, name, state, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    private static ValueTask FindOrdinaryReferencesAsync<TData>(
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

        return FindReferencesInDocumentUsingIdentifierAsync(
            namedType, name, state, processResult, processResultData, cancellationToken);
    }

    private static ValueTask FindPredefinedTypeReferencesAsync<TData>(
        INamedTypeSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        var predefinedType = symbol.SpecialType.ToPredefinedType();
        if (predefinedType == PredefinedType.None)
            return ValueTaskFactory.CompletedTask;

        var tokens = state.Root
            .DescendantTokens(descendIntoTrivia: true)
            .WhereAsArray(
                static (token, tuple) => IsPotentialReference(tuple.predefinedType, tuple.state.SyntaxFacts, token),
                (state, predefinedType));

        return FindReferencesInTokensAsync(symbol, state, tokens, processResult, processResultData, cancellationToken);
    }

    private static ValueTask FindAttributeReferencesAsync<TData>(
        INamedTypeSymbol namedType,
        string name,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        return TryGetNameWithoutAttributeSuffix(name, state.SyntaxFacts, out var nameWithoutSuffix)
            ? FindReferencesInDocumentUsingIdentifierAsync(namedType, nameWithoutSuffix, state, processResult, processResultData, cancellationToken)
            : ValueTaskFactory.CompletedTask;
    }
}
