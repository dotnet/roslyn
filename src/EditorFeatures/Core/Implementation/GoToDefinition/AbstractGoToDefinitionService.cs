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

        protected abstract ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation);

        protected AbstractGoToDefinitionService(IEnumerable<Lazy<INavigableItemsPresenter>> presenters)
        {
            _presenters = presenters;
        }

        private async Task<ISymbol> FindSymbolAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, workspace, bindLiteralsToUnderlyingType: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            return FindRelatedExplicitlyDeclaredSymbol(symbol, semanticModel.Compilation);
        }

        public async Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var symbol = await FindSymbolAsync(document, position, cancellationToken).ConfigureAwait(false);

            // realize the list here so that the consumer await'ing the result doesn't lazily cause
            // them to be created on an inappropriate thread.
            return NavigableItemFactory.GetItemsFromPreferredSourceLocations(document.Project.Solution, symbol).ToList();
        }

        public bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken)
        {
            var symbol = FindSymbolAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken);

            if (symbol != null)
            {
                var isThirdPartyNavigationAllowed = IsThirdPartyNavigationAllowed(symbol, position, document, cancellationToken);

                if (GoToDefinitionHelpers.TryGoToDefinition(symbol,
                    document.Project,
                    _presenters,
                    thirdPartyNavigationAllowed: isThirdPartyNavigationAllowed,
                    throwOnHiddenDefinition: true,
                    cancellationToken: cancellationToken))
                {
                    return true;
                }
            }

            return false;
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
