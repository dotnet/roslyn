// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class PropertySymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IPropertySymbol>
    {
        protected override bool CanFind(IPropertySymbol symbol)
        {
            return true;
        }

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IPropertySymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var baseSymbols = await base.DetermineCascadedSymbolsAsync(
                symbolAndProjectId, solution, projects, options, cancellationToken).ConfigureAwait(false);

            var symbol = symbolAndProjectId.Symbol;
            var backingFields = symbol.ContainingType.GetMembers()
                                      .OfType<IFieldSymbol>()
                                      .Where(f => symbol.Equals(f.AssociatedSymbol))
                                      .Select(f => (SymbolAndProjectId)symbolAndProjectId.WithSymbol(f))
                                      .ToImmutableArray();

            var result = baseSymbols.Concat(backingFields);

            if (symbol.GetMethod != null)
            {
                result = result.Add(symbolAndProjectId.WithSymbol(symbol.GetMethod));
            }

            if (symbol.SetMethod != null)
            {
                result = result.Add(symbolAndProjectId.WithSymbol(symbol.SetMethod));
            }

            return result;
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IPropertySymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var ordinaryDocuments = await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);

            var forEachDocuments = IsForEachProperty(symbol)
                ? await FindDocumentsWithForEachStatementsAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            var elementAccessDocument = symbol.IsIndexer
                ? await FindDocumentWithElementAccessExpressionsAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            var indexerMemberCrefDocument = symbol.IsIndexer
                ? await FindDocumentWithIndexerMemberCrefAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<Document>.Empty;

            return ordinaryDocuments.Concat(forEachDocuments)
                                    .Concat(elementAccessDocument)
                                    .Concat(indexerMemberCrefDocument);
        }

        private static bool IsForEachProperty(IPropertySymbol symbol)
        {
            return symbol.Name == WellKnownMemberNames.CurrentPropertyName;
        }

        protected override async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IPropertySymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            var nameReferences = await FindReferencesInDocumentUsingSymbolNameAsync(
                symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            if (options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                // We want to associate property references to a specific accessor (if an accessor
                // is being referenced).  Check if this reference would match an accessor. If so, do
                // not add it.  It will be added by PropertyAccessorSymbolReferenceFinder.
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

                nameReferences = nameReferences.WhereAsArray(loc =>
                {
                    var accessors = GetReferencedAccessorSymbols(
                        syntaxFacts, semanticFacts, semanticModel, symbol, loc.Node, cancellationToken);
                    return accessors.IsEmpty;
                });
            }

            var forEachReferences = IsForEachProperty(symbol)
                ? await FindReferencesInForEachStatementsAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            var elementAccessReferences = symbol.IsIndexer
                ? await FindElementAccessReferencesAsync(symbol, document, semanticModel, options, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            var indexerCrefReferences = symbol.IsIndexer
                ? await FindIndexerCrefReferencesAsync(symbol, document, semanticModel, options, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            return nameReferences.Concat(forEachReferences)
                                 .Concat(elementAccessReferences)
                                 .Concat(indexerCrefReferences);
        }

        private Task<ImmutableArray<Document>> FindDocumentWithElementAccessExpressionsAsync(
            Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsWithPredicateAsync(project, documents, info => info.ContainsElementAccessExpression, cancellationToken);
        }

        private Task<ImmutableArray<Document>> FindDocumentWithIndexerMemberCrefAsync(
            Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsWithPredicateAsync(project, documents, info => info.ContainsIndexerMemberCref, cancellationToken);
        }

        private async Task<ImmutableArray<FinderLocation>> FindElementAccessReferencesAsync(
            IPropertySymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            if (options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                // Looking for individual get/set references.  Don't find anything here. 
                // these results will be provided by the PropertyAccessorSymbolReferenceFinder
                return ImmutableArray<FinderLocation>.Empty;
            }

            var symbolsMatch = GetStandardSymbolsNodeMatchFunction(symbol, document.Project.Solution, cancellationToken);

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var elementAccessExpressions = syntaxRoot.DescendantNodes().Where(syntaxFacts.IsElementAccessExpression);
            var locations = ArrayBuilder<FinderLocation>.GetInstance();

            foreach (var node in elementAccessExpressions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (matched, reason) = symbolsMatch(node, semanticModel);
                if (matched)
                {
                    syntaxFacts.GetPartsOfElementAccessExpression(node, out var expression, out var argumentList);

                    if (symbolsMatch(expression, semanticModel).matched)
                    {
                        // Element access with explicit member name (allowed in VB).
                        // We have already added a reference location for the member name identifier, so skip this one.
                        continue;
                    }

                    var location = argumentList.SyntaxTree.GetLocation(new TextSpan(argumentList.SpanStart, 0));

                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);
                    locations.Add(new FinderLocation(
                        node, new ReferenceLocation(document, null, location, isImplicit: false, symbolUsageInfo, GetAdditionalProperties(syntaxFacts, semanticModel, node), candidateReason: reason)));
                }
            }

            return locations.ToImmutableAndFree();
        }

        private async Task<ImmutableArray<FinderLocation>> FindIndexerCrefReferencesAsync(
            IPropertySymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            if (options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                // can't find indexer get/set accessors in a cref.
                return ImmutableArray<FinderLocation>.Empty;
            }

            var symbolsMatch = GetStandardSymbolsNodeMatchFunction(symbol, document.Project.Solution, cancellationToken);

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            // Now that we have Doc Comments in place, We are searching for References in the Trivia as well by setting descendIntoTrivia: true
            var indexerMemberCrefs = syntaxRoot.DescendantNodes(descendIntoTrivia: true)
                                               .Where(syntaxFacts.IsIndexerMemberCRef);

            var locations = ArrayBuilder<FinderLocation>.GetInstance();

            foreach (var node in indexerMemberCrefs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (matched, reason) = symbolsMatch(node, semanticModel);
                if (matched)
                {
                    var location = node.SyntaxTree.GetLocation(new TextSpan(node.SpanStart, 0));
                    var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);
                    locations.Add(new FinderLocation(
                        node, new ReferenceLocation(document, null, location, isImplicit: false, symbolUsageInfo, GetAdditionalProperties(syntaxFacts, semanticModel, node), candidateReason: reason)));
                }
            }

            return locations.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns an array of additional properties which consists of the Containing Type and Containing Member of the reference.
        /// </summary>
        /// <param name="syntaxFacts"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private static ImmutableArray<AdditionalProperty> GetAdditionalProperties(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel, SyntaxNode node)
        {
            var additionalProperties = new ArrayBuilder<AdditionalProperty>();
            additionalProperties.Add(GetInfo(syntaxFacts.GetContainingTypeDeclaration(node, node.SpanStart), ContainingTypeInfoPropertyName, semanticModel, syntaxFacts));
            additionalProperties.Add(GetInfo(syntaxFacts.GetContainingMemberDeclaration(node, node.SpanStart), ContainingMemberInfoPropertyName, semanticModel, syntaxFacts));
            return additionalProperties.ToImmutable();
        }
    }
}
