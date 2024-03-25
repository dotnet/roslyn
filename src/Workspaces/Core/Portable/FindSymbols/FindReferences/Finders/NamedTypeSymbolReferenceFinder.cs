// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
        INamedTypeSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<Document>.GetInstance(out var result);

        await AddDocumentsToSearchAsync(symbol.Name, project, documents, result, cancellationToken).ConfigureAwait(false);
        if (globalAliases != null)
        {
            foreach (var alias in globalAliases)
                await AddDocumentsToSearchAsync(alias, project, documents, result, cancellationToken).ConfigureAwait(false);
        }

        result.AddRange(await FindDocumentsAsync(
            project, documents, symbol.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false));

        result.AddRange(await FindDocumentsWithGlobalSuppressMessageAttributeAsync(
            project, documents, cancellationToken).ConfigureAwait(false));

        return result.ToImmutable();
    }

    /// <summary>
    /// Looks for documents likely containing <paramref name="throughName"/> in them.  That name will either be the actual
    /// name of the named type we're looking for, or it might be a global alias to it.
    /// </summary>
    private static async Task AddDocumentsToSearchAsync(
        string throughName,
        Project project,
        IImmutableSet<Document>? documents,
        ArrayBuilder<Document> result,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = project.Services.GetRequiredService<ISyntaxFactsService>();

        var documentsWithName = await FindDocumentsAsync(
            project, documents, cancellationToken, throughName).ConfigureAwait(false);

        var documentsWithAttribute = TryGetNameWithoutAttributeSuffix(throughName, syntaxFacts, out var simpleName)
            ? await FindDocumentsAsync(project, documents, cancellationToken, simpleName).ConfigureAwait(false)
            : [];

        result.AddRange(documentsWithName);
        result.AddRange(documentsWithAttribute);
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

    protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
        INamedTypeSymbol namedType,
        FindReferencesDocumentState state,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var initialReferences);

        // First find all references to this type, either with it's actual name, or through potential
        // global alises to it.
        await AddReferencesToTypeOrGlobalAliasToItAsync(
            namedType, state, initialReferences, cancellationToken).ConfigureAwait(false);

        // This named type may end up being locally aliased as well.  If so, now find all the references
        // to the local alias.

        initialReferences.AddRange(await FindLocalAliasReferencesAsync(
            initialReferences, state, cancellationToken).ConfigureAwait(false));

        initialReferences.AddRange(await FindPredefinedTypeReferencesAsync(
            namedType, state, cancellationToken).ConfigureAwait(false));

        initialReferences.AddRange(await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
            namedType, state, cancellationToken).ConfigureAwait(false));

        return initialReferences.ToImmutable();
    }

    internal static async ValueTask AddReferencesToTypeOrGlobalAliasToItAsync(
        INamedTypeSymbol namedType,
        FindReferencesDocumentState state,
        ArrayBuilder<FinderLocation> nonAliasReferences,
        CancellationToken cancellationToken)
    {
        await AddNonAliasReferencesAsync(
            namedType, namedType.Name, state, nonAliasReferences, cancellationToken).ConfigureAwait(false);

        foreach (var globalAlias in state.GlobalAliases)
        {
            // ignore the cases where the global alias might match the type name (i.e.
            // global alias Console = System.Console).  We'll already find those references
            // above.
            if (state.SyntaxFacts.StringComparer.Equals(namedType.Name, globalAlias))
                continue;

            await AddNonAliasReferencesAsync(
                namedType, globalAlias, state, nonAliasReferences, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Finds references to <paramref name="symbol"/> in this <paramref name="state"/>, but
    /// only if it referenced though <paramref name="name"/> (which might be the actual name
    /// of the type, or a global alias to it).
    /// </summary>
    private static async ValueTask AddNonAliasReferencesAsync(
        INamedTypeSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        ArrayBuilder<FinderLocation> nonAliasesReferences,
        CancellationToken cancellationToken)
    {
        nonAliasesReferences.AddRange(await FindOrdinaryReferencesAsync(
            symbol, name, state, cancellationToken).ConfigureAwait(false));

        nonAliasesReferences.AddRange(await FindAttributeReferencesAsync(
            symbol, name, state, cancellationToken).ConfigureAwait(false));
    }

    private static ValueTask<ImmutableArray<FinderLocation>> FindOrdinaryReferencesAsync(
        INamedTypeSymbol namedType,
        string name,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        // Get the parent node that best matches what this token represents.  For example, if we have `new a.b()`
        // then the parent node of `b` won't be `a.b`, but rather `new a.b()`.  This will actually cause us to bind
        // to the constructor not the type.  That's a good thing as we don't want these object-creations to
        // associate with the type, but rather with the constructor itself.

        return FindReferencesInDocumentUsingIdentifierAsync(
            namedType, name, state, cancellationToken);
    }

    private static ValueTask<ImmutableArray<FinderLocation>> FindPredefinedTypeReferencesAsync(
        INamedTypeSymbol symbol,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        var predefinedType = symbol.SpecialType.ToPredefinedType();
        if (predefinedType == PredefinedType.None)
            return new([]);

        var tokens = state.Root
            .DescendantTokens(descendIntoTrivia: true)
            .WhereAsArray(
                static (token, tuple) => IsPotentialReference(tuple.predefinedType, tuple.state.SyntaxFacts, token),
                (state, predefinedType));

        return FindReferencesInTokensAsync(symbol, state, tokens, cancellationToken);
    }

    private static ValueTask<ImmutableArray<FinderLocation>> FindAttributeReferencesAsync(
        INamedTypeSymbol namedType,
        string name,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        return TryGetNameWithoutAttributeSuffix(name, state.SyntaxFacts, out var nameWithoutSuffix)
            ? FindReferencesInDocumentUsingIdentifierAsync(namedType, nameWithoutSuffix, state, cancellationToken)
            : new([]);
    }
}
