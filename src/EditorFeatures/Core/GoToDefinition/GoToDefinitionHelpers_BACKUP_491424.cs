// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    internal static class GoToDefinitionHelpers
    {
        public static ImmutableArray<DefinitionItem> GetDefinitions(
            ISymbol symbol,
            Project project,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true)
        {
            var alias = symbol as IAliasSymbol;
            if (alias != null)
            {
                var ns = alias.Target as INamespaceSymbol;
                if (ns != null && ns.IsGlobalNamespace)
                {
                    return default(ImmutableArray<DefinitionItem>);
                }
            }

            // VB global import aliases have a synthesized SyntaxTree.
            // We can't go to the definition of the alias, so use the target type.

            var solution = project.Solution;
            if (alias != null)
            {
                var sourceLocations = NavigableItemFactory.GetPreferredSourceLocations(
                    solution, symbol, cancellationToken);

                if (sourceLocations.All(l => project.Solution.GetDocument(l.SourceTree) == null))
                {
                    symbol = alias.Target;
                }
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).WaitAndGetResult(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            symbol = definition ?? symbol;

<<<<<<< HEAD
=======
            var definitions = ImmutableArray.CreateBuilder<DefinitionItem>();
            if (thirdPartyNavigationAllowed)
            {
                var factory = solution.Workspace.Services.GetService<IDefinitionsAndReferencesFactory>();
                var thirdPartyItem = factory?.GetThirdPartyDefinitionItem(solution, symbol, cancellationToken);
                if (thirdPartyItem != null)
                {
                    definitions.Add(thirdPartyItem);
                }
            }

>>>>>>> Add a service for creating the DefinitionItems consumed by GTD
            // If it is a partial method declaration with no body, choose to go to the implementation
            // that has a method body.
            if (symbol is IMethodSymbol method)
            {
                symbol = method.PartialImplementationPart ?? symbol;
            }

            var definitions = ArrayBuilder<DefinitionItem>.GetInstance();
            var definitionItem = symbol.ToClassifiedDefinitionItemAsync(
                solution, includeHiddenLocations: true, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

<<<<<<< HEAD
            if (thirdPartyNavigationAllowed)
            {
                var factory = solution.Workspace.Services.GetService<IDefinitionsAndReferencesFactory>();
                var thirdPartyItem = factory?.GetThirdPartyDefinitionItem(solution, definitionItem, cancellationToken);
                definitions.AddIfNotNull(thirdPartyItem);
            }

            definitions.Add(definitionItem);

            var presenter = streamingPresenters.FirstOrDefault()?.Value;
=======
            definitions.Add(symbol.ToDefinitionItem(solution, includeHiddenLocations: true));
            return definitions.ToImmutable();
        }

        public static bool TryGoToDefinition(
            ISymbol symbol,
            Project project,
            IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true,
            bool throwOnHiddenDefinition = false)
        {
            var definitions = GetDefinitions(symbol, project, cancellationToken, thirdPartyNavigationAllowed);
            if (definitions == default(ImmutableArray<DefinitionItem>))
            {
                return false;
            }

>>>>>>> Add a service for creating the DefinitionItems consumed by GTD
            var title = string.Format(EditorFeaturesResources._0_declarations,
                FindUsagesHelpers.GetDisplayName(symbol));

            return TryGoToDefinition(definitions, title, streamingPresenters, cancellationToken);
        }

        public static bool TryGoToDefinition(
            ImmutableArray<DefinitionItem> definitions,
            string title,
            IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            CancellationToken cancellationToken)
        {
            if (definitions == default(ImmutableArray<DefinitionItem>))
            {
                return false;
            }

            var presenter = GetFindUsagesPresenter(streamingPresenters);

            return presenter.TryNavigateToOrPresentItemsAsync(
<<<<<<< HEAD
                project.Solution.Workspace, title, definitions.ToImmutableAndFree()).WaitAndGetResult(cancellationToken);
=======
                title, definitions).WaitAndGetResult(cancellationToken);
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

        private static bool TryThirdPartyNavigation(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();

            // Notify of navigation so third parties can intercept the navigation
            return symbolNavigationService.TrySymbolNavigationNotify(symbol, solution, cancellationToken);
>>>>>>> Add a service for creating the DefinitionItems consumed by GTD
        }
    }
}