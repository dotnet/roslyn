// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    // Ctrl+Click (GoToSymbol)
    internal abstract class AbstractGoToSymbolService : ForegroundThreadAffinitizedObject, IGoToSymbolService
    {
        protected AbstractGoToSymbolService(IThreadingContext threadingContext, bool assertIsForeground = false)
            : base(threadingContext, assertIsForeground)
        {
        }

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

            foreach (var def in definitions)
            {
                if (def.CanNavigateTo(solution.Workspace, cancellationToken))
                    context.AddItem(WellKnownSymbolTypes.Definition, def);
            }

            context.Span = span;
        }
    }
}
