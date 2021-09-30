// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess), InternalLanguageNames.TypeScript), Shared]
    internal class VSTypeScriptNavigationBarItemService : INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess
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

        public async Task<IList<NavigationBarItem>?> GetItemsAsync(Document document, CancellationToken cancellationToken)
        {
            var items = await _service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            return items.Select(x => ConvertToNavigationBarItem(x)).ToList();
        }

        public async Task<bool> TryNavigateToItemAsync(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken)
        {
            if (item.Spans.Length > 0)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var workspace = document.Project.Solution.Workspace;
                var navigationService = VSTypeScriptDocumentNavigationServiceWrapper.Create(workspace);
                navigationService.TryNavigateToPosition(workspace, document.Id, item.Spans[0].Start, virtualSpace: 0, options: null, cancellationToken: cancellationToken);
            }

            return true;
        }

        public bool ShowItemGrayedIfNear(NavigationBarItem item)
        {
            return true;
        }

        private static NavigationBarItem ConvertToNavigationBarItem(VSTypescriptNavigationBarItem item)
        {
            return new InternalNavigationBarItem(
                item.Text,
                VSTypeScriptGlyphHelpers.ConvertTo(item.Glyph),
                item.Spans,
                item.ChildItems.SelectAsArray(x => ConvertToNavigationBarItem(x)),
                item.Indent,
                item.Bolded,
                item.Grayed);
        }

        private class InternalNavigationBarItem : NavigationBarItem
        {
            public InternalNavigationBarItem(string text, Glyph glyph, ImmutableArray<TextSpan> spans, ImmutableArray<NavigationBarItem> childItems, int indent, bool bolded, bool grayed)
                : base(text, glyph, spans, childItems, indent, bolded, grayed)
            {
            }
        }
    }
}
