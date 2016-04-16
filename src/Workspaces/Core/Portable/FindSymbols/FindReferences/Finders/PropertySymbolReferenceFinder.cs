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

        protected override async Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsAsync(
            IPropertySymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            var baseSymbols = await base.DetermineCascadedSymbolsAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);
            baseSymbols = baseSymbols ?? SpecializedCollections.EmptyEnumerable<ISymbol>();
            var backingField = symbol.ContainingType.GetMembers().OfType<IFieldSymbol>().Where(f => symbol.Equals(f.AssociatedSymbol));

            var result = baseSymbols.Concat(backingField);

            if (symbol.GetMethod != null)
            {
                result = result.Concat(symbol.GetMethod);
            }

            if (symbol.SetMethod != null)
            {
                result = result.Concat(symbol.SetMethod);
            }

            return result;
        }

        protected override async Task<IEnumerable<Document>> DetermineDocumentsToSearchAsync(
            IPropertySymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            var ordinaryDocuments = await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);

            var forEachDocuments = IsForEachProperty(symbol)
                ? await FindDocumentsWithForEachStatementsAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<Document>();

            var elementAccessDocument = symbol.IsIndexer
                ? await FindDocumentWithElementAccessExpressionsAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<Document>();

            var indexerMemberCrefDocument = symbol.IsIndexer
                ? await FindDocumentWithIndexerMemberCrefAsync(project, documents, cancellationToken).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<Document>();

            return ordinaryDocuments.Concat(forEachDocuments).Concat(elementAccessDocument).Concat(indexerMemberCrefDocument);
        }

        private static bool IsForEachProperty(IPropertySymbol symbol)
        {
            return symbol.Name == WellKnownMemberNames.CurrentPropertyName;
        }

        protected override async Task<IEnumerable<ReferenceLocation>> FindReferencesInDocumentAsync(
            IPropertySymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var nameReferences = await FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, cancellationToken).ConfigureAwait(false);

            var forEachReferences = IsForEachProperty(symbol)
                ? await FindReferencesInForEachStatementsAsync(symbol, document, cancellationToken).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<ReferenceLocation>();

            var elementAccessReferences = symbol.IsIndexer
                ? await FindElementAccessReferencesAndIndexerMemberCrefReferencesAsync(symbol, document, cancellationToken).ConfigureAwait(false)
                : SpecializedCollections.EmptyEnumerable<ReferenceLocation>();

            return nameReferences.Concat(forEachReferences).Concat(elementAccessReferences);
        }

        private Task<IEnumerable<Document>> FindDocumentWithElementAccessExpressionsAsync(
            Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeInfo.GetContextInfoAsync(d, c).ConfigureAwait(false);
                return info.ContainsElementAccessExpression;
            }, cancellationToken);
        }

        private Task<IEnumerable<Document>> FindDocumentWithIndexerMemberCrefAsync(
            Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var info = await SyntaxTreeInfo.GetContextInfoAsync(d, c).ConfigureAwait(false);
                return info.ContainsIndexerMemberCref;
            }, cancellationToken);
        }

        private async Task<IEnumerable<ReferenceLocation>> FindElementAccessReferencesAndIndexerMemberCrefReferencesAsync(
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

            var locations = new List<ReferenceLocation>();

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

            return locations;
        }
    }
}
