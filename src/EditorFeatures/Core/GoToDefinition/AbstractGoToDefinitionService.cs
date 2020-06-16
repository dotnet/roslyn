// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    // GoToDefinition
    internal abstract class AbstractGoToDefinitionService : IGoToDefinitionService
    {
        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// Used to present go to definition results in <see cref="TryGoToDefinition(Document, int, CancellationToken)"/>
        /// This is lazily created as the LSP server only calls <see cref="FindDefinitionsAsync(Document, int, CancellationToken)"/>
        /// and therefore never needs to construct the presenter.
        /// </summary>
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

        protected AbstractGoToDefinitionService(
            IThreadingContext threadingContext,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
        }

        public async Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var symbolService = document.GetLanguageService<IGoToDefinitionSymbolService>();
            var (symbol, _) = await symbolService.GetSymbolAndBoundSpanAsync(document, position, includeType: true, cancellationToken).ConfigureAwait(false);

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
                return false;

            // if the symbol only has a single source location, and we're already on it,
            // try to see if there's a better symbol we could navigate to.
            var remapped = TryGoToAlternativeLocationIfAlreadyOnDefinition(document, position, symbol, cancellationToken);
            if (remapped)
                return true;

            var isThirdPartyNavigationAllowed = IsThirdPartyNavigationAllowed(symbol, position, document, cancellationToken);

            return GoToDefinitionHelpers.TryGoToDefinition(
                symbol,
                document.Project.Solution,
                _threadingContext,
                _streamingPresenter.Value,
                thirdPartyNavigationAllowed: isThirdPartyNavigationAllowed,
                cancellationToken: cancellationToken);
        }

        private bool TryGoToAlternativeLocationIfAlreadyOnDefinition(
            Document document, int position,
            ISymbol symbol, CancellationToken cancellationToken)
        {
            var project = document.Project;
            var solution = project.Solution;

            var sourceLocations = symbol.Locations.WhereAsArray(loc => loc.IsInSource);
            if (sourceLocations.Length != 1)
                return false;

            var definitionLocation = sourceLocations[0];
            if (!definitionLocation.SourceSpan.IntersectsWith(position))
                return false;

            var definitionTree = definitionLocation.SourceTree;
            var definitionDocument = solution.GetDocument(definitionTree);
            if (definitionDocument != document)
                return false;

            // Ok, we were already on the definition. Look for better symbols we could show results
            // for instead. For now, just see if we're on an interface member impl. If so, we can
            // instead navigate to the actual interface member.
            //
            // In the future we can expand this with other mappings if appropriate.
            var interfaceImpls = symbol.ExplicitOrImplicitInterfaceImplementations();
            if (interfaceImpls.Length == 0)
                return false;

            var definitions = interfaceImpls.SelectMany(
                i => GoToDefinitionHelpers.GetDefinitions(
                    i, solution, thirdPartyNavigationAllowed: false, cancellationToken)).ToImmutableArray();

            var title = string.Format(EditorFeaturesResources._0_implemented_members,
                FindUsagesHelpers.GetDisplayName(symbol));

            return _threadingContext.JoinableTaskFactory.Run(() =>
                _streamingPresenter.Value.TryNavigateToOrPresentItemsAsync(
                    _threadingContext, solution.Workspace, title, definitions));
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
