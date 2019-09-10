// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.Editor.GoToBase
{
    internal abstract partial class AbstractGoToBaseService : IGoToBaseService
    {
        public async Task FindBasesAsync(Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            var tuple = await FindBaseHelpers.FindBasesAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (tuple == null)
            {
                await context.ReportMessageAsync(
                    EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret).ConfigureAwait(false);
                return;
            }

            var (symbol, implementations, message) = tuple.Value;

            if (message != null)
            {
                await context.ReportMessageAsync(message).ConfigureAwait(false);
                return;
            }

            await context.SetSearchTitleAsync(
                string.Format(EditorFeaturesResources._0_bases,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

            var solution = document.Project.Solution;

            foreach (var implementation in implementations)
            {
                var definitionItem = await implementation.Symbol.ToClassifiedDefinitionItemAsync(
                    solution.GetProject(implementation.ProjectId), includeHiddenLocations: false,
                    FindReferencesSearchOptions.Default, cancellationToken: cancellationToken).ConfigureAwait(false);
                await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }
        }
    }
}
