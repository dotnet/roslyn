// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
{
    internal abstract class AbstractEditorNavigationBarItemService : ForegroundThreadAffinitizedObject, INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess
    {
        protected AbstractEditorNavigationBarItemService(IThreadingContext threadingContext)
            : base(threadingContext, assertIsForeground: false)
        {
        }

        protected abstract Task<VirtualTreePoint?> GetSymbolNavigationPointAsync(Document document, ISymbol symbol, CancellationToken cancellationToken);
        protected abstract Task<bool> TryNavigateToItemAsync(Document document, WrappedNavigationBarItem item, ITextView textView, CancellationToken cancellationToken);

        [Obsolete("Caller should call NavigateToItemAsync instead", error: true)]
        public void NavigateToItem(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken)
            => throw new NotSupportedException($"Caller should call {nameof(TryNavigateToItemAsync)} instead");

        public async Task<IList<NavigationBarItem>?> GetItemsAsync(Document document, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<CodeAnalysis.NavigationBar.INavigationBarItemService>();
            var workspaceSupportsDocumentChanges = document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument);
            var items = await service.GetItemsAsync(document, workspaceSupportsDocumentChanges, cancellationToken).ConfigureAwait(false);
            return items.SelectAsArray(v => (NavigationBarItem)new WrappedNavigationBarItem(v));
        }

        public Task<bool> TryNavigateToItemAsync(Document document, NavigationBarItem item, ITextView textView, CancellationToken cancellationToken)
            => TryNavigateToItemAsync(document, (WrappedNavigationBarItem)item, textView, cancellationToken);

        protected async Task NavigateToSymbolItemAsync(
            Document document, RoslynNavigationBarItem.SymbolItem item, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(item.Kind == RoslynNavigationBarItemKind.Symbol);
            var symbolNavigationService = document.Project.Solution.Workspace.Services.GetRequiredService<ISymbolNavigationService>();

            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbolInfo = item.NavigationSymbolId.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken);
            var symbol = symbolInfo.GetAnySymbol();

            // Do not allow third party navigation to types or constructors
            if (symbol != null &&
                symbol is not ITypeSymbol &&
                !symbol.IsConstructor() &&
                await symbolNavigationService.TrySymbolNavigationNotifyAsync(symbol, document.Project, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var navigationPoint = await this.GetSymbolItemNavigationPointAsync(document, item, cancellationToken).ConfigureAwait(false);
            if (navigationPoint.HasValue)
            {
                await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                NavigateToVirtualTreePoint(document.Project.Solution, navigationPoint.Value, cancellationToken);
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

        public async Task<VirtualTreePoint?> GetSymbolItemNavigationPointAsync(Document document, RoslynNavigationBarItem.SymbolItem item, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(item.Kind == RoslynNavigationBarItemKind.Symbol);
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbols = item.NavigationSymbolId.Resolve(compilation, cancellationToken: cancellationToken);

            var symbol = symbols.Symbol;
            if (symbol == null)
            {
                if (item.NavigationSymbolIndex < symbols.CandidateSymbols.Length)
                {
                    symbol = symbols.CandidateSymbols[item.NavigationSymbolIndex];
                }
                else
                {
                    return null;
                }
            }

            return await GetSymbolNavigationPointAsync(document, symbol, cancellationToken).ConfigureAwait(false);
        }
    }
}
