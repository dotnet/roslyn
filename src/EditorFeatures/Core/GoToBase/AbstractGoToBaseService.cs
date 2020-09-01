// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            var symbolAndProjectOpt = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            if (symbolAndProjectOpt == null)
            {
                await context.ReportMessageAsync(
                    EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret).ConfigureAwait(false);
                return;
            }

            var (symbol, project) = symbolAndProjectOpt.Value;

            var solution = project.Solution;
            var bases = await FindBaseHelpers.FindBasesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

            await context.SetSearchTitleAsync(
                string.Format(EditorFeaturesResources._0_bases,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

            var found = false;

            // For each potential base, try to find its definition in sources.
            // If found, add it's definitionItem to the context.
            // If not found but the symbol is from metadata, create it's definition item from metadata and add to the context.
            foreach (var baseSymbol in bases)
            {
                var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(
                   baseSymbol, solution, cancellationToken).ConfigureAwait(false);
                if (sourceDefinition != null)
                {
                    var definitionItem = await sourceDefinition.ToClassifiedDefinitionItemAsync(
                        solution, isPrimary: true, includeHiddenLocations: false, FindReferencesSearchOptions.Default, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
                    found = true;
                }
                else if (baseSymbol.Locations.Any(l => l.IsInMetadata))
                {
                    var definitionItem = baseSymbol.ToNonClassifiedDefinitionItem(
                        solution, includeHiddenLocations: true);
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
