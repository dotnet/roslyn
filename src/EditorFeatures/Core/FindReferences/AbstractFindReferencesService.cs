// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols;
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

        /// <summary>
        /// Common helper for both the synchronous and streaming versions of FAR. 
        /// It returns the symbol we want to search for and the solution we should
        /// be searching.
        /// 
        /// Note that the <see cref="Solution"/> returned may absolutely *not* be
        /// the same as <code>document.Project.Solution</code>.  This is because 
        /// there may be symbol mapping involved (for example in Metadata-As-Source
        /// scenarios).
        /// </summary>
        private async Task<Tuple<ISymbol, Project>> GetRelevantSymbolAndProjectAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (symbol == null)
            {
                return null;
            }

            // If this document is not in the primary workspace, we may want to search for results
            // in a solution different from the one we started in. Use the starting workspace's
            // ISymbolMappingService to get a context for searching in the proper solution.
            var mappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();

            var mapping = await mappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
            if (mapping == null)
            {
                return null;
            }

            return Tuple.Create(mapping.Symbol, mapping.Project);
        }

        private async Task<Tuple<IEnumerable<ReferencedSymbol>, Solution>> FindReferencedSymbolsAsync(
            Document document, int position, IWaitContext waitContext)
        {
            var cancellationToken = waitContext.CancellationToken;

            var symbolAndProject = await GetRelevantSymbolAndProjectAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
            {
                return null;
            }

            var symbol = symbolAndProject.Item1;
            var project = symbolAndProject.Item2;

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

            // Otherwise, fall back to displaying SymbolFinder based references.
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
            var symbolAndProject = await GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
            {
                return null;
            }

            var symbol = symbolAndProject.Item1;
            var project = symbolAndProject.Item2;

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