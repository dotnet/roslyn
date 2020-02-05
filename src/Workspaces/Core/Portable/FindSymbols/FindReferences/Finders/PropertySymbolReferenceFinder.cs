// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
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

            var indexerReferences = symbol.IsIndexer
                ? await FindIndexerReferencesAsync(symbol, document, semanticModel, options, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<FinderLocation>.Empty;

            return nameReferences.Concat(forEachReferences)
                                 .Concat(indexerReferences);
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

        private async Task<ImmutableArray<FinderLocation>> FindIndexerReferencesAsync(
            IPropertySymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            if (options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                // Looking for individual get/set references.  Don't find anything here. 
                // these results will be provided by the PropertyAccessorSymbolReferenceFinder
                return ImmutableArray<FinderLocation>.Empty;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var indexerReferenceExpresssions = syntaxRoot.DescendantNodes(descendIntoTrivia: true)
                .Where(node => syntaxFacts.IsElementAccessExpression(node) || syntaxFacts.IsConditionalAccessExpression(node) || syntaxFacts.IsIndexerMemberCRef(node));
            var locations = ArrayBuilder<FinderLocation>.GetInstance();

            foreach (var node in indexerReferenceExpresssions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                (var indexerReference, var matched, var reason) = EvaluateIndexerNode(symbol, document, semanticModel, node, cancellationToken);
                if (!matched)
                {
                    continue;
                }

                var location = indexerReference.SyntaxTree.GetLocation(new TextSpan(indexerReference.SpanStart, 0));
                var symbolUsageInfo = GetSymbolUsageInfo(node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);
                locations.Add(new FinderLocation(
                    node, new ReferenceLocation(document, null, location, isImplicit: false, symbolUsageInfo, GetAdditionalFindUsagesProperties(node, semanticModel, syntaxFacts), candidateReason: reason)));
            }

            return locations.ToImmutableAndFree();
        }

        private static (SyntaxNode indexerReference, bool matched, CandidateReason reason) EvaluateIndexerNode(
            IPropertySymbol symbol, Document document, SemanticModel semanticModel,
            SyntaxNode node, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var symbolsMatch = GetStandardSymbolsNodeMatchFunction(symbol, document.Project.Solution, cancellationToken);

            SyntaxNode indexerReference;
            var matched = false;
            var reason = CandidateReason.None;

            if (syntaxFacts.IsElementAccessExpression(node))
            {
                // For an ElementAccessExpression the indexer we are looking for is the argumentList component.
                syntaxFacts.GetPartsOfElementAccessExpression(node, out var expression, out indexerReference);

                if (expression != null && symbolsMatch(expression, semanticModel).matched)
                {
                    // Element access with explicit member name (allowed in VB).
                    // We have already added a reference location for the member name identifier, so skip this one.
                    return (indexerReference, matched, reason);
                }

                (matched, reason) = symbolsMatch(node, semanticModel);
            }
            else if (syntaxFacts.IsConditionalAccessExpression(node))
            {
                // For a ConditionalAccessExpression the whenNotNull component is the indexer reference we are looking for
                syntaxFacts.GetPartsOfConditionalAccessExpression(node, out var expression, out indexerReference);

                if (syntaxFacts.IsInvocationExpression(indexerReference) && symbolsMatch(indexerReference, semanticModel).matched)
                {
                    // Element access with explicit member name (allowed in VB).
                    // We have already added a reference location for the member name identifier, so skip this one.
                    return (indexerReference, matched, reason);
                }

                (matched, reason) = symbolsMatch(indexerReference, semanticModel);
            }
            else
            {
                Debug.Assert(syntaxFacts.IsIndexerMemberCRef(node));

                // For an IndexerMemberCRef the node itself is the indexer we are looking for.
                indexerReference = node;

                (matched, reason) = symbolsMatch(node, semanticModel);
            }

            return (indexerReference, matched, reason);
        }
    }
}
