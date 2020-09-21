// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class ConstructorSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
    {
        public static readonly ConstructorSymbolReferenceFinder Instance = new ConstructorSymbolReferenceFinder();

        private ConstructorSymbolReferenceFinder()
        {
        }

        protected override bool CanFind(IMethodSymbol symbol)
            => symbol.MethodKind switch
            {
                MethodKind.Constructor => true,
                MethodKind.StaticConstructor => true,
                _ => false,
            };

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var typeName = symbol.ContainingType.Name;
            var documentsWithName = await FindDocumentsAsync(project, documents, findInGlobalSuppressions: true, cancellationToken, typeName).ConfigureAwait(false);
            var documentsWithType = await FindDocumentsAsync(project, documents, symbol.ContainingType.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false);
            var documentsWithAttribute = TryGetNameWithoutAttributeSuffix(typeName, project.LanguageServices.GetRequiredService<ISyntaxFactsService>(), out var simpleName)
                ? await FindDocumentsAsync(project, documents, findInGlobalSuppressions: false, cancellationToken, simpleName).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<Document>();

            var documentsWithImplicitObjectCreations = symbol.MethodKind == MethodKind.Constructor
                ? await FindDocumentsWithImplicitObjectCreationExpressionAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            return documentsWithName.Concat(documentsWithType, documentsWithImplicitObjectCreations)
                                    .Concat(documentsWithAttribute)
                                    .Distinct()
                                    .ToImmutableArray();
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

        protected override Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return FindAllReferencesInDocumentAsync(methodSymbol, document, semanticModel, cancellationToken);
        }

        internal async Task<ImmutableArray<FinderLocation>> FindAllReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var findParentNode = GetNamedTypeOrConstructorFindParentNodeFunction(document, methodSymbol);

            var normalReferences = await FindReferencesInDocumentWorkerAsync(methodSymbol, document, semanticModel, findParentNode, cancellationToken).ConfigureAwait(false);
            var nonAliasTypeReferences = await NamedTypeSymbolReferenceFinder.FindNonAliasReferencesAsync(methodSymbol.ContainingType, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var aliasReferences = await FindAliasReferencesAsync(
                nonAliasTypeReferences, methodSymbol, document, semanticModel,
                findParentNode, cancellationToken).ConfigureAwait(false);
            return normalReferences.Concat(aliasReferences);
        }

        private async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentWorkerAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var ordinaryRefs = await FindOrdinaryReferencesAsync(symbol, document, semanticModel, findParentNode, cancellationToken).ConfigureAwait(false);
            var attributeRefs = await FindAttributeReferencesAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var predefinedTypeRefs = await FindPredefinedTypeReferencesAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var implicitObjectCreationMatches = await FindReferencesInImplicitObjectCreationExpressionAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            return ordinaryRefs.Concat(attributeRefs, predefinedTypeRefs, implicitObjectCreationMatches);
        }

        private static Task<ImmutableArray<FinderLocation>> FindOrdinaryReferencesAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SyntaxNode>? findParentNode,
            CancellationToken cancellationToken)
        {
            var name = symbol.ContainingType.Name;
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, name, document, semanticModel, findParentNode, cancellationToken);
        }

        private static Task<ImmutableArray<FinderLocation>> FindPredefinedTypeReferencesAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var predefinedType = symbol.ContainingType.SpecialType.ToPredefinedType();
            if (predefinedType == PredefinedType.None)
            {
                return SpecializedTasks.EmptyImmutableArray<FinderLocation>();
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return FindReferencesInDocumentAsync(symbol, document,
                semanticModel,
                t => IsPotentialReference(predefinedType, syntaxFacts, t),
                cancellationToken);
        }

        private static Task<ImmutableArray<FinderLocation>> FindAttributeReferencesAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return TryGetNameWithoutAttributeSuffix(symbol.ContainingType.Name, syntaxFacts, out var simpleName)
                ? FindReferencesInDocumentUsingIdentifierAsync(symbol, simpleName, document, semanticModel, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<FinderLocation>();
        }
    }
}
