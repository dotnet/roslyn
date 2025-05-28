// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class SolutionExplorerNavigationSupport(
    Workspace workspace,
    IThreadingContext threadingContext,
    IAsynchronousOperationListener listener)
{
    private readonly CancellationSeries _cancellationSeries = new(threadingContext.DisposalToken);

    public void NavigateTo(
        DocumentId documentId, int position, bool preview)
    {
        // Cancel any in flight navigation and kick off a new one.
        var cancellationToken = _cancellationSeries.CreateNext();
        var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

        var token = listener.BeginAsyncOperation(nameof(NavigateTo));
        navigationService.TryNavigateToPositionAsync(
            threadingContext,
            workspace,
            documentId,
            position,
            virtualSpace: 0,
            // May be calling this on stale data.  Allow the position to be invalid
            allowInvalidPosition: true,
            new NavigationOptions(PreferProvisionalTab: preview),
            cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken).CompletesAsyncOperation(token);
    }
}
