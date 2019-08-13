// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    // GoToDefinition
    internal abstract class AbstractGoToDefinitionService : IGoToDefinitionService
    {
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;

        protected AbstractGoToDefinitionService(IStreamingFindUsagesPresenter streamingPresenter)
        {
            _streamingPresenter = streamingPresenter;
        }

        public async Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var symbolService = document.GetLanguageService<IGoToDefinitionSymbolService>();
            var (symbol, span) = await symbolService.GetSymbolAndBoundSpanAsync(document, position, includeType: true, cancellationToken).ConfigureAwait(false);

            // Try to compute source definitions from symbol.
            var items = symbol != null
                ? NavigableItemFactory.GetItemsFromPreferredSourceLocations(document.Project.Solution, symbol, displayTaggedParts: null, cancellationToken: cancellationToken)
                : null;

            // realize the list here so that the consumer await'ing the result doesn't lazily cause
            // them to be created on an inappropriate thread.
            return items?.ToList();
        }

        public bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken)
        {
            // Try to compute the referenced symbol and attempt to go to definition for the symbol.
            var symbolService = document.GetLanguageService<IGoToDefinitionSymbolService>();
            var (symbol, _) = symbolService.GetSymbolAndBoundSpanAsync(document, position, includeType: true, cancellationToken).WaitAndGetResult(cancellationToken);
            if (symbol is null)
            {
                return false;
            }

            var isThirdPartyNavigationAllowed = IsThirdPartyNavigationAllowed(symbol, position, document, cancellationToken);

            return GoToDefinitionHelpers.TryGoToDefinition(symbol,
                document.Project,
                _streamingPresenter,
                thirdPartyNavigationAllowed: isThirdPartyNavigationAllowed,
                throwOnHiddenDefinition: true,
                cancellationToken: cancellationToken);
        }

        private static bool IsThirdPartyNavigationAllowed(ISymbol symbolToNavigateTo, int caretPosition, Document document, CancellationToken cancellationToken)
        {
            var syntaxRoot = document.GetSyntaxRootSynchronously(cancellationToken);
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var containingTypeDeclaration = syntaxFactsService.GetContainingTypeDeclaration(syntaxRoot, caretPosition);

            if (containingTypeDeclaration != null)
            {
                var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                // Allow third parties to navigate to all symbols except types/constructors
                // if we are navigating from the corresponding type.

                if (semanticModel.GetDeclaredSymbol(containingTypeDeclaration, cancellationToken) is ITypeSymbol containingTypeSymbol &&
                    (symbolToNavigateTo is ITypeSymbol || symbolToNavigateTo.IsConstructor()))
                {
                    var candidateTypeSymbol = symbolToNavigateTo is ITypeSymbol
                        ? symbolToNavigateTo
                        : symbolToNavigateTo.ContainingType;

                    if (Equals(containingTypeSymbol, candidateTypeSymbol))
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
