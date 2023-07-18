// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.NavigateTo
{
    internal static class NavigateToHelpers
    {
        public static void NavigateTo(
            INavigateToSearchResult searchResult,
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor threadOperationExecutor,
            IAsynchronousOperationListener asyncListener)
        {
            var token = asyncListener.BeginAsyncOperation(nameof(NavigateTo));
            NavigateToAsync(searchResult, threadingContext, threadOperationExecutor)
                .ReportNonFatalErrorAsync()
                .CompletesAsyncOperation(token);
        }

        private static async Task NavigateToAsync(
            INavigateToSearchResult searchResult,
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor threadOperationExecutor)
        {
            var document = searchResult.NavigableItem.Document;
            if (document == null)
                return;

            var workspace = document.Workspace;
            var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

            // Document tabs opened by NavigateTo are carefully created as preview or regular tabs
            // by them; trying to specifically open them in a particular kind of tab here has no
            // effect.
            //
            // In the case of a stale item, don't require that the span be in bounds of the document
            // as it exists right now.
            using var context = threadOperationExecutor.BeginExecute(
                EditorFeaturesResources.Navigating_to_definition, EditorFeaturesResources.Navigating_to_definition, allowCancellation: true, showProgress: false);
            await navigationService.TryNavigateToSpanAsync(
                threadingContext,
                workspace,
                document.Id,
                searchResult.NavigableItem.SourceSpan,
                NavigationOptions.Default,
                allowInvalidSpan: searchResult.NavigableItem.IsStale,
                context.UserCancellationToken).ConfigureAwait(false);
        }
    }
}
