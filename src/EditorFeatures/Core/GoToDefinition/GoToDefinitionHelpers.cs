// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GoToDefinition
{
    internal static class GoToDefinitionHelpers
    {
        public static async Task<bool> TryNavigateToLocationAsync(
            ISymbol symbol,
            Solution solution,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true)
        {
            var location = await GetDefinitionLocationAsync(
                symbol, solution, threadingContext, streamingPresenter, cancellationToken, thirdPartyNavigationAllowed).ConfigureAwait(false);
            return await location.TryNavigateToAsync(
                threadingContext, new NavigationOptions(PreferProvisionalTab: true, ActivateTab: true), cancellationToken).ConfigureAwait(false);
        }

        public static async Task<INavigableLocation?> GetDefinitionLocationAsync(
            ISymbol symbol,
            Solution solution,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true)
        {
            var title = string.Format(EditorFeaturesResources._0_declarations,
                FindUsagesHelpers.GetDisplayName(symbol));

            var definitions = await GoToDefinitionFeatureHelpers.GetDefinitionsAsync(
                symbol, solution, thirdPartyNavigationAllowed, cancellationToken).ConfigureAwait(false);

            return await streamingPresenter.GetStreamingLocationAsync(
                threadingContext, solution.Workspace, title, definitions, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<INavigableItem>?> GetDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // Try IFindDefinitionService first. Until partners implement this, it could fail to find a service, so fall back if it's null.
            var findDefinitionService = document.GetLanguageService<IFindDefinitionService>();
            if (findDefinitionService != null)
            {
                return await findDefinitionService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            }

            // Removal of this codepath is tracked by https://github.com/dotnet/roslyn/issues/50391. Once it is removed, this GetDefinitions method should
            // be inlined into call sites.
            var goToDefinitionsService = document.GetRequiredLanguageService<IGoToDefinitionService>();
            return await goToDefinitionsService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
        }
    }
}
