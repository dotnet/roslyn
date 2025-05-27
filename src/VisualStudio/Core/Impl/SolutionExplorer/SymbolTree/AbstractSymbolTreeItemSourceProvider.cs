// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal abstract class AbstractSymbolTreeItemSourceProvider<TItem>(
    IThreadingContext threadingContext,
    VisualStudioWorkspace workspace,
    IAsynchronousOperationListenerProvider listenerProvider) : AttachedCollectionSourceProvider<TItem>
{
    public readonly IThreadingContext ThreadingContext = threadingContext;
    protected readonly Workspace Workspace = workspace;

    public readonly IAsynchronousOperationListener Listener = listenerProvider.GetListener(FeatureAttribute.SolutionExplorer);

    private static readonly CancellationSeries s_navigationCancellationSeries = new();

    public void NavigateTo(SymbolTreeItem item, bool preview)
    {
        // Cancel any in flight navigation and kick off a new one.
        var cancellationToken = s_navigationCancellationSeries.CreateNext(this.ThreadingContext.DisposalToken);
        var navigationService = Workspace.Services.GetRequiredService<IDocumentNavigationService>();

        var token = Listener.BeginAsyncOperation(nameof(NavigateTo));
        navigationService.TryNavigateToPositionAsync(
            ThreadingContext,
            Workspace,
            item.DocumentId,
            item.ItemSyntax.NavigationToken.SpanStart,
            virtualSpace: 0,
            // May be calling this on stale data.  Allow the position to be invalid
            allowInvalidPosition: true,
            new NavigationOptions(PreferProvisionalTab: preview),
            cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken).CompletesAsyncOperation(token);
    }
}
