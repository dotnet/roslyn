// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
{
    internal static class GoToDefinitionHelpers
    {
        public static bool TryGoToDefinition(
            ISymbol symbol,
            Project project,
            IEnumerable<Lazy<INavigableItemsPresenter>> presenters,
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

            var options = project.Solution.Workspace.Options;

            var preferredSourceLocations = NavigableItemFactory.GetPreferredSourceLocations(solution, symbol).ToArray();
            if (!preferredSourceLocations.Any())
            {
                // If there are no visible source locations, then tell the host about the symbol and 
                // allow it to navigate to it.  THis will either navigate to any non-visible source
                // locations, or it can appropriately deal with metadata symbols for hosts that can go 
                // to a metadata-as-source view.

                var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();
                return symbolNavigationService.TryNavigateToSymbol(
                    symbol, project,
                    options: options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true),
                    cancellationToken: cancellationToken);
            }

            // If we have a single location, then just navigate to it.
            if (preferredSourceLocations.Length == 1)
            {
                var firstItem = preferredSourceLocations[0];
                var workspace = project.Solution.Workspace;
                var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

                if (navigationService.CanNavigateToSpan(workspace, solution.GetDocument(firstItem.SourceTree).Id, firstItem.SourceSpan))
                {
                    return navigationService.TryNavigateToSpan(
                        workspace,
                        documentId: solution.GetDocument(firstItem.SourceTree).Id,
                        textSpan: firstItem.SourceSpan,
                        options: options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true));
                }
                else
                {
                    if (throwOnHiddenDefinition)
                    {
                        const int E_FAIL = -2147467259;
                        throw new COMException(EditorFeaturesResources.TheDefinitionOfTheObjectIsHidden, E_FAIL);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                // We have multiple viable source locations, so ask the host what to do. Most hosts
                // will simply display the results to the user and allow them to choose where to 
                // go.

                if (presenters.Any())
                {
                    presenters.First().Value.DisplayResult(NavigableItemFactory.GetSymbolDisplayString(project, symbol),
                        preferredSourceLocations.Select(location => NavigableItemFactory.GetItemFromSymbolLocation(solution, symbol, location)).ToList());

                    return true;
                }

                return false;
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
