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

internal abstract class AbstractSymbolTreeItemSourceProvider<TItem> : AttachedCollectionSourceProvider<TItem>
{
    protected readonly IThreadingContext ThreadingContext;
    protected readonly Workspace Workspace;

    protected readonly IAsynchronousOperationListener Listener;

    private static readonly CancellationSeries s_navigationCancellationSeries = new();

    // private readonly IAnalyzersCommandHandler _commandHandler = commandHandler;

    // private IHierarchyItemToProjectIdMap? _projectMap;

    protected AbstractSymbolTreeItemSourceProvider(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider
    /*,
[Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler*/)
    {
        ThreadingContext = threadingContext;
        Workspace = workspace;
        Listener = listenerProvider.GetListener(FeatureAttribute.SolutionExplorer);
    }

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
            item.SyntaxNode.SpanStart,
            virtualSpace: 0,
            // May be calling this on stale data.  Allow the position to be invalid
            allowInvalidPosition: true,
            new NavigationOptions(PreferProvisionalTab: preview),
            cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken).CompletesAsyncOperation(token);
    }
}
