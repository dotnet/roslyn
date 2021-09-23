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
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem;

namespace Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
{
    internal abstract class AbstractEditorNavigationBarItemService : ForegroundThreadAffinitizedObject, INavigationBarItemService
    {
        protected AbstractEditorNavigationBarItemService(IThreadingContext threadingContext)
            : base(threadingContext, assertIsForeground: false)
        {
        }

        protected abstract Task<bool> TryNavigateToItemAsync(Document document, WrappedNavigationBarItem item, ITextView textView, ITextVersion textVersion, CancellationToken cancellationToken);

        public async Task<ImmutableArray<NavigationBarItem>> GetItemsAsync(Document document, ITextVersion textVersion, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<CodeAnalysis.NavigationBar.INavigationBarItemService>();
            var workspaceSupportsDocumentChanges = document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument);
            var items = await service.GetItemsAsync(document, workspaceSupportsDocumentChanges, cancellationToken).ConfigureAwait(false);
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

            // Ensure we're back on the UI thread before either navigating or showing a failure message.
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            NavigateToPosition(workspace, documentId, position, virtualSpace, cancellationToken);
        }

        protected void NavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();
            var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();
            if (navigationService.CanNavigateToPosition(workspace, documentId, position, virtualSpace, cancellationToken))
            {
                navigationService.TryNavigateToPosition(workspace, documentId, position, virtualSpace, options: null, cancellationToken);
            }
            else
            {
                var notificationService = workspace.Services.GetRequiredService<INotificationService>();
                notificationService.SendNotification(EditorFeaturesResources.The_definition_of_the_object_is_hidden, severity: NotificationSeverity.Error);
            }
        }

        internal virtual Task<(DocumentId documentId, int position, int virtualSpace)> GetNavigationLocationAsync(
            Document document,
            NavigationBarItem item,
            SymbolItem symbolItem,
            ITextVersion textVersion,
            CancellationToken cancellationToken)
        {
            // If the item points to a location in this document, then just determine the current location
            // of that item and go directly to it.
            var navigationSpan = item.TryGetNavigationSpan(textVersion);
            if (navigationSpan != null)
            {
                return Task.FromResult((document.Id, navigationSpan.Value.Start, 0));
            }
            else
            {
                // Otherwise, the item pointed to a location in another document.  Just return the position we
                // computed and stored for it.
                Contract.ThrowIfNull(symbolItem.Location.OtherDocumentInfo);
                var (documentId, span) = symbolItem.Location.OtherDocumentInfo.Value;
                return Task.FromResult((documentId, span.Start, 0));
            }
        }

        public bool ShowItemGrayedIfNear(NavigationBarItem item)
        {
            // We only show items in gray when near that actually exist (i.e. are not meant for codegen).
            // This will be all C# items, and only VB non-codegen items.
            return ((WrappedNavigationBarItem)item).UnderlyingItem is SymbolItem;
        }
    }
}
