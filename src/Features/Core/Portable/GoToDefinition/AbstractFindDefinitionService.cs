// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GoToDefinition
{
    internal class AbstractFindDefinitionService : IFindDefinitionService
    {
        public async Task<IEnumerable<INavigableItem>?> FindDefinitionsAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var symbolService = document.GetRequiredLanguageService<IGoToDefinitionSymbolService>();
            var (symbol, _) = await symbolService.GetSymbolAndBoundSpanAsync(document, position, includeType: true, cancellationToken).ConfigureAwait(false);

            // Try to compute source definitions from symbol.
            var items = symbol != null
                ? NavigableItemFactory.GetItemsFromPreferredSourceLocations(document.Project.Solution, symbol, displayTaggedParts: null, cancellationToken: cancellationToken)
                : null;

            // realize the list here so that the consumer await'ing the result doesn't lazily cause
            // them to be created on an inappropriate thread.
            return items?.ToList();
        }
    }
}
