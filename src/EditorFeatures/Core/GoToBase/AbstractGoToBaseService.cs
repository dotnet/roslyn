// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
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
            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            if (symbolAndProject == default)
            {
                await context.ReportMessageAsync(
                    EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret).ConfigureAwait(false);
                return;
            }

            var symbol = symbolAndProject.Value.symbol;
            var bases = FindBaseHelpers.FindBases(
                symbol, symbolAndProject.Value.project, cancellationToken);

            await context.SetSearchTitleAsync(
                string.Format(EditorFeaturesResources._0_bases,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

            var project = document.Project;
            var solution = project.Solution;
            var projectId = project.Id;

            var found = false;

            // For each potential base, try to find its definition in sources.
            // If found, add it's definitionItem to the context.
            // If not found but the symbol is from metadata, create it's definition item from metadata and add to the context.
            foreach (var baseSymbol in bases)
            {
                var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(
                   SymbolAndProjectId.Create(baseSymbol, projectId), solution, cancellationToken).ConfigureAwait(false);
                if (sourceDefinition.Symbol != null)
                {
                    var definitionItem = await sourceDefinition.Symbol.ToClassifiedDefinitionItemAsync(
                        solution.GetProject(sourceDefinition.ProjectId), includeHiddenLocations: false,
                        FindReferencesSearchOptions.Default, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
                    found = true;
                }
                else if (baseSymbol.Locations.Any(l => l.IsInMetadata))
                {
                    var definitionItem = baseSymbol.ToNonClassifiedDefinitionItem(
                        project, includeHiddenLocations: true);
                    await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
                    found = true;
                }
            }

            if (!found)
            {
                await context.ReportMessageAsync(EditorFeaturesResources.The_symbol_has_no_base)
                    .ConfigureAwait(false);
            }
        }
    }
}
