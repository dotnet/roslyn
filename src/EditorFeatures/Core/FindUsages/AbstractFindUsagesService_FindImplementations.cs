// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService
    {
        public async Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            var symbolAndProjectOpt = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProjectOpt == null)
            {
                await context.ReportMessageAsync(
                    EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret).ConfigureAwait(false);
                return;
            }

            var symbolAndProject = symbolAndProjectOpt.Value;
            await FindImplementationsAsync(
                symbolAndProject.symbol, symbolAndProject.project, context).ConfigureAwait(false);
        }

        public static async Task FindImplementationsAsync(
            ISymbol symbol, Project project, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            var solution = project.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                // Create a callback that we can pass to the server process to hear about the 
                // results as it finds them.  When we hear about results we'll forward them to
                // the 'progress' parameter which will then update the UI.
                var serverCallback = new FindUsagesServerCallback(solution, context);

                var success = await client.TryRunRemoteAsync(
                    WellKnownServiceHubServices.CodeAnalysisService,
                    nameof(IRemoteFindUsagesService.FindImplementationsAsync),
                    solution,
                    new object[]
                    {
                        SerializableSymbolAndProjectId.Create(symbol, project, cancellationToken),
                    },
                    serverCallback,
                    cancellationToken).ConfigureAwait(false);

                if (success)
                    return;
            }

            // Couldn't effectively search in OOP. Perform the search in-process.
            await FindImplementationsInCurrentProcessAsync(
                symbol, project, context).ConfigureAwait(false);
        }

        private static async Task FindImplementationsInCurrentProcessAsync(
            ISymbol symbol, Project project, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;

            var solution = project.Solution;
            var (implementations, message) = await FindUsagesHelpers.FindSourceImplementationsAsync(
                solution, symbol, cancellationToken).ConfigureAwait(false);

            if (message != null)
            {
                await context.ReportMessageAsync(message).ConfigureAwait(false);
                return;
            }

            await context.SetSearchTitleAsync(
                string.Format(EditorFeaturesResources._0_implementations,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

            foreach (var implementation in implementations)
            {
                var definitionItem = await implementation.ToClassifiedDefinitionItemAsync(
                    solution, isPrimary: true, includeHiddenLocations: false, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);

                await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }
        }
    }
}
