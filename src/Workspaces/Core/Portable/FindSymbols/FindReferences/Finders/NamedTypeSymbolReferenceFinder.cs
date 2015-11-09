// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        protected override Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsAsync(
            INamedTypeSymbol symbol,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            List<ISymbol> result = null;
            if (symbol.AssociatedSymbol != null)
            {
                result = Add(result, SpecializedCollections.SingletonEnumerable(symbol.AssociatedSymbol));
            }

            // cascade to constructors
            result = Add(result, symbol.Constructors);

            // cascade to destructor
            result = Add(result, symbol.GetMembers(WellKnownMemberNames.DestructorName));

            return Task.FromResult<IEnumerable<ISymbol>>(result ?? SpecializedCollections.EmptyList<ISymbol>());
        }

        private List<ISymbol> Add(List<ISymbol> result, IEnumerable<ISymbol> enumerable)
        {
            if (enumerable != null)
            {
                result = result ?? new List<ISymbol>();
                result.AddRange(enumerable);
            }

            return result;
        }

        protected override async Task<IEnumerable<Document>> DetermineDocumentsToSearchAsync(
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
                : SpecializedCollections.EmptyEnumerable<Document>();

            return documentsWithName.Concat(documentsWithType).Concat(documentsWithAttribute);
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

        protected override async Task<IEnumerable<ReferenceLocation>> FindReferencesInDocumentAsync(
            INamedTypeSymbol namedType,
            Document document,
            CancellationToken cancellationToken)
        {
            var nonAliasReferences = await FindNonAliasReferencesAsync(namedType, document, cancellationToken).ConfigureAwait(false);
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);
            var aliasReferences = await FindAliasReferencesAsync(nonAliasReferences, namedType, document, symbolsMatch, cancellationToken).ConfigureAwait(false);
            return nonAliasReferences.Concat(aliasReferences);
        }

        internal static async Task<IEnumerable<ReferenceLocation>> FindNonAliasReferencesAsync(
            INamedTypeSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var ordinaryRefs = await FindOrdinaryReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false);
            var attributeRefs = await FindAttributeReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false);
            var predefinedTypeRefs = await FindPredefinedTypeReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false);
            return ordinaryRefs.Concat(attributeRefs).Concat(predefinedTypeRefs);
        }

        private static Task<IEnumerable<ReferenceLocation>> FindOrdinaryReferencesAsync(
            INamedTypeSymbol namedType,
            Document document,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);

            return FindReferencesInDocumentUsingIdentifierAsync(
                namedType.Name, document, symbolsMatch, cancellationToken);
        }

        private static Task<IEnumerable<ReferenceLocation>> FindPredefinedTypeReferencesAsync(
            INamedTypeSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var predefinedType = symbol.SpecialType.ToPredefinedType();
            if (predefinedType == PredefinedType.None)
            {
                return SpecializedTasks.EmptyEnumerable<ReferenceLocation>();
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return FindReferencesInDocumentAsync(symbol, document, t =>
                IsPotentialReference(predefinedType, syntaxFacts, t),
                (t, m) => ValueTuple.Create(true, CandidateReason.None),
                cancellationToken);
        }

        private static Task<IEnumerable<ReferenceLocation>> FindAttributeReferencesAsync(
            INamedTypeSymbol namedType,
            Document document,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, null, document.Project.Solution, cancellationToken);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            string simpleName;
            return TryGetNameWithoutAttributeSuffix(namedType.Name, syntaxFacts, out simpleName)
                ? FindReferencesInDocumentUsingIdentifierAsync(simpleName, document, symbolsMatch, cancellationToken)
                : SpecializedTasks.EmptyEnumerable<ReferenceLocation>();
        }
    }
}
