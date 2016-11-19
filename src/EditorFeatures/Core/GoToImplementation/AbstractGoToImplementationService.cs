// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToImplementation
{
    internal abstract class AbstractGoToImplementationService : IGoToImplementationService
    {
        private readonly IEnumerable<Lazy<INavigableItemsPresenter>> _navigableItemPresenters;

        public AbstractGoToImplementationService(
            IEnumerable<Lazy<INavigableItemsPresenter>> navigableItemPresenters)
        {
            _navigableItemPresenters = navigableItemPresenters;
        }

        public bool TryGoToImplementation(Document document, int position, CancellationToken cancellationToken, out string message)
        {
            var result = FindUsagesHelpers.FindImplementationsAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken);
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
    }
}