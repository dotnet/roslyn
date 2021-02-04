// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal abstract class AbstractEditorNavigationBarItemService : INavigationBarItemService
    {
        public abstract VirtualTreePoint? GetSymbolItemNavigationPoint(Document document, RoslynNavigationBarItem symbolItem, CancellationToken cancellationToken);
        protected abstract void NavigateToItem(Document document, WrappedNavigationBarItem item, ITextView textView, CancellationToken cancellationToken);

        public async Task<IList<NavigationBarItem>> GetItemsAsync(Document document, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<CodeAnalysis.NavigationBar.INavigationBarItemService>();
            var items = await service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            return items.SelectAsArray(v => (NavigationBarItem)new WrappedNavigationBarItem(v));
        }

        public void NavigateToItem(Document document, NavigationBarItem item, ITextView textView, CancellationToken cancellationToken)
            => NavigateToItem(document, (WrappedNavigationBarItem)item, textView, cancellationToken);

        public void NavigateToSymbolItem(
            Document document, RoslynNavigationBarItem item, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(item.Kind == RoslynNavigationBarItemKind.Symbol);
            var symbolNavigationService = document.Project.Solution.Workspace.Services.GetService<ISymbolNavigationService>();

            var symbolInfo = item.NavigationSymbolId.Resolve(document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken), ignoreAssemblyKey: true, cancellationToken: cancellationToken);
            var symbol = symbolInfo.GetAnySymbol();

            // Do not allow third party navigation to types or constructors
            if (symbol != null &&
                !(symbol is ITypeSymbol) &&
                !symbol.IsConstructor() &&
                symbolNavigationService.TrySymbolNavigationNotify(symbol, document.Project, cancellationToken))
            {
                return;
            }

            var navigationPoint = this.GetSymbolItemNavigationPoint(document, item, cancellationToken);

            if (navigationPoint.HasValue)
            {
                NavigateToVirtualTreePoint(document.Project.Solution, navigationPoint.Value, cancellationToken);
            }
        }

        protected static void NavigateToVirtualTreePoint(Solution solution, VirtualTreePoint navigationPoint, CancellationToken cancellationToken)
        {
            var documentToNavigate = solution.GetDocument(navigationPoint.Tree);
            var workspace = solution.Workspace;
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

            if (navigationService.CanNavigateToPosition(workspace, documentToNavigate.Id, navigationPoint.Position, navigationPoint.VirtualSpaces, cancellationToken))
            {
                navigationService.TryNavigateToPosition(workspace, documentToNavigate.Id, navigationPoint.Position, navigationPoint.VirtualSpaces, options: null, cancellationToken);
            }
            else
            {
                var notificationService = workspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(EditorFeaturesResources.The_definition_of_the_object_is_hidden, severity: NotificationSeverity.Error);
            }
        }

        public virtual bool ShowItemGrayedIfNear(NavigationBarItem item)
            => true;
    }
}
