// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
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
            CancellationToken cancellationToken)
        {
            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);
            var documentsWithType = await FindDocumentsAsync(project, documents, symbol.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false);

            string simpleName;
            var documentsWithAttribute = TryGetNameWithoutAttributeSuffix(symbol.Name, project.LanguageServices.GetService<ISyntaxFactsService>(), out simpleName)
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
            PredefinedType actualType;

            return
                syntaxFacts.TryGetPredefinedType(token, out actualType) &&
                predefinedType == actualType;
        }

        protected override async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            INamedTypeSymbol namedType,
            Document document,
            CancellationToken cancellationToken)
        {
            var nonAliasReferences = await FindNonAliasReferencesAsync(namedType, document, cancellationToken).ConfigureAwait(false);
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);
            var aliasReferences = await FindAliasReferencesAsync(nonAliasReferences, namedType, document, symbolsMatch, cancellationToken).ConfigureAwait(false);
            return nonAliasReferences.Concat(aliasReferences);
        }

        internal static async Task<ImmutableArray<ReferenceLocation>> FindNonAliasReferencesAsync(
            INamedTypeSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var ordinaryRefs = await FindOrdinaryReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false);
            var attributeRefs = await FindAttributeReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false);
            var predefinedTypeRefs = await FindPredefinedTypeReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false);
            return ordinaryRefs.Concat(attributeRefs).Concat(predefinedTypeRefs);
        }

        private static Task<ImmutableArray<ReferenceLocation>> FindOrdinaryReferencesAsync(
            INamedTypeSymbol namedType,
            Document document,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);

            return FindReferencesInDocumentUsingIdentifierAsync(
                namedType.Name, document, symbolsMatch, cancellationToken);
        }

        private static Task<ImmutableArray<ReferenceLocation>> FindPredefinedTypeReferencesAsync(
            INamedTypeSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var predefinedType = symbol.SpecialType.ToPredefinedType();
            if (predefinedType == PredefinedType.None)
            {
                return SpecializedTasks.EmptyImmutableArray<ReferenceLocation>();
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return FindReferencesInDocumentAsync(symbol, document, t =>
                IsPotentialReference(predefinedType, syntaxFacts, t),
                (t, m) => ValueTuple.Create(true, CandidateReason.None),
                cancellationToken);
        }

        private static Task<ImmutableArray<ReferenceLocation>> FindAttributeReferencesAsync(
            INamedTypeSymbol namedType,
            Document document,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            string simpleName;
            return TryGetNameWithoutAttributeSuffix(namedType.Name, syntaxFacts, out simpleName)
                ? FindReferencesInDocumentUsingIdentifierAsync(simpleName, document, symbolsMatch, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<ReferenceLocation>();
        }
    }
}
