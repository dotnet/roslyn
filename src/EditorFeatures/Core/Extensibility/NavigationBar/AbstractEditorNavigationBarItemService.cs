// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
{
    internal abstract class AbstractEditorNavigationBarItemService : ForegroundThreadAffinitizedObject, INavigationBarItemService
    {
        protected AbstractEditorNavigationBarItemService(IThreadingContext threadingContext)
            : base(threadingContext, assertIsForeground: false)
        {
        }

        protected abstract Task<bool> TryNavigateToItemAsync(Document document, WrappedNavigationBarItem item, ITextView textView, ITextSnapshot textSnapshot, CancellationToken cancellationToken);

        public async Task<ImmutableArray<NavigationBarItem>> GetItemsAsync(
            Document document, ITextSnapshot textSnapshot, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<CodeAnalysis.NavigationBar.INavigationBarItemService>();
            var workspaceSupportsDocumentChanges = document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument);
            var items = await service.GetItemsAsync(document, workspaceSupportsDocumentChanges, cancellationToken).ConfigureAwait(false);
            return items.SelectAsArray(v => (NavigationBarItem)new WrappedNavigationBarItem(v, textSnapshot));
        }

        public Task<bool> TryNavigateToItemAsync(Document document, NavigationBarItem item, ITextView textView, ITextSnapshot textSnapshot, CancellationToken cancellationToken)
            => TryNavigateToItemAsync(document, (WrappedNavigationBarItem)item, textView, textSnapshot, cancellationToken);

        protected static void NavigateToSymbolItem(
            Document document, NavigationBarItem item, RoslynNavigationBarItem.SymbolItem symbolItem, ITextSnapshot textSnapshot, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var (documentId, position) = GetNavigationLocation(document, item, symbolItem, textSnapshot);
            var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

            if (navigationService.CanNavigateToPosition(workspace, documentId, position, cancellationToken))
            {
                navigationService.TryNavigateToPosition(workspace, documentId, position, cancellationToken);
            }
            else
            {
                var notificationService = workspace.Services.GetRequiredService<INotificationService>();
                notificationService.SendNotification(EditorFeaturesResources.The_definition_of_the_object_is_hidden, severity: NotificationSeverity.Error);
            }
        }

        internal static (DocumentId documentId, int position) GetNavigationLocation(
            Document document, NavigationBarItem item, RoslynNavigationBarItem.SymbolItem symbolItem, ITextSnapshot textSnapshot)
        {
            if (item.NavigationTrackingSpan != null)
            {
                return (document.Id, item.NavigationTrackingSpan.GetSpan(textSnapshot).Start);
            }
            else
            {
                Contract.ThrowIfNull(symbolItem.Location.OtherDocumentInfo);
                var otherLocation = symbolItem.Location.OtherDocumentInfo.Value;
                return (otherLocation.documentId, otherLocation.navigationSpan.Start);
            }
        }

        protected void NavigateToVirtualTreePoint(Solution solution, VirtualTreePoint navigationPoint, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();
            var documentToNavigate = solution.GetRequiredDocument(navigationPoint.Tree);
            var workspace = solution.Workspace;
            var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

            if (navigationService.CanNavigateToPosition(workspace, documentToNavigate.Id, navigationPoint.Position, navigationPoint.VirtualSpaces, cancellationToken))
            {
                navigationService.TryNavigateToPosition(workspace, documentToNavigate.Id, navigationPoint.Position, navigationPoint.VirtualSpaces, options: null, cancellationToken);
            }
            else
            {
                var notificationService = workspace.Services.GetRequiredService<INotificationService>();
                notificationService.SendNotification(EditorFeaturesResources.The_definition_of_the_object_is_hidden, severity: NotificationSeverity.Error);
            }
        }

        public virtual bool ShowItemGrayedIfNear(NavigationBarItem item)
            => true;

        //public async Task<VirtualTreePoint?> GetSymbolItemNavigationPointAsync(Document document, RoslynNavigationBarItem.SymbolItem item, CancellationToken cancellationToken)
        //{
        //    Contract.ThrowIfFalse(item.Kind == RoslynNavigationBarItemKind.Symbol);
        //    var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        //    var symbols = item.NavigationSymbolId.Resolve(compilation, cancellationToken: cancellationToken);

        //    var symbol = symbols.Symbol;
        //    if (symbol == null)
        //    {
        //        if (item.NavigationSymbolIndex < symbols.CandidateSymbols.Length)
        //        {
        //            symbol = symbols.CandidateSymbols[item.NavigationSymbolIndex];
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }

        //    return await GetSymbolNavigationPointAsync(document, symbol, cancellationToken).ConfigureAwait(false);
        //}
    }
}
