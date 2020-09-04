// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => symbol.TypeKind != TypeKind.Error;

        protected override Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            INamedTypeSymbol symbol,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<ISymbol>.GetInstance();

            if (symbol.AssociatedSymbol != null)
            {
                Add(result, ImmutableArray.Create(symbol.AssociatedSymbol));
            }

            // cascade to constructors
            Add(result, symbol.Constructors);

            // cascade to destructor
            Add(result, symbol.GetMembers(WellKnownMemberNames.DestructorName));

            return Task.FromResult(result.ToImmutableAndFree());
        }

        private static void Add<TSymbol>(ArrayBuilder<ISymbol> result, ImmutableArray<TSymbol> enumerable) where TSymbol : ISymbol
        {
            result.AddRange(enumerable.Cast<ISymbol>());
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            INamedTypeSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var documentsWithName = await FindDocumentsAsync(project, documents, findInGlobalSuppressions: true, cancellationToken, symbol.Name).ConfigureAwait(false);
            var documentsWithType = await FindDocumentsAsync(project, documents, symbol.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false);
            var documentsWithAttribute = TryGetNameWithoutAttributeSuffix(symbol.Name, project.LanguageServices.GetService<ISyntaxFactsService>(), out var simpleName)
                ? await FindDocumentsAsync(project, documents, findInGlobalSuppressions: false, cancellationToken, simpleName).ConfigureAwait(false)
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
            // Get the parent node that best matches what this token represents.  For example, if we have `new a.b()`
            // then the parent node of `b` won't be `a.b`, but rather `new a.b()`.  This will actually cause us to bind
            // to the constructor not the type.  That's a good thing as we don't want these object-creations to
            // associate with the type, but rather with the constructor itself.
            var findParentNode = GetNamedTypeOrConstructorFindParentNodeFunction(document, namedType);
            var symbolsMatch = GetStandardSymbolsMatchFunction(namedType, findParentNode, document.Project.Solution, cancellationToken);

            return FindReferencesInDocumentUsingIdentifierAsync(
                namedType, namedType.Name, document, semanticModel, symbolsMatch, cancellationToken);
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
                (t, m) => new ValueTask<(bool matched, CandidateReason reason)>((matched: true, reason: CandidateReason.None)),
                docCommentId: null,
                findInGlobalSuppressions: false,
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
                ? FindReferencesInDocumentUsingIdentifierAsync(simpleName, document, semanticModel,
                    symbolsMatch, docCommentId: null, findInGlobalSuppressions: false, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<FinderLocation>();
        }
    }
}
