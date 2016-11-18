// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindReferences
{
    internal abstract partial class AbstractFindReferencesService :
        ForegroundThreadAffinitizedObject, IFindReferencesService, IStreamingFindReferencesService
    {
        private readonly IEnumerable<IDefinitionsAndReferencesPresenter> _referenceSymbolPresenters;
        private readonly IEnumerable<INavigableItemsPresenter> _navigableItemPresenters;

        protected AbstractFindReferencesService(
            IEnumerable<IDefinitionsAndReferencesPresenter> referenceSymbolPresenters,
            IEnumerable<INavigableItemsPresenter> navigableItemPresenters)
        {
            _referenceSymbolPresenters = referenceSymbolPresenters;
            _navigableItemPresenters = navigableItemPresenters;
        }

        private async Task<Tuple<IEnumerable<ReferencedSymbol>, Solution>> FindReferencedSymbolsAsync(
            Document document, int position, IWaitContext waitContext)
        {
            var cancellationToken = waitContext.CancellationToken;

            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
            {
                return null;
            }

            var symbol = symbolAndProject?.symbol;
            var project = symbolAndProject?.project;

            var displayName = GetDisplayName(symbol);

            waitContext.Message = string.Format(
                EditorFeaturesResources.Finding_references_of_0, displayName);

            var result = await SymbolFinder.FindReferencesAsync(symbol, project.Solution, cancellationToken).ConfigureAwait(false);

            return Tuple.Create(result, project.Solution);
        }

        private static string GetDisplayName(ISymbol symbol)
        {
            return symbol.IsConstructor() ? symbol.ContainingType.Name : symbol.Name;
        }

        public bool TryFindReferences(Document document, int position, IWaitContext waitContext)
        {
            var cancellationToken = waitContext.CancellationToken;

            var result = this.FindReferencedSymbolsAsync(document, position, waitContext).WaitAndGetResult(cancellationToken);
            return TryDisplayReferences(result);
        }

        private bool TryDisplayReferences(IEnumerable<INavigableItem> result)
        {
            if (result != null && result.Any())
            {
                var title = result.First().DisplayTaggedParts.JoinText();
                foreach (var presenter in _navigableItemPresenters)
                {
                    presenter.DisplayResult(title, result);
                    return true;
                }
            }

            return false;
        }

        private bool TryDisplayReferences(Tuple<IEnumerable<ReferencedSymbol>, Solution> result)
        {
            if (result != null && result.Item1 != null)
            {
                var solution = result.Item2;
                var factory = solution.Workspace.Services.GetService<IDefinitionsAndReferencesFactory>();
                var definitionsAndReferences = factory.CreateDefinitionsAndReferences(
                    solution, result.Item1);

                foreach (var presenter in _referenceSymbolPresenters)
                {
                    presenter.DisplayResult(definitionsAndReferences);
                    return true;
                }
            }

            return false;
        }

        public async Task FindReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            // NOTE: All ConFigureAwaits in this method need to pass 'true' so that
            // we return to the caller's context.  that's so the call to 
            // CallThirdPartyExtensionsAsync will happen on the UI thread.  We need
            // this to maintain the threading guarantee we had around that method
            // from pre-Roslyn days.
            var findReferencesProgress = await FindReferencesWorkerAsync(
                document, position, context).ConfigureAwait(true);
            if (findReferencesProgress == null)
            {
                return;
            }

            // After the FAR engine is done call into any third party extensions to see
            // if they want to add results.
            await findReferencesProgress.CallThirdPartyExtensionsAsync().ConfigureAwait(true);
        }

        private async Task<ProgressAdapter> FindReferencesWorkerAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            // Find the symbol we want to search and the solution we want to search in.
            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
            {
                return null;
            }

            var symbol = symbolAndProject?.symbol;
            var project = symbolAndProject?.project;

            var displayName = GetDisplayName(symbol);
            context.SetSearchLabel(displayName);

            var progressAdapter = new ProgressAdapter(project.Solution, context);

            // Now call into the underlying FAR engine to find reference.  The FAR
            // engine will push results into the 'progress' instance passed into it.
            // We'll take those results, massage them, and forward them along to the 
            // FindReferencesContext instance we were given.
            await SymbolFinder.FindReferencesAsync(
                SymbolAndProjectId.Create(symbol, project.Id),
                project.Solution,
                progressAdapter,
                documents: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return progressAdapter;
        }
    }
}