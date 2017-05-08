// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService : IFindUsagesService
    {
        public async Task FindImplementationsAsync(
            Document document, int position, IFindUsagesContext context)
        {
            // First, on the host side, try to map the document+position to a symbol we can search
            // for.  We need this so that we can properly map back a metadata-as-source symbol to
            // a symbol in the user's actual project context.
            var cancellationToken = context.CancellationToken;
            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            if (symbolAndProject == null)
            {
                await context.ReportMessageAsync(
                    EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret).ConfigureAwait(false);
                return;
            }

            if (await TryFindImplementationsInRemoteProcessAsync(
                    symbolAndProject?.symbol, symbolAndProject?.project, context).ConfigureAwait(false))
            {
                return;
            }

            await FindImplementationsInCurrentProcessAsync(
                context, symbolAndProject?.symbol, symbolAndProject?.project).ConfigureAwait(false);
        }

        // Internal so it can be called from the remote code entrypoint.
        internal static async Task FindImplementationsInCurrentProcessAsync(
            IFindUsagesContext context, ISymbol symbol, Project project)
        {
            var cancellationToken = context.CancellationToken;
            var implementations = await FindUsagesHelpers.FindImplementationsAsync(
                symbol, project, cancellationToken).ConfigureAwait(false);

            if (implementations.Length == 0)
            {
                await context.ReportMessageAsync(EditorFeaturesResources.The_symbol_has_no_implementations).ConfigureAwait(false);
                return;
            }

            await context.SetSearchTitleAsync(
                string.Format(EditorFeaturesResources._0_implementations,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

            foreach (var implementation in implementations)
            {
                var definitionItem = await implementation.ToClassifiedDefinitionItemAsync(
                    project.Solution, includeHiddenLocations: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }
        }

        private async Task<bool> TryFindImplementationsInRemoteProcessAsync(
            ISymbol symbol, Project project, IFindUsagesContext context)
        {
            var solution = project.Solution;
            var callback = new FindUsagesCallback(solution, context);
            using (var session = await TryGetRemoteSessionAsync(
                solution, callback, context.CancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    return false;
                }

                await session.InvokeAsync(
                    nameof(IRemoteFindUsages.FindImplementationsAsync),
                    SerializableSymbolAndProjectId.Dehydrate(new SymbolAndProjectId(symbol, project.Id))).ConfigureAwait(false);
                return true;
            }
        }
    }
}