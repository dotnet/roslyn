// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
            CancellationToken cancellationToken)
        {
            var baseSymbols = await base.DetermineCascadedSymbolsAsync(symbolAndProjectId, solution, projects, cancellationToken).ConfigureAwait(false);

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

        protected override async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            IPropertySymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var nameReferences = await FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, cancellationToken).ConfigureAwait(false);

            var forEachReferences = IsForEachProperty(symbol)
                ? await FindReferencesInForEachStatementsAsync(symbol, document, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<ReferenceLocation>.Empty;

            var elementAccessReferences = symbol.IsIndexer
                ? await FindElementAccessReferencesAndIndexerMemberCrefReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false)
                : ImmutableArray<ReferenceLocation>.Empty;

            return nameReferences.Concat(forEachReferences)
                                 .Concat(elementAccessReferences);
        }

        private Task<ImmutableArray<Document>> FindDocumentWithElementAccessExpressionsAsync(
            Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeInfo.GetContextInfoAsync(d, c).ConfigureAwait(false);
                return info.ContainsElementAccessExpression;
            }, cancellationToken);
        }

        private Task<ImmutableArray<Document>> FindDocumentWithIndexerMemberCrefAsync(
            Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeInfo.GetContextInfoAsync(d, c).ConfigureAwait(false);
                return info.ContainsIndexerMemberCref;
            }, cancellationToken);
        }

        private async Task<ImmutableArray<ReferenceLocation>> FindElementAccessReferencesAndIndexerMemberCrefReferencesAsync(
            IPropertySymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetStandardSymbolsNodeMatchFunction(symbol, document.Project.Solution, cancellationToken);

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            // Now that we have Doc Comments in place, We are searching for References in the Trivia as well by setting descendIntoTrivia: true
            var elementAccessExpressions = syntaxRoot.DescendantNodes().Where(syntaxFacts.IsElementAccessExpression);
            var indexerMemberCref = syntaxRoot.DescendantNodes(descendIntoTrivia: true).Where(syntaxFacts.IsIndexerMemberCRef);

            var elementAccessExpressionsAndIndexerMemberCref = elementAccessExpressions.Concat(indexerMemberCref);

            var locations = ArrayBuilder<ReferenceLocation>.GetInstance();

            foreach (var node in elementAccessExpressionsAndIndexerMemberCref)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var match = symbolsMatch(node, semanticModel);
                if (match.Item1)
                {
                    SyntaxNode nodeToBeReferenced = null;

                    if (syntaxFacts.IsIndexerMemberCRef(node))
                    {
                        nodeToBeReferenced = node;
                    }
                    else
                    {
                        var childNodes = node.ChildNodes();
                        var leftOfInvocation = childNodes.First();
                        if (symbolsMatch(leftOfInvocation, semanticModel).Item1)
                        {
                            // Element access with explicit member name (allowed in VB).
                            // We have already added a reference location for the member name identifier, so skip this one.
                            continue;
                        }

                        nodeToBeReferenced = childNodes.Skip(1).FirstOrDefault();
                    }

                    if (nodeToBeReferenced != null)
                    {
                        var location = nodeToBeReferenced.SyntaxTree.GetLocation(new TextSpan(nodeToBeReferenced.SpanStart, 0));
                        locations.Add(new ReferenceLocation(document, null, location, isImplicit: false, isWrittenTo: false, candidateReason: match.Item2));
                    }
                }
            }

            return locations.ToImmutableAndFree();
        }
    }
}
