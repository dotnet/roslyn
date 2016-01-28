// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToImplementation
{
    internal abstract class AbstractGoToImplementationService : IGoToImplementationService
    {
        private readonly IEnumerable<Lazy<INavigableItemsPresenter>> _navigableItemPresenters;

        public AbstractGoToImplementationService(IEnumerable<Lazy<INavigableItemsPresenter>> navigableItemPresenters)
        {
            _navigableItemPresenters = navigableItemPresenters;
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

            message = EditorFeaturesResources.CannotNavigateToTheSymbol;
            return false;
        }

        private bool TryGoToImplementationOnMappedSymbol(SymbolMappingResult mapping, CancellationToken cancellationToken, out string message)
        {
            if (mapping.Symbol.IsInterfaceType() || mapping.Symbol.IsImplementableMember())
            {
                var implementations =
                    SymbolFinder.FindImplementationsAsync(mapping.Symbol, mapping.Solution, cancellationToken: cancellationToken)
                        .WaitAndGetResult(cancellationToken)
                        .Where(s => s.Locations.Any(l => l.IsInSource))
                        .ToList();

                return TryGoToImplementations(mapping, implementations, cancellationToken, out message);
            }
            else if (mapping.Symbol.IsOverridable())
            {
                var overrides =
                    SymbolFinder.FindOverridesAsync(mapping.Symbol, mapping.Solution, cancellationToken: cancellationToken)
                        .WaitAndGetResult(cancellationToken)
                        .ToList();

                // If the original symbol isn't abstract, then it's an implementation too
                if (!mapping.Symbol.IsAbstract)
                {
                    overrides.Add(mapping.Symbol);
                }

                return TryGoToImplementations(mapping, overrides, cancellationToken, out message);
            }
            else
            {
                // This is something boring like a regular method or type, so we'll just go there directly
                if (GoToDefinition.GoToDefinitionHelpers.TryGoToDefinition(mapping.Symbol, mapping.Project, _navigableItemPresenters, cancellationToken))
                {
                    message = null;
                    return true;
                }
                else
                {
                    message = EditorFeaturesResources.CannotNavigateToTheSymbol;
                    return false;
                }
            }
        }

        private bool TryGoToImplementations(SymbolMappingResult mapping, IList<ISymbol> implementations, CancellationToken cancellationToken, out string message)
        {
            if (implementations.Count == 0)
            {
                message = EditorFeaturesResources.SymbolHasNoImplementations;
                return false;
            }
            else if (implementations.Count == 1)
            {
                GoToDefinition.GoToDefinitionHelpers.TryGoToDefinition(implementations.Single(), mapping.Project, _navigableItemPresenters, cancellationToken);
                message = null;
                return true;
            }
            else
            {
                // We have multiple symbols, so we'll build a list of all preferred locations for all the symbols
                var navigableItems = implementations.SelectMany(
                    implementation => CreateItemsForImplementation(implementation, mapping.Solution));

                var presenter = _navigableItemPresenters.First();
                presenter.Value.DisplayResult(NavigableItemFactory.GetSymbolDisplayString(mapping.Project, mapping.Symbol), navigableItems);
                message = null;
                return true;
            }
        }

        private static IEnumerable<INavigableItem> CreateItemsForImplementation(ISymbol implementation, Solution solution)
        {
            var symbolDisplayService = solution.Workspace.Services.GetLanguageServices(implementation.Language).GetRequiredService<ISymbolDisplayService>();

            return NavigableItemFactory.GetItemsFromPreferredSourceLocations(
                solution,
                implementation,
                displayString: symbolDisplayService.ToDisplayString(implementation));
        }
    }
}
