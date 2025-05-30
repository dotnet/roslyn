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

/// <summary>
/// Helper type for navigating to items shown in the solution explorer tree. Used for navigating to symbol
/// tree items, as well as the results of symbol tree search.
/// </summary>
internal sealed class SolutionExplorerNavigationSupport(
    Workspace workspace,
    IThreadingContext threadingContext,
    IAsynchronousOperationListenerProvider listenerProvider)
{
    private readonly CancellationSeries _cancellationSeries = new(threadingContext.DisposalToken);
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.SolutionExplorer);

    public void NavigateTo(DocumentId documentId, int position, bool preview)
    {
        // Cancel any in flight navigation and kick off a new one.
        var cancellationToken = _cancellationSeries.CreateNext();
        var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

        var token = _listener.BeginAsyncOperation(nameof(NavigateTo));
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
