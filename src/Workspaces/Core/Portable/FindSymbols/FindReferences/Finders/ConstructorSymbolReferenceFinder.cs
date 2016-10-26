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
            CancellationToken cancellationToken)
        {
            var typeName = symbol.ContainingType.Name;
            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, typeName).ConfigureAwait(false);
            var documentsWithType = await FindDocumentsAsync(project, documents, symbol.ContainingType.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false);

            string simpleName;
            var documentsWithAttribute = TryGetNameWithoutAttributeSuffix(typeName, project.LanguageServices.GetService<ISyntaxFactsService>(), out simpleName)
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
            PredefinedType actualType;

            return
                syntaxFacts.TryGetPredefinedType(token, out actualType) &&
                predefinedType == actualType;
        }

        protected override Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            CancellationToken cancellationToken)
        {
            return FindAllReferencesInDocumentAsync(methodSymbol, document, cancellationToken);
        }

        internal async Task<ImmutableArray<ReferenceLocation>> FindAllReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var findParentNode = GetFindParentNodeFunction(syntaxFacts);

            var normalReferences = await FindReferencesInDocumentWorkerAsync(methodSymbol, document, findParentNode, cancellationToken).ConfigureAwait(false);
            var nonAliasTypeReferences = await NamedTypeSymbolReferenceFinder.FindNonAliasReferencesAsync(methodSymbol.ContainingType, document, cancellationToken).ConfigureAwait(false);
            var aliasReferences = await FindAliasReferencesAsync(
                nonAliasTypeReferences, methodSymbol, document, 
                findParentNode, cancellationToken).ConfigureAwait(false);
            return normalReferences.Concat(aliasReferences);
        }

        private static Func<SyntaxToken, SyntaxNode> GetFindParentNodeFunction(ISyntaxFactsService syntaxFacts)
        {
            return t => syntaxFacts.GetBindableParent(t);
        }

        private async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentWorkerAsync(
            IMethodSymbol symbol,
            Document document,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var ordinaryRefs = await FindOrdinaryReferencesAsync(symbol, document, findParentNode, cancellationToken).ConfigureAwait(false);
            var attributeRefs = await FindAttributeReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false);
            var predefinedTypeRefs = await FindPredefinedTypeReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false);

            return ordinaryRefs.Concat(attributeRefs).Concat(predefinedTypeRefs);
        }

        private Task<ImmutableArray<ReferenceLocation>> FindOrdinaryReferencesAsync(
            IMethodSymbol symbol,
            Document document,
            Func<SyntaxToken, SyntaxNode> findParentNode,
            CancellationToken cancellationToken)
        {
            var name = symbol.ContainingType.Name;
            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, name, document, findParentNode, cancellationToken);
        }

        private Task<ImmutableArray<ReferenceLocation>> FindPredefinedTypeReferencesAsync(
            IMethodSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var predefinedType = symbol.ContainingType.SpecialType.ToPredefinedType();
            if (predefinedType == PredefinedType.None)
            {
                return SpecializedTasks.EmptyImmutableArray<ReferenceLocation>();
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return FindReferencesInDocumentAsync(symbol, document,
                t => IsPotentialReference(predefinedType, syntaxFacts, t),
                cancellationToken);
        }

        private Task<ImmutableArray<ReferenceLocation>> FindAttributeReferencesAsync(
            IMethodSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            string simpleName;
            return TryGetNameWithoutAttributeSuffix(symbol.ContainingType.Name, syntaxFacts, out simpleName)
                ? FindReferencesInDocumentUsingIdentifierAsync(symbol, simpleName, document, cancellationToken)
                : SpecializedTasks.EmptyImmutableArray<ReferenceLocation>();
        }
    }
}
