// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindReferences;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService : IFindUsagesService
    {
        public async Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
        {
            var tuple = await FindUsagesHelpers.FindImplementationsAsync(
                document, position, context.CancellationToken).ConfigureAwait(false);
            if (tuple == null)
            {
                context.ReportMessage(EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret);
                return;
            }

            var message = tuple.Value.message;

            if (message != null)
            {
                context.ReportMessage(message);
                return;
            }

            context.SetSearchTitle(string.Format(EditorFeaturesResources._0_implementations,
                FindUsagesHelpers.GetDisplayName(tuple.Value.symbol)));

            var project = tuple.Value.project;
            foreach (var implementation in tuple.Value.implementations)
            {
                var definitionItem = implementation.ToDefinitionItem(
                    project.Solution, includeHiddenLocations: false);
                await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }
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

            context.SetSearchTitle(string.Format(EditorFeaturesResources._0_references,
                FindUsagesHelpers.GetDisplayName(symbol)));

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
