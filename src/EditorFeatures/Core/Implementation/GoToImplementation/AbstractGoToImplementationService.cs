// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToImplementation
{
    abstract class AbstractGoToImplementationService : IGoToImplementationService
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
                var solution = document.Project.Solution;
                var implementations =
                    SymbolFinder.FindImplementationsAsync(
                        symbol,
                        solution,
                        cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken).ToList();

                if (implementations.Count == 0)
                {
                    message = EditorFeaturesResources.SymbolHasNoImplementations;
                    return false;
                }
                else if (implementations.Count == 1)
                {
                    GoToDefinition.GoToDefinitionHelpers.TryGoToDefinition(implementations.Single(), document.Project, _navigableItemPresenters, cancellationToken);
                    message = null;
                    return true;
                }
                else
                {
                    // We have multiple symbols, so we'll build a list of all preferred locations for all the symbols
                    var navigableItems = implementations.SelectMany(
                        implementation => CreateItemsForImplementation(implementation, solution));

                    var presenter = _navigableItemPresenters.First();
                    presenter.Value.DisplayResult(NavigableItemFactory.GetSymbolDisplayString(document.Project, symbol), navigableItems);
                }

                message = null;
                return true;
            }
            else
            {
                message = EditorFeaturesResources.CannotNavigateToTheSymbol;
                return false;
            }
        }

        private static IEnumerable<INavigableItem> CreateItemsForImplementation(ISymbol implementation, Solution solution)
        {
            var symbolDisplayService = solution.Workspace.Services.GetLanguageServices(implementation.Language).GetRequiredService<ISymbolDisplayService>();

            return NavigableItemFactory.GetItemsFromPreferredSourceLocations(solution, implementation,
                                        displayString: symbolDisplayService.ToDisplayString(implementation));
        }

    }
}
