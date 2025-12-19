// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using static Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem;

namespace Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar;

internal abstract class AbstractEditorNavigationBarItemService : INavigationBarItemService
{
    protected readonly IThreadingContext ThreadingContext;

    protected AbstractEditorNavigationBarItemService(IThreadingContext threadingContext)
    {
        ThreadingContext = threadingContext;
    }

    protected abstract Task<bool> TryNavigateToItemAsync(Document document, WrappedNavigationBarItem item, ITextView textView, ITextVersion textVersion, CancellationToken cancellationToken);

    public async Task<ImmutableArray<NavigationBarItem>> GetItemsAsync(
        Document document,
        bool workspaceSupportsDocumentChanges,
        bool frozenPartialSemantics,
        ITextVersion textVersion,
        CancellationToken cancellationToken)
    {
        var service = document.GetRequiredLanguageService<CodeAnalysis.NavigationBar.INavigationBarItemService>();
        var items = await service.GetItemsAsync(document, workspaceSupportsDocumentChanges, frozenPartialSemantics, cancellationToken).ConfigureAwait(false);
        return items.SelectAsArray(v => (NavigationBarItem)new WrappedNavigationBarItem(textVersion, v));
    }

    public Task<bool> TryNavigateToItemAsync(Document document, NavigationBarItem item, ITextView textView, ITextVersion textVersion, CancellationToken cancellationToken)
        => TryNavigateToItemAsync(document, (WrappedNavigationBarItem)item, textView, textVersion, cancellationToken);

    protected async Task NavigateToSymbolItemAsync(
        Document document, NavigationBarItem item, SymbolItem symbolItem, ITextVersion textVersion, CancellationToken cancellationToken)
    {
        var workspace = document.Project.Solution.Workspace;

        var (documentId, position, virtualSpace) = await GetNavigationLocationAsync(
            document, item, symbolItem, textVersion, cancellationToken).ConfigureAwait(false);

        await NavigateToPositionAsync(workspace, documentId, position, virtualSpace, cancellationToken).ConfigureAwait(false);
    }

    protected async Task NavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
    {
        var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

        if (!await navigationService.TryNavigateToPositionAsync(
                ThreadingContext, workspace, documentId, position, virtualSpace,
                allowInvalidPosition: false, NavigationOptions.Default, cancellationToken).ConfigureAwait(false))
        {
            // Ensure we're back on the UI thread before showing a failure message.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var notificationService = workspace.Services.GetRequiredService<INotificationService>();
            notificationService.SendNotification(EditorFeaturesResources.The_definition_of_the_object_is_hidden, severity: NotificationSeverity.Error);
        }
    }

    internal virtual async ValueTask<(DocumentId documentId, int position, int virtualSpace)> GetNavigationLocationAsync(
        Document document,
        NavigationBarItem item,
        SymbolItem symbolItem,
        ITextVersion textVersion,
        CancellationToken cancellationToken)
    {
        if (symbolItem.Location.InDocumentInfo != null)
        {
            // If the item points to a location in this document, then just determine the where that span currently
            // is (in case recent edits have moved it) and navigate there.
            var navigationSpan = item.GetCurrentItemSpan(textVersion, symbolItem.Location.InDocumentInfo.Value.navigationSpan);
            return (document.Id, navigationSpan.Start, 0);
        }
        else
        {
            // Otherwise, the item pointed to a location in another document.  Just return the position we
            // computed and stored for it.
            Contract.ThrowIfNull(symbolItem.Location.OtherDocumentInfo);
            var (documentId, span) = symbolItem.Location.OtherDocumentInfo.Value;
            return (documentId, span.Start, 0);
        }
    }

    public bool ShowItemGrayedIfNear(NavigationBarItem item)
    {
        // We only show items in gray when near that actually exist (i.e. are not meant for codegen).
        // This will be all C# items, and only VB non-codegen items.
        return ((WrappedNavigationBarItem)item).UnderlyingItem is SymbolItem;
    }
}
