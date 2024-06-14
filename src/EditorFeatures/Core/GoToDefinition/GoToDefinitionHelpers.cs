// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.GoToDefinition;

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
}
