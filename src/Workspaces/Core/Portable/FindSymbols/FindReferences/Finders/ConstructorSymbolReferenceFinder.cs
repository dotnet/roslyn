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

internal class ConstructorSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
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

    protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
        IMethodSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var containingType = symbol.ContainingType;
        var typeName = symbol.ContainingType.Name;

        using var _ = ArrayBuilder<Document>.GetInstance(out var result);

        await AddDocumentsAsync(
            project, documents, typeName, result, cancellationToken).ConfigureAwait(false);

        if (globalAliases != null)
        {
            foreach (var globalAlias in globalAliases)
            {
                await AddDocumentsAsync(
                    project, documents, globalAlias, result, cancellationToken).ConfigureAwait(false);
            }
        }

        result.AddRange(await FindDocumentsAsync(
            project, documents, containingType.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false));

        result.AddRange(await FindDocumentsWithGlobalSuppressMessageAttributeAsync(
            project, documents, cancellationToken).ConfigureAwait(false));

        result.AddRange(symbol.MethodKind == MethodKind.Constructor
            ? await FindDocumentsWithImplicitObjectCreationExpressionAsync(project, documents, cancellationToken).ConfigureAwait(false)
            : []);

        return result.ToImmutable();
    }

    private static Task<ImmutableArray<Document>> FindDocumentsWithImplicitObjectCreationExpressionAsync(Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
        => FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsImplicitObjectCreation, cancellationToken);

    private static async Task AddDocumentsAsync(
        Project project,
        IImmutableSet<Document>? documents,
        string typeName,
        ArrayBuilder<Document> result,
        CancellationToken cancellationToken)
    {
        var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, typeName).ConfigureAwait(false);

        var documentsWithAttribute = TryGetNameWithoutAttributeSuffix(typeName, project.Services.GetRequiredService<ISyntaxFactsService>(), out var simpleName)
            ? await FindDocumentsAsync(project, documents, cancellationToken, simpleName).ConfigureAwait(false)
            : [];

        result.AddRange(documentsWithName);
        result.AddRange(documentsWithAttribute);
    }

    private static bool IsPotentialReference(PredefinedType predefinedType, ISyntaxFactsService syntaxFacts, SyntaxToken token)
        => syntaxFacts.TryGetPredefinedType(token, out var actualType) &&
           predefinedType == actualType;

    protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
        IMethodSymbol methodSymbol,
        FindReferencesDocumentState state,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _1 = ArrayBuilder<FinderLocation>.GetInstance(out var result);

        // First just look for this normal constructor references using the name of it's containing type.
        var name = methodSymbol.ContainingType.Name;
        await AddReferencesInDocumentWorkerAsync(
            methodSymbol, name, state, result, cancellationToken).ConfigureAwait(false);

        // Next, look for constructor references through a global alias to our containing type.
        foreach (var globalAlias in state.GlobalAliases)
        {
            // ignore the cases where the global alias might match the type name (i.e.
            // global alias Console = System.Console).  We'll already find those references
            // above.
            if (state.SyntaxFacts.StringComparer.Equals(name, globalAlias))
                continue;

            await AddReferencesInDocumentWorkerAsync(
                methodSymbol, globalAlias, state, result, cancellationToken).ConfigureAwait(false);
        }

        // Nest, our containing type might itself have local aliases to it in this particular file.
        // If so, see what the local aliases are and then search for constructor references to that.
        using var _2 = ArrayBuilder<FinderLocation>.GetInstance(out var typeReferences);
        await NamedTypeSymbolReferenceFinder.AddReferencesToTypeOrGlobalAliasToItAsync(
            methodSymbol.ContainingType, state, typeReferences, cancellationToken).ConfigureAwait(false);

        var aliasReferences = await FindLocalAliasReferencesAsync(
            typeReferences, methodSymbol, state, cancellationToken).ConfigureAwait(false);

        // Finally, look for constructor references to predefined types (like `new int()`),
        // implicit object references, and inside global suppression attributes.
        result.AddRange(await FindPredefinedTypeReferencesAsync(
            methodSymbol, state, cancellationToken).ConfigureAwait(false));

        result.AddRange(await FindReferencesInImplicitObjectCreationExpressionAsync(
            methodSymbol, state, cancellationToken).ConfigureAwait(false));

        result.AddRange(await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
            methodSymbol, state, cancellationToken).ConfigureAwait(false));

        return result.ToImmutable();
    }

    /// <summary>
    /// Finds references to <paramref name="symbol"/> in this <paramref name="state"/>, but only if it referenced
    /// though <paramref name="name"/> (which might be the actual name of the type, or a global alias to it).
    /// </summary>
    private static async Task AddReferencesInDocumentWorkerAsync(
        IMethodSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        ArrayBuilder<FinderLocation> result,
        CancellationToken cancellationToken)
    {
        result.AddRange(await FindOrdinaryReferencesAsync(
            symbol, name, state, cancellationToken).ConfigureAwait(false));
        result.AddRange(await FindAttributeReferencesAsync(
            symbol, name, state, cancellationToken).ConfigureAwait(false));
    }

    private static ValueTask<ImmutableArray<FinderLocation>> FindOrdinaryReferencesAsync(
        IMethodSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        return FindReferencesInDocumentUsingIdentifierAsync(
            symbol, name, state, cancellationToken);
    }

    private static ValueTask<ImmutableArray<FinderLocation>> FindPredefinedTypeReferencesAsync(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        var predefinedType = symbol.ContainingType.SpecialType.ToPredefinedType();
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
        IMethodSymbol symbol,
        string name,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        return TryGetNameWithoutAttributeSuffix(name, state.SyntaxFacts, out var simpleName)
            ? FindReferencesInDocumentUsingIdentifierAsync(symbol, simpleName, state, cancellationToken)
            : new([]);
    }

    private Task<ImmutableArray<FinderLocation>> FindReferencesInImplicitObjectCreationExpressionAsync(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        CancellationToken cancellationToken)
    {
        // Only check `new (...)` calls that supply enough arguments to match all the required parameters for the constructor.
        var minimumArgumentCount = symbol.Parameters.Count(p => !p.IsOptional && !p.IsParams);
        var maximumArgumentCount = symbol.Parameters is [.., { IsParams: true }]
            ? int.MaxValue
            : symbol.Parameters.Length;

        var exactArgumentCount = symbol.Parameters.Any(static p => p.IsOptional || p.IsParams)
            ? -1
            : symbol.Parameters.Length;

        return FindReferencesInDocumentAsync(state, IsRelevantDocument, CollectMatchingReferences, cancellationToken);

        static bool IsRelevantDocument(SyntaxTreeIndex syntaxTreeInfo)
            => syntaxTreeInfo.ContainsImplicitObjectCreation;

        void CollectMatchingReferences(
            SyntaxNode node, FindReferencesDocumentState state, ArrayBuilder<FinderLocation> locations)
        {
            var syntaxFacts = state.SyntaxFacts;
            if (!syntaxFacts.IsImplicitObjectCreationExpression(node))
                return;

            // if there are too few or too many arguments, then don't bother checking.
            var actualArgumentCount = syntaxFacts.GetArgumentsOfObjectCreationExpression(node).Count;
            if (actualArgumentCount < minimumArgumentCount || actualArgumentCount > maximumArgumentCount)
                return;

            // if we need an exact count then make sure that the count we have fits the count we need.
            if (exactArgumentCount != -1 && exactArgumentCount != actualArgumentCount)
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
}
