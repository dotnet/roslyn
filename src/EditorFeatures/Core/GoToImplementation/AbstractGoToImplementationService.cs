// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToImplementation
{
    internal abstract class AbstractGoToImplementationService : IGoToImplementationService
    {
        private readonly IEnumerable<Lazy<INavigableItemsPresenter>> _navigableItemPresenters;
        private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _streamingPresenters;

        public AbstractGoToImplementationService(
            IEnumerable<Lazy<INavigableItemsPresenter>> navigableItemPresenters,
            IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters)
        {
            _navigableItemPresenters = navigableItemPresenters;
            _streamingPresenters = streamingPresenters;
        }

        public bool TryGoToImplementation(Document document, int position, CancellationToken cancellationToken, out string message)
        {
            var symbol = SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken);
            if (symbol != null)
            {
                // Map the symbol if necessary back to the originating workspace if we're invoking from something
                // like metadata as source
                var mappingService = document.Project.Solution.Workspace.Services.GetRequiredService<ISymbolMappingService>();
                var mapping = mappingService.MapSymbolAsync(document, symbol, cancellationToken).WaitAndGetResult(cancellationToken);

                if (mapping != null)
                {
                    return TryGoToImplementationOnMappedSymbol(mapping, cancellationToken, out message);
                }
            }

            message = EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret;
            return false;
        }

        private bool TryGoToImplementationOnMappedSymbol(SymbolMappingResult mapping, CancellationToken cancellationToken, out string message)
        {
            if (mapping.Symbol.IsInterfaceType() || mapping.Symbol.IsImplementableMember())
            {
                var implementations =
                    SymbolFinder.FindImplementationsAsync(mapping.Symbol, mapping.Solution, cancellationToken: cancellationToken)
                        .WaitAndGetResult(cancellationToken);

                // It's important we use a HashSet here -- we may have cases in an inheritence hierarchy where more than one method
                // in an overrides chain implements the same interface method, and we want to duplicate those. The easiest way to do it
                // is to just use a HashSet.
                var implementationsAndOverrides = new HashSet<ISymbol>();

                foreach (var implementation in implementations)
                {
                    implementationsAndOverrides.Add(implementation);

                    // FindImplementationsAsync will only return the base virtual/abstract method, not that method and the overrides
                    // of the method. We should also include those.
                    if (implementation.IsOverridable())
                    {
                        implementationsAndOverrides.AddRange(
                            SymbolFinder.FindOverridesAsync(implementation, mapping.Solution, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken));
                    }
                }

                return TryGoToImplementations(implementationsAndOverrides, mapping, cancellationToken, out message);
            }
            else if ((mapping.Symbol as INamedTypeSymbol)?.TypeKind == TypeKind.Class)
            {
                var implementations =
                    SymbolFinder.FindDerivedClassesAsync((INamedTypeSymbol)mapping.Symbol, mapping.Solution, cancellationToken: cancellationToken)
                        .WaitAndGetResult(cancellationToken)
                        .Concat(mapping.Symbol);

                return TryGoToImplementations(implementations, mapping, cancellationToken, out message);
            }
            else if (mapping.Symbol.IsOverridable())
            {
                var implementations =
                    SymbolFinder.FindOverridesAsync(mapping.Symbol, mapping.Solution, cancellationToken: cancellationToken)
                        .WaitAndGetResult(cancellationToken)
                        .Concat(mapping.Symbol);

                return TryGoToImplementations(implementations, mapping, cancellationToken, out message);
            }
            else
            {
                // This is something boring like a regular method or type, so we'll just go there directly
                if (GoToDefinitionHelpers.TryGoToDefinition(
                        mapping.Symbol, mapping.Project,
                        _navigableItemPresenters, _streamingPresenters, cancellationToken))
                {
                    message = null;
                    return true;
                }

                message = EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret;
                return false;
            }
        }

        private bool TryGoToImplementations(IEnumerable<ISymbol> candidateImplementations, SymbolMappingResult mapping, CancellationToken cancellationToken, out string message)
        {
            var implementations = candidateImplementations
                .Where(s => !s.IsAbstract && s.Locations.Any(l => l.IsInSource))
                .ToList();

            if (implementations.Count == 0)
            {
                message = EditorFeaturesResources.The_symbol_has_no_implementations;
                return false;
            }
            else if (implementations.Count == 1)
            {
                GoToDefinitionHelpers.TryGoToDefinition(
                    implementations.Single(), mapping.Project, 
                    _navigableItemPresenters, _streamingPresenters, cancellationToken);
                message = null;
                return true;
            }
            else
            {
                return TryPresentInFindUsagesPresenter(mapping, implementations, cancellationToken, out message) ||
                       TryPresentInNavigableItemsPresenter(mapping, implementations, out message);
            }
        }

        private bool TryPresentInFindUsagesPresenter(
            SymbolMappingResult mapping, List<ISymbol> implementations, CancellationToken cancellationToken, out string message)
        {
            message = null;

            var presenter = GetFindUsagesPresenter();
            if (presenter == null)
            {
                return false;
            }

            var definitionItems = implementations.Select(s =>
                s.ToDefinitionItem(mapping.Solution)).ToImmutableArrayOrEmpty();

            var context = presenter.StartSearch(EditorFeaturesResources.Go_To_Implementation);
            try
            {
                foreach (var item in definitionItems)
                {
                    context.OnDefinitionFoundAsync(item).Wait(cancellationToken);
                }
            }
            finally
            {
                context.OnCompletedAsync().Wait(cancellationToken);
            }

            return true;
        }

        private IStreamingFindUsagesPresenter GetFindUsagesPresenter()
        {
            try
            {
                return _streamingPresenters.FirstOrDefault()?.Value;
            }
            catch
            {
                return null;
            }
        }

        private bool TryPresentInNavigableItemsPresenter(
            SymbolMappingResult mapping, List<ISymbol> implementations, out string message)
        {
            // We have multiple symbols, so we'll build a list of all preferred locations for all the symbols
            var navigableItems = implementations.SelectMany(
                implementation => CreateItemsForImplementation(implementation, mapping.Solution));

            var presenter = _navigableItemPresenters.First();

            var taggedParts = NavigableItemFactory.GetSymbolDisplayTaggedParts(mapping.Project, mapping.Symbol);

            presenter.Value.DisplayResult(taggedParts.JoinText(), navigableItems);
            message = null;
            return true;
        }

        private static IEnumerable<INavigableItem> CreateItemsForImplementation(ISymbol implementation, Solution solution)
        {
            var symbolDisplayService = solution.Workspace.Services.GetLanguageServices(implementation.Language).GetRequiredService<ISymbolDisplayService>();

            return NavigableItemFactory.GetItemsFromPreferredSourceLocations(
                solution,
                implementation,
                displayTaggedParts: symbolDisplayService.ToDisplayParts(implementation).ToTaggedText());
        }
    }
}