// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class NamedTypeSymbolReferenceFinder : AbstractReferenceFinder<INamedTypeSymbol>
    {
        protected override bool CanFind(INamedTypeSymbol symbol)
        {
            return symbol.TypeKind != TypeKind.Error;
        }

        protected override Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<INamedTypeSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            var symbol = symbolAndProjectId.Symbol;
            if (symbol.AssociatedSymbol != null)
            {
                Add(result, symbolAndProjectId, ImmutableArray.Create(symbol.AssociatedSymbol));
            }

            // cascade to constructors
            Add(result, symbolAndProjectId, symbol.Constructors);

            // cascade to destructor
            Add(result, symbolAndProjectId, symbol.GetMembers(WellKnownMemberNames.DestructorName));

            return Task.FromResult(result.ToImmutableAndFree());
        }

        private void Add<TSymbol>(
            ArrayBuilder<SymbolAndProjectId> result,
            SymbolAndProjectId symbolAndProjectId,
            ImmutableArray<TSymbol> enumerable) where TSymbol : ISymbol
        {
            result.AddRange(enumerable.Select(
                s => symbolAndProjectId.WithSymbol((ISymbol)s)));
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            INamedTypeSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);
            var documentsWithType = await FindDocumentsAsync(project, documents, symbol.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false);
            var documentsWithAttribute = TryGetNameWithoutAttributeSuffix(symbol.Name, project.LanguageServices.GetService<ISyntaxFactsService>(), out var simpleName)
                ? await FindDocumentsAsync(project, documents, cancellationToken, simpleName).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            return documentsWithName.Concat(documentsWithType)
                                    .Concat(documentsWithAttribute);
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

        protected override async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            INamedTypeSymbol namedType,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var namedTypeReferences = await FindReferencesInDocumentWorker(
                namedType, document, semanticModel, cancellationToken).ConfigureAwait(false);

            // Mark any references that are also Constructor references.  Some callers
            // will want to know about these so they won't display duplicates.
            return await MarkConstructorReferences(
                namedType, document, semanticModel, namedTypeReferences, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<FinderLocation>> MarkConstructorReferences(
            INamedTypeSymbol namedType, Document document,
            SemanticModel semanticModel,
            ImmutableArray<FinderLocation> namedTypeReferences,
            CancellationToken cancellationToken)
        {
            var constructorReferences = ArrayBuilder<FinderLocation>.GetInstance();
            foreach (var constructor in namedType.Constructors)
            {
                var references = await ConstructorSymbolReferenceFinder.Instance.FindAllReferencesInDocumentAsync(
                    constructor, document, semanticModel, cancellationToken).ConfigureAwait(false);
                constructorReferences.AddRange(references);
            }

            var result = ArrayBuilder<FinderLocation>.GetInstance();
            foreach (var finderLocation in namedTypeReferences)
            {
                if (Contains(constructorReferences, finderLocation))
                {
                    var location = finderLocation.Location;
                    location.IsDuplicateReferenceLocation = true;
                    result.Add(finderLocation);
                }
                else
                {
                    result.Add(finderLocation);
                }
            }

            return result.ToImmutableAndFree();
        }

        private bool Contains(
            ArrayBuilder<FinderLocation> constructorReferences,
            FinderLocation reference)
        {
            foreach (var constructorRef in constructorReferences)
            {
                if (reference.Location.Location == constructorRef.Location.Location)
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentWorker(
            INamedTypeSymbol namedType,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var nonAliasReferences = await FindNonAliasReferencesAsync(namedType, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);
            var aliasReferences = await FindAliasReferencesAsync(nonAliasReferences, document, semanticModel, symbolsMatch, cancellationToken).ConfigureAwait(false);
            return nonAliasReferences.Concat(aliasReferences);
        }

        internal static async Task<ImmutableArray<FinderLocation>> FindNonAliasReferencesAsync(
            INamedTypeSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var ordinaryRefs = await FindOrdinaryReferencesAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var attributeRefs = await FindAttributeReferencesAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var predefinedTypeRefs = await FindPredefinedTypeReferencesAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
            return ordinaryRefs.Concat(attributeRefs).Concat(predefinedTypeRefs);
        }

        private static Task<ImmutableArray<FinderLocation>> FindOrdinaryReferencesAsync(
            INamedTypeSymbol namedType,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);

            return FindReferencesInDocumentUsingIdentifierAsync(
                namedType.Name, document, semanticModel, symbolsMatch, cancellationToken);
        }

        private static Task<ImmutableArray<FinderLocation>> FindPredefinedTypeReferencesAsync(
            INamedTypeSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var predefinedType = symbol.SpecialType.ToPredefinedType();
            if (predefinedType == PredefinedType.None)
            {
                return SpecializedTasks.EmptyImmutableArray<FinderLocation>();
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return FindReferencesInDocumentAsync(document, semanticModel, t =>
                IsPotentialReference(predefinedType, syntaxFacts, t),
                (t, m) => (matched: true, reason: CandidateReason.None),
                cancellationToken);
        }

        private static Task<ImmutableArray<FinderLocation>> FindAttributeReferencesAsync(
            INamedTypeSymbol namedType,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return TryGetNameWithoutAttributeSuffix(namedType.Name, syntaxFacts, out var simpleName)
                ? FindReferencesInDocumentUsingIdentifierAsync(simpleName, document, semanticModel, symbolsMatch, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<FinderLocation>();
        }
    }
}
