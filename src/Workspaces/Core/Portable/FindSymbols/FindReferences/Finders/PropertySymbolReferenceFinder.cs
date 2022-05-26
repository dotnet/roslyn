// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
    using SymbolsMatchAsync = Func<SyntaxNode, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>>;

    internal sealed class PropertySymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IPropertySymbol>
    {
        protected override bool CanFind(IPropertySymbol symbol)
            => true;

        protected override Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            IPropertySymbol symbol,
            Solution solution,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);

            CascadeToBackingFields(symbol, result);
            CascadeToAccessors(symbol, result);
            CascadeToPrimaryConstructorParameters(symbol, result, cancellationToken);

            return Task.FromResult(result.ToImmutable());
        }

        private static void CascadeToBackingFields(IPropertySymbol symbol, ArrayBuilder<ISymbol> result)
        {
            foreach (var member in symbol.ContainingType.GetMembers())
            {
                if (member is IFieldSymbol field &&
                    symbol.Equals(field.AssociatedSymbol))
                {
                    result.Add(field);
                }
            }
        }

        private static void CascadeToAccessors(IPropertySymbol symbol, ArrayBuilder<ISymbol> result)
        {
            result.AddIfNotNull(symbol.GetMethod);
            result.AddIfNotNull(symbol.SetMethod);
        }

        private static void CascadeToPrimaryConstructorParameters(IPropertySymbol property, ArrayBuilder<ISymbol> result, CancellationToken cancellationToken)
        {
            if (property is
                {
                    IsStatic: false,
                    DeclaringSyntaxReferences.Length: > 0,
                    ContainingType:
                    {
                        IsRecord: true,
                        DeclaringSyntaxReferences.Length: > 0,
                    } containingType,
                })
            {
                // OK, we have a property in a record.  See if we can find a primary constructor that has a parameter that synthesized this
                var containingTypeSyntaxes = containingType.DeclaringSyntaxReferences.SelectAsArray(r => r.GetSyntax(cancellationToken));
                foreach (var constructor in containingType.Constructors)
                {
                    if (constructor.DeclaringSyntaxReferences.Length > 0)
                    {
                        var constructorSyntax = constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                        if (containingTypeSyntaxes.Contains(constructorSyntax))
                        {
                            // OK found the primary construct.  Try to find a parameter that corresponds to this property.
                            foreach (var parameter in constructor.Parameters)
                            {
                                if (property.Name.Equals(parameter.Name) &&
                                    property.Equals(parameter.GetAssociatedSynthesizedRecordProperty(cancellationToken)))
                                {
                                    result.Add(parameter);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected sealed override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IPropertySymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
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

            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, cancellationToken).ConfigureAwait(false);
            return ordinaryDocuments.Concat(forEachDocuments, elementAccessDocument, indexerMemberCrefDocument, documentsWithGlobalAttributes);
        }

        private static bool IsForEachProperty(IPropertySymbol symbol)
            => symbol.Name == WellKnownMemberNames.CurrentPropertyName;

        protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IPropertySymbol symbol,
            HashSet<string>? globalAliases,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var nameReferences = await FindReferencesInDocumentUsingSymbolNameAsync(
                symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            if (options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                // We want to associate property references to a specific accessor (if an accessor
                // is being referenced).  Check if this reference would match an accessor. If so, do
                // not add it.  It will be added by PropertyAccessorSymbolReferenceFinder.
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

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

            var suppressionReferences = await FindReferencesInDocumentInsideGlobalSuppressionsAsync(document, semanticModel, symbol, cancellationToken).ConfigureAwait(false);
            return nameReferences.Concat(forEachReferences, indexerReferences, suppressionReferences);
        }

        private static Task<ImmutableArray<Document>> FindDocumentWithElementAccessExpressionsAsync(
            Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
        {
            return FindDocumentsWithPredicateAsync(project, documents, info => info.ContainsElementAccessExpression, cancellationToken);
        }

        private static Task<ImmutableArray<Document>> FindDocumentWithIndexerMemberCrefAsync(
            Project project, IImmutableSet<Document>? documents, CancellationToken cancellationToken)
        {
            return FindDocumentsWithPredicateAsync(project, documents, info => info.ContainsIndexerMemberCref, cancellationToken);
        }

        private static async Task<ImmutableArray<FinderLocation>> FindIndexerReferencesAsync(
            IPropertySymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            if (options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                // Looking for individual get/set references.  Don't find anything here. 
                // these results will be provided by the PropertyAccessorSymbolReferenceFinder
                return ImmutableArray<FinderLocation>.Empty;
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var indexerReferenceExpresssions = syntaxRoot.DescendantNodes(descendIntoTrivia: true)
                .Where(node =>
                    syntaxFacts.IsElementAccessExpression(node) ||
                    syntaxFacts.IsConditionalAccessExpression(node) ||
                    syntaxFacts.IsIndexerMemberCRef(node));
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var locations);

            foreach (var node in indexerReferenceExpresssions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (matched, candidateReason, indexerReference) = await ComputeIndexerInformationAsync(
                    symbol, document, semanticModel, node, cancellationToken).ConfigureAwait(false);
                if (!matched)
                    continue;

                var location = syntaxTree.GetLocation(new TextSpan(indexerReference.SpanStart, 0));
                var symbolUsageInfo = GetSymbolUsageInfo(
                    node, semanticModel, syntaxFacts, semanticFacts, cancellationToken);

                locations.Add(new FinderLocation(node,
                    new ReferenceLocation(
                        document, alias: null, location, isImplicit: false, symbolUsageInfo,
                        GetAdditionalFindUsagesProperties(node, semanticModel, syntaxFacts),
                        candidateReason)));
            }

            return locations.ToImmutable();
        }

        private static ValueTask<(bool matched, CandidateReason reason, SyntaxNode indexerReference)> ComputeIndexerInformationAsync(
            IPropertySymbol symbol, Document document, SemanticModel semanticModel,
            SyntaxNode node, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var symbolsMatchAsync = GetStandardSymbolsNodeMatchFunction(symbol, document.Project.Solution, cancellationToken);

            if (syntaxFacts.IsElementAccessExpression(node))
            {
                // The indexerReference for an element access expression will not be null
                return ComputeElementAccessInformationAsync(
                    semanticModel, node, syntaxFacts, symbolsMatchAsync)!;
            }
            else if (syntaxFacts.IsConditionalAccessExpression(node))
            {
                return ComputeConditionalAccessInformationAsync(
                    semanticModel, node, syntaxFacts, symbolsMatchAsync);
            }
            else
            {
                Debug.Assert(syntaxFacts.IsIndexerMemberCRef(node));

                return ComputeIndexerMemberCRefInformationAsync(
                    semanticModel, node, symbolsMatchAsync);
            }
        }

        private static async ValueTask<(bool matched, CandidateReason reason, SyntaxNode indexerReference)> ComputeIndexerMemberCRefInformationAsync(
            SemanticModel semanticModel, SyntaxNode node, SymbolsMatchAsync symbolsMatchAsync)
        {
            var (matched, reason) = await symbolsMatchAsync(node, semanticModel).ConfigureAwait(false);

            // For an IndexerMemberCRef the node itself is the indexer we are looking for.
            return (matched, reason, node);
        }

        private static async ValueTask<(bool matched, CandidateReason reason, SyntaxNode indexerReference)> ComputeConditionalAccessInformationAsync(
            SemanticModel semanticModel, SyntaxNode node, ISyntaxFactsService syntaxFacts, SymbolsMatchAsync symbolsMatchAsync)
        {
            // For a ConditionalAccessExpression the whenNotNull component is the indexer reference we are looking for
            syntaxFacts.GetPartsOfConditionalAccessExpression(node, out _, out var indexerReference);

            if (syntaxFacts.IsInvocationExpression(indexerReference))
            {
                // call to something like: goo?.bar(1)
                //
                // this will already be handled by the existing method ref finder.
                return default;
            }

            var (matched, reason) = await symbolsMatchAsync(indexerReference, semanticModel).ConfigureAwait(false);
            return (matched, reason, indexerReference);
        }

        private static async ValueTask<(bool matched, CandidateReason reason, SyntaxNode? indexerReference)> ComputeElementAccessInformationAsync(
            SemanticModel semanticModel, SyntaxNode node,
            ISyntaxFactsService syntaxFacts, SymbolsMatchAsync symbolsMatchAsync)
        {
            // For an ElementAccessExpression the indexer we are looking for is the argumentList component.
            syntaxFacts.GetPartsOfElementAccessExpression(node, out var expression, out var indexerReference);
            if (expression != null && (await symbolsMatchAsync(expression, semanticModel).ConfigureAwait(false)).matched)
            {
                // Element access with explicit member name (allowed in VB). We will have
                // already added a reference location for the member name identifier, so skip
                // this one.
                return default;
            }

            var (matched, reason) = await symbolsMatchAsync(node, semanticModel).ConfigureAwait(false);
            return (matched, reason, indexerReference);
        }
    }
}
