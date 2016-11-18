// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
{
    internal static class GoToDefinitionHelpers
    {
        public static bool TryGoToDefinition(
            ISymbol symbol,
            Project project,
            IEnumerable<Lazy<INavigableItemsPresenter>> presenters,
            IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true,
            bool throwOnHiddenDefinition = false)
        {
            var alias = symbol as IAliasSymbol;
            if (alias != null)
            {
                var ns = alias.Target as INamespaceSymbol;
                if (ns != null && ns.IsGlobalNamespace)
                {
                    return false;
                }
            }

            // VB global import aliases have a synthesized SyntaxTree.
            // We can't go to the definition of the alias, so use the target type.

            var solution = project.Solution;
            if (symbol is IAliasSymbol &&
                NavigableItemFactory.GetPreferredSourceLocations(solution, symbol).All(l => project.Solution.GetDocument(l.SourceTree) == null))
            {
                symbol = ((IAliasSymbol)symbol).Target;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).WaitAndGetResult(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            symbol = definition ?? symbol;

            if (thirdPartyNavigationAllowed && TryThirdPartyNavigation(symbol, solution))
            {
                return true;
            }

            // If it is a partial method declaration with no body, choose to go to the implementation
            // that has a method body.
            if (symbol is IMethodSymbol)
            {
                symbol = ((IMethodSymbol)symbol).PartialImplementationPart ?? symbol;
            }

            var options = project.Solution.Options;

            var preferredSourceLocations = NavigableItemFactory.GetPreferredSourceLocations(solution, symbol).ToArray();
            var displayParts = NavigableItemFactory.GetSymbolDisplayTaggedParts(project, symbol);
            var title = displayParts.JoinText();

            if (preferredSourceLocations.Length == 0)
            {
                // If there are no visible source locations, then tell the host about the symbol and 
                // allow it to navigate to it.  This will either navigate to any non-visible source
                // locations, or it can appropriately deal with metadata symbols for hosts that can go 
                // to a metadata-as-source view.

                var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();
                return symbolNavigationService.TryNavigateToSymbol(
                    symbol, project,
                    options: options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true),
                    cancellationToken: cancellationToken);
            }
            else if (preferredSourceLocations.Length == 1)
            {
                var item = NavigableItemFactory.GetItemFromSymbolLocation(
                        solution, symbol, preferredSourceLocations[0],
                        displayTaggedParts: null);
                return TryGoToSingleLocation(item, options, throwOnHiddenDefinition);
            }
            else
            {
                // We have multiple viable source locations, so ask the host what to do. Most hosts
                // will simply display the results to the user and allow them to choose where to 
                // go.

                return TryPresentInFindUsagesPresenter(solution, symbol, streamingPresenters, cancellationToken) ||
                       TryPresentInNavigableItemsPresenter(solution, symbol, presenters, title, preferredSourceLocations);
            }
        }

        private static bool TryPresentInFindUsagesPresenter(
            Solution solution, ISymbol symbol, 
            IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            CancellationToken cancellationToken)
        {
            var presenter = GetFindUsagesPresenter(streamingPresenters);
            if (presenter == null)
            {
                return false;
            }

            var definition = symbol.ToDefinitionItem(solution);
            var context = presenter.StartSearch(EditorFeaturesResources.Go_to_Definition);
            try
            {
                context.OnDefinitionFoundAsync(definition).Wait(cancellationToken);
            }
            finally
            {
                context.OnCompletedAsync().Wait(cancellationToken);
            }

            return true;
        }

        private static IStreamingFindUsagesPresenter GetFindUsagesPresenter(
            IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters)
        {
            try
            {
                return streamingPresenters.FirstOrDefault()?.Value;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryPresentInNavigableItemsPresenter(
            Solution solution, ISymbol symbol,
            IEnumerable<Lazy<INavigableItemsPresenter>> presenters, 
            string title, Location[] locations)
        {
            var presenter = presenters.FirstOrDefault();
            if (presenter != null)
            {
                var navigableItems = locations.Select(location =>
                    NavigableItemFactory.GetItemFromSymbolLocation(
                        solution, symbol, location,
                        displayTaggedParts: null)).ToImmutableArray();

                presenter.Value.DisplayResult(title, navigableItems);
                return true;
            }

            return false;
        }

        private static bool TryGoToSingleLocation(
            INavigableItem navigableItem, OptionSet options, bool throwOnHiddenDefinition)
        {
            var firstItem = navigableItem;
            var workspace = firstItem.Document.Project.Solution.Workspace;
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

            if (navigationService.CanNavigateToSpan(workspace, firstItem.Document.Id, firstItem.SourceSpan))
            {
                return navigationService.TryNavigateToSpan(
                    workspace,
                    documentId: firstItem.Document.Id,
                    textSpan: firstItem.SourceSpan,
                    options: options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true));
            }
            else
            {
                if (throwOnHiddenDefinition)
                {
                    const int E_FAIL = -2147467259;
                    throw new COMException(EditorFeaturesResources.The_definition_of_the_object_is_hidden, E_FAIL);
                }
                else
                {
                    return false;
                }
            }
        }

        private static bool TryThirdPartyNavigation(ISymbol symbol, Solution solution)
        {
            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();

            // Notify of navigation so third parties can intercept the navigation
            return symbolNavigationService.TrySymbolNavigationNotify(symbol, solution);
        }
    }
}