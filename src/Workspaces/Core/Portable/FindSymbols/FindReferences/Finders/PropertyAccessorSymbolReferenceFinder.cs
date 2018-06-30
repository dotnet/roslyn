// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class PropertyAccessorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
        {
            return symbol.MethodKind.IsPropertyAccessor();
        }

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IMethodSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects, 
            SymbolFinderOptions options,
            CancellationToken cancellationToken)
        {
            var result = await base.DetermineCascadedSymbolsAsync(
                symbolAndProjectId, solution, projects, options, cancellationToken).ConfigureAwait(false);

            if ((options ?? SymbolFinderOptions.Default).SearchAccessorsAsContainingMember)
            {
                var symbol = symbolAndProjectId.Symbol;
                if (symbol.AssociatedSymbol != null)
                {
                    result = result.Add(symbolAndProjectId.WithSymbol(symbol.AssociatedSymbol));
                }
            }

            return result;
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(IMethodSymbol symbol, Project project, IImmutableSet<Document> documents, SymbolFinderOptions options, CancellationToken cancellationToken)
        {
            var namesToIncludeInSearch =
                options.IncludeImplicitAccessorUsages && symbol.AssociatedSymbol is IPropertySymbol property
                    ? new[] { symbol.Name, property.Name }
                    : new[] { symbol.Name };

            return FindDocumentsAsync(
                project,
                documents,
                documentIndex => namesToIncludeInSearch.Any(documentIndex.ProbablyContainsIdentifier),
                cancellationToken);
        }

        protected override async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(IMethodSymbol symbol, Document document, SemanticModel semanticModel, SymbolFinderOptions options, CancellationToken cancellationToken)
        {
            var explicitReferences = await FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            var property = symbol.AssociatedSymbol as IPropertySymbol;
            if (property == null || !options.IncludeImplicitAccessorUsages)
            {
                return explicitReferences;
            }

            // Picks up whatever an IPropertySymbol search would find, e.g. foreach and element access.
            // Each of these is a candidate implicit reference.
            var propertyReferences = await ReferenceFinders.Property.FindReferencesInDocumentAsync(
                SymbolAndProjectId.Create(property, document.Project.Id),
                document,
                semanticModel,
                options,
                cancellationToken).ConfigureAwait(false);

            var results = ImmutableArray.CreateBuilder<ReferenceLocation>();
            results.AddRange(explicitReferences);

            var semanticFactsService = document.GetLanguageService<ISemanticFactsService>();
            var isSetterSearch = symbol == property.SetMethod;

            foreach (var propertyReference in propertyReferences)
            {
                if (isSetterSearch) 
                {
                    if (!propertyReference.IsWrittenTo)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!IsPropertyReadFrom(propertyReference, semanticModel, semanticFactsService, cancellationToken))
                    {
                        continue;
                    }
                }

                results.Add(new ReferenceLocation(
                    document,
                    propertyReference.Alias,
                    propertyReference.Location,
                    isImplicit: true,
                    propertyReference.IsWrittenTo,
                    propertyReference.CandidateReason));
            }

            return results.ToImmutable();
        }

        private static bool IsPropertyReadFrom(ReferenceLocation propertyReference, SemanticModel semanticModel, ISemanticFactsService semanticFactsService, CancellationToken cancellationToken)
        {
            if (propertyReference.IsWrittenTo)
            {
                // If it’s not only written to, it’s read from.
                return !semanticFactsService.IsOnlyWrittenTo(semanticModel, propertyReference.Location.FindNode(cancellationToken), cancellationToken);
            }
            else
            {
                // If it’s a nameof expression, neither accessor is involved.
                return !semanticFactsService.IsNameOfContext(semanticModel, propertyReference.Location.SourceSpan.Start, cancellationToken);
            }
        }
    }
}
