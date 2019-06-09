// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class PropertyAccessorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
            => symbol.MethodKind.IsPropertyAccessor();

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IMethodSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var result = await base.DetermineCascadedSymbolsAsync(
                symbolAndProjectId, solution, projects, options, cancellationToken).ConfigureAwait(false);

            // If we've been asked to search for specific accessors, then do not cascade.
            // We don't want to produce results for the associated property.
            if (!options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                var symbol = symbolAndProjectId.Symbol;
                if (symbol.AssociatedSymbol != null)
                {
                    result = result.Add(symbolAndProjectId.WithSymbol(symbol.AssociatedSymbol));
                }
            }

            return result;
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol, Project project, IImmutableSet<Document> documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            // First, find any documents with the full name of the accessor (i.e. get_Goo).
            // This will find explicit calls to the method (which can happen when C# references
            // a VB parameterized property).
            var result = await FindDocumentsAsync(
                project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);

            if (symbol.AssociatedSymbol is IPropertySymbol property &&
                options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                // we want to associate normal property references with the specific accessor being
                // referenced.  So we also need to include documents with our property's name. Just
                // defer to the Property finder to find these docs and combine them with the result.
                var propertyDocuments = await ReferenceFinders.Property.DetermineDocumentsToSearchAsync(
                    property, project, documents,
                    options.WithAssociatePropertyReferencesWithSpecificAccessor(false),
                    cancellationToken).ConfigureAwait(false);

                result = result.AddRange(propertyDocuments);
            }

            return result;
        }

        protected override async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            var references = await FindReferencesInDocumentUsingSymbolNameAsync(
                symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            if (symbol.AssociatedSymbol is IPropertySymbol property &&
                options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                var propertyReferences = await ReferenceFinders.Property.FindReferencesInDocumentAsync(
                    new SymbolAndProjectId(property, document.Project.Id), document, semanticModel,
                    options.WithAssociatePropertyReferencesWithSpecificAccessor(false),
                    cancellationToken).ConfigureAwait(false);

                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

                var accessorReferences = propertyReferences.WhereAsArray(
                    loc =>
                    {
                        var accessors = GetReferencedAccessorSymbols(
                            syntaxFacts, semanticFacts, semanticModel, property, loc.Node, cancellationToken);
                        return accessors.Contains(symbol);
                    });

                references = references.AddRange(accessorReferences);
            }

            return references;
        }
    }
}
