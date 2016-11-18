// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToImplementation
{
    internal abstract class AbstractGoToImplementationService : 
        IGoToImplementationService, IStreamingFindImplementationsService
    {
        private readonly IEnumerable<Lazy<INavigableItemsPresenter>> _navigableItemPresenters;

        public AbstractGoToImplementationService(
            IEnumerable<Lazy<INavigableItemsPresenter>> navigableItemPresenters)
        {
            _navigableItemPresenters = navigableItemPresenters;
        }

        public bool TryGoToImplementation(Document document, int position, CancellationToken cancellationToken, out string message)
        {
            var result = this.FindImplementationsAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken);
            if (result == null)
            {
                message = EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret;
                return false;
            }

            if (result.Value.message != null)
            {
                message = result.Value.message;
                return false;
            }

            return TryGoToImplementations(
                result.Value.symbol, result.Value.project,
                result.Value.implementations, cancellationToken, out message);
        }

        public async Task<(ISymbol symbol, Project project, ImmutableArray<ISymbol> implementations, string message)?> FindImplementationsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
            {
                return null;
            }

            return await FindImplementationsAsync(
                symbolAndProject.Item1, symbolAndProject.Item2, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(ISymbol symbol, Project project, ImmutableArray<ISymbol> implementations, string message)?> FindImplementationsAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var implementations = await FindImplementationsWorkerAsync(
                symbol, project, cancellationToken).ConfigureAwait(false);

            var filteredSymbols = implementations.WhereAsArray(
                s => !s.IsAbstract && s.Locations.Any(l => l.IsInSource));

            return filteredSymbols.Length == 0
                ? (symbol, project, filteredSymbols, EditorFeaturesResources.The_symbol_has_no_implementations)
                : (symbol, project, filteredSymbols, null);
        }

        private async Task<ImmutableArray<ISymbol>> FindImplementationsWorkerAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            if (symbol.IsInterfaceType() || symbol.IsImplementableMember())
            {
                var implementations = await SymbolFinder.FindImplementationsAsync(
                    symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

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
                        var overrides = await SymbolFinder.FindOverridesAsync(
                            implementation, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                        implementationsAndOverrides.AddRange(overrides);
                    }
                }

                return implementationsAndOverrides.ToImmutableArray();
            }
            else if ((symbol as INamedTypeSymbol)?.TypeKind == TypeKind.Class)
            {
                var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                    (INamedTypeSymbol)symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                var implementations = derivedClasses.Concat(symbol);

                return implementations.ToImmutableArray();
            }
            else if (symbol.IsOverridable())
            {
                var overrides = await SymbolFinder.FindOverridesAsync(
                    symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                var implementations = overrides.Concat(symbol);

                return implementations.ToImmutableArray();
            }
            else
            {
                // This is something boring like a regular method or type, so we'll just go there directly
                return ImmutableArray.Create(symbol);
            }
        }

        private bool TryGoToImplementations(
            ISymbol symbol, Project project, ImmutableArray<ISymbol> implementations, CancellationToken cancellationToken, out string message)
        {
            if (implementations.Length == 0)
            {
                message = EditorFeaturesResources.The_symbol_has_no_implementations;
                return false;
            }
            else if (implementations.Length == 1)
            {
                GoToDefinitionHelpers.TryGoToDefinition(
                    implementations.Single(), project, _navigableItemPresenters,  
                    SpecializedCollections.EmptyEnumerable<Lazy<IStreamingFindUsagesPresenter>>(),
                    cancellationToken);
                message = null;
                return true;
            }
            else
            {
                return TryPresentInNavigableItemsPresenter(symbol, project, implementations, out message);
            }
        }

        private bool TryPresentInNavigableItemsPresenter(
            ISymbol symbol, Project project, ImmutableArray<ISymbol> implementations, out string message)
        {
            // We have multiple symbols, so we'll build a list of all preferred locations for all the symbols
            var navigableItems = implementations.SelectMany(
                implementation => CreateItemsForImplementation(implementation, project.Solution));

            var presenter = _navigableItemPresenters.First();

            var taggedParts = NavigableItemFactory.GetSymbolDisplayTaggedParts(project, symbol);

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

        public async Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
        {
            var tuple = await FindImplementationsAsync(
                document, position, context.CancellationToken).ConfigureAwait(false);
            if (tuple == null)
            {
                context.ReportMessage(EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret);
                return;
            }

            var message = tuple.Value.message;

            if (message != null)
            {
                context.ReportMessage(message);
                return;
            }

            var project = tuple.Value.project;

            foreach (var implementation in tuple.Value.implementations)
            {
                var definitionItem = implementation.ToDefinitionItem(project.Solution);
                await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }
        }
    }
}