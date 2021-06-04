// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(INavigationBarItemService), InternalLanguageNames.TypeScript), Shared]
    internal class VSTypeScriptNavigationBarItemService : INavigationBarItemService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IVSTypeScriptNavigationBarItemService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptNavigationBarItemService(
            IThreadingContext threadingContext,
            IVSTypeScriptNavigationBarItemService service)
        {
            _threadingContext = threadingContext;
            _service = service;
        }

        public async Task<ImmutableArray<NavigationBarItem>> GetItemsAsync(
            Document document, ITextSnapshot textSnapshot, CancellationToken cancellationToken)
        {
            var items = await _service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            return items.SelectAsArray(x => ConvertToNavigationBarItem(x, textSnapshot));
        }

        public async Task<bool> TryNavigateToItemAsync(
            Document document, NavigationBarItem item, ITextView view, ITextSnapshot textSnapshot, CancellationToken cancellationToken)
        {
            if (item.TrackingSpans.Any())
            {
                var span = item.TrackingSpans[0].GetSpan(textSnapshot);
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var workspace = document.Project.Solution.Workspace;
                var navigationService = VSTypeScriptDocumentNavigationServiceWrapper.Create(workspace);
                navigationService.TryNavigateToPosition(workspace, document.Id, span.Start, virtualSpace: 0, options: null, cancellationToken: cancellationToken);
            }

            return true;
        }

        public bool ShowItemGrayedIfNear(NavigationBarItem item)
        {
            return true;
        }

        private static NavigationBarItem ConvertToNavigationBarItem(VSTypescriptNavigationBarItem item, ITextSnapshot textSnapshot)
        {
            return new InternalNavigationBarItem(
                item.Text,
                VSTypeScriptGlyphHelpers.ConvertTo(item.Glyph),
                NavigationBarItem.GetTrackingSpans(textSnapshot, item.Spans),
                item.ChildItems.SelectAsArray(x => ConvertToNavigationBarItem(x, textSnapshot)),
                item.Indent,
                item.Bolded,
                item.Grayed);
        }

        private class InternalNavigationBarItem : NavigationBarItem
        {
            public InternalNavigationBarItem(string text, Glyph glyph, ImmutableArray<ITrackingSpan> trackingSpans, ImmutableArray<NavigationBarItem> childItems, int indent, bool bolded, bool grayed)
                : base(text, glyph, trackingSpans, childItems, indent, bolded, grayed)
            {
            }
        }
    }
}
