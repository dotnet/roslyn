// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        {
            return symbol.MethodKind == MethodKind.Constructor;
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var typeName = symbol.ContainingType.Name;
            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, typeName).ConfigureAwait(false);
            var documentsWithType = await FindDocumentsAsync(project, documents, symbol.ContainingType.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false);
            var documentsWithAttribute = TryGetNameWithoutAttributeSuffix(typeName, project.LanguageServices.GetService<ISyntaxFactsService>(), out var simpleName)
                ? await FindDocumentsAsync(project, documents, cancellationToken, simpleName).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<Document>();

            return documentsWithName.Concat(documentsWithType)
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
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var findParentNode = GetFindParentNodeFunction(syntaxFacts);

            var normalReferences = await FindReferencesInDocumentWorkerAsync(methodSymbol, document, semanticModel, findParentNode, cancellationToken).ConfigureAwait(false);
            var nonAliasTypeReferences = await NamedTypeSymbolReferenceFinder.FindNonAliasReferencesAsync(methodSymbol.ContainingType, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var aliasReferences = await FindAliasReferencesAsync(
                nonAliasTypeReferences, methodSymbol, document, semanticModel,
                findParentNode, cancellationToken).ConfigureAwait(false);
            return normalReferences.Concat(aliasReferences);
        }

        private static Func<SyntaxToken, SyntaxNode> GetFindParentNodeFunction(ISyntaxFactsService syntaxFacts)
        {
            return t => syntaxFacts.GetBindableParent(t);
        }

        private async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentWorkerAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var ordinaryRefs = await FindOrdinaryReferencesAsync(symbol, document, semanticModel, findParentNode, cancellationToken).ConfigureAwait(false);
            var attributeRefs = await FindAttributeReferencesAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var predefinedTypeRefs = await FindPredefinedTypeReferencesAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            return ordinaryRefs.Concat(attributeRefs).Concat(predefinedTypeRefs);
        }

        private Task<ImmutableArray<FinderLocation>> FindOrdinaryReferencesAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var name = symbol.ContainingType.Name;
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, name, document, semanticModel, findParentNode, cancellationToken);
        }

        private Task<ImmutableArray<FinderLocation>> FindPredefinedTypeReferencesAsync(
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

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return FindReferencesInDocumentAsync(symbol, document,
                semanticModel,
                t => IsPotentialReference(predefinedType, syntaxFacts, t),
                cancellationToken);
        }

        private Task<ImmutableArray<FinderLocation>> FindAttributeReferencesAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return TryGetNameWithoutAttributeSuffix(symbol.ContainingType.Name, syntaxFacts, out var simpleName)
                ? FindReferencesInDocumentUsingIdentifierAsync(symbol, simpleName, document, semanticModel, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<FinderLocation>();
        }
    }
}
