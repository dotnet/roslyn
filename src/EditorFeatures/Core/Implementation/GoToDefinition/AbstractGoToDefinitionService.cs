// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
{
    internal abstract class AbstractGoToDefinitionService : IGoToDefinitionService
    {
        private readonly IEnumerable<Lazy<INavigableItemsPresenter>> _presenters;
        private readonly IEnumerable<Lazy<INavigableDefinitionProvider>> _externalDefinitionProviders;

        protected abstract ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation);

        protected AbstractGoToDefinitionService(IEnumerable<Lazy<INavigableItemsPresenter>> presenters, IEnumerable<Lazy<INavigableDefinitionProvider>> externalDefinitionProviders)
        {
            _presenters = presenters;
            _externalDefinitionProviders = externalDefinitionProviders;
        }

        private async Task<ISymbol> FindSymbolAsync(Document document, int position, CancellationToken cancellationToken)
        {
            if (!document.SupportsSemanticModel)
            {
                return null;
            }

            var workspace = document.Project.Solution.Workspace;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, workspace, bindLiteralsToUnderlyingType: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            return FindRelatedExplicitlyDeclaredSymbol(symbol, semanticModel.Compilation);
        }

        public async Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var symbol = await FindSymbolAsync(document, position, cancellationToken).ConfigureAwait(false);

            // Try to compute source definitions from symbol.
            var items = symbol != null ? NavigableItemFactory.GetItemsFromPreferredSourceLocations(document.Project.Solution, symbol) : null;
            if (items == null || items.IsEmpty())
            {
                // Fallback to asking the navigation definition providers for navigable definition locations.
                items = await GoToDefinitionHelpers.FindExternalDefinitionsAsync(document, position, _externalDefinitionProviders, cancellationToken).ConfigureAwait(false);
            }

            // realize the list here so that the consumer await'ing the result doesn't lazily cause
            // them to be created on an inappropriate thread.
            return items?.ToList();
        }

        public bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken)
        {
            // First try to compute the referenced symbol and attempt to go to definition for the symbol.
            var symbol = FindSymbolAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken);
            if (symbol != null)
            {
                var isThirdPartyNavigationAllowed = IsThirdPartyNavigationAllowed(symbol, position, document, cancellationToken);

                return GoToDefinitionHelpers.TryGoToDefinition(symbol,
                    document.Project,
                    _externalDefinitionProviders,
                    _presenters,
                    thirdPartyNavigationAllowed: isThirdPartyNavigationAllowed,
                    throwOnHiddenDefinition: true,
                    cancellationToken: cancellationToken);
            }

            // Otherwise, fallback to the external navigation definition providers.
            return GoToDefinitionHelpers.TryExternalGoToDefinition(document, position, _externalDefinitionProviders, _presenters, cancellationToken);
        }

        private static bool IsThirdPartyNavigationAllowed(ISymbol symbolToNavigateTo, int caretPosition, Document document, CancellationToken cancellationToken)
        {
            var syntaxRoot = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var containingTypeDeclaration = syntaxFactsService.GetContainingTypeDeclaration(syntaxRoot, caretPosition);

            if (containingTypeDeclaration != null)
            {
                var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                var containingTypeSymbol = semanticModel.GetDeclaredSymbol(containingTypeDeclaration, cancellationToken) as ITypeSymbol;

                // Allow third parties to navigate to all symbols except types/constructors
                // if we are navigating from the corresponding type.

                if (containingTypeSymbol != null &&
                    (symbolToNavigateTo is ITypeSymbol || symbolToNavigateTo.IsConstructor()))
                {
                    var candidateTypeSymbol = symbolToNavigateTo is ITypeSymbol
                        ? symbolToNavigateTo
                        : symbolToNavigateTo.ContainingType;

                    if (containingTypeSymbol == candidateTypeSymbol)
                    {
                        // We are navigating from the same type, so don't allow third parties to perform the navigation.
                        // This ensures that if we navigate to a class from within that class, we'll stay in the same file
                        // rather than navigate to, say, XAML.
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
