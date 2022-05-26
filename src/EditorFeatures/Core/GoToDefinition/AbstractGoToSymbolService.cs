// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.GoToDefinition;

namespace Microsoft.CodeAnalysis.GoToDefinition
{
    // Ctrl+Click (GoToSymbol)
    internal abstract class AbstractGoToSymbolService : IGoToSymbolService
    {
        public async Task GetSymbolsAsync(GoToSymbolContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var service = document.GetRequiredLanguageService<IGoToDefinitionSymbolService>();

            // [includeType: false]
            // Enable Ctrl+Click on tokens with aliased, referenced or declared symbol.
            // If the token has none of those but does have a type (mostly literals), we're not interested
            var (symbol, span) = await service.GetSymbolAndBoundSpanAsync(document, position, includeType: false, cancellationToken).ConfigureAwait(false);

            if (symbol == null)
            {
                return;
            }

            var solution = document.Project.Solution;
            var definitions = await GoToDefinitionHelpers.GetDefinitionsAsync(symbol, solution, thirdPartyNavigationAllowed: true, cancellationToken).ConfigureAwait(false);

            foreach (var definition in definitions)
            {
                var location = await definition.GetNavigableLocationAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                if (location != null)
                    context.AddItem(WellKnownSymbolTypes.Definition, definition);
            }

            context.Span = span;
        }
    }
}
