// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

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
            Document document, CancellationToken cancellationToken)
        {
            var items = await _service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            return ConvertItems(items);
        }

        private static ImmutableArray<NavigationBarItem> ConvertItems(ImmutableArray<VSTypescriptNavigationBarItem> items)
            => items.SelectAsArray(x => !x.Spans.IsEmpty, x => ConvertToNavigationBarItem(x));

        public async Task<bool> TryNavigateToItemAsync(
            Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken)
        {
            if (item.NavigationSpan != null)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var workspace = document.Project.Solution.Workspace;
                var navigationService = VSTypeScriptDocumentNavigationServiceWrapper.Create(workspace);
                navigationService.TryNavigateToPosition(
                    workspace, document.Id, item.NavigationSpan.Value.Start,
                    virtualSpace: 0, options: null, cancellationToken: cancellationToken);
            }

            return true;
        }

        public bool ShowItemGrayedIfNear(NavigationBarItem item)
        {
            return true;
        }

        private static NavigationBarItem ConvertToNavigationBarItem(VSTypescriptNavigationBarItem item)
        {
            Contract.ThrowIfTrue(item.Spans.IsEmpty);
            return new InternalNavigationBarItem(
                item.Text,
                VSTypeScriptGlyphHelpers.ConvertTo(item.Glyph),
                item.Spans,
                ConvertItems(item.ChildItems),
                item.Indent,
                item.Bolded,
                item.Grayed);
        }

        private sealed class InternalNavigationBarItem : NavigationBarItem, IEquatable<InternalNavigationBarItem>
        {
            public InternalNavigationBarItem(string text, Glyph glyph, ImmutableArray<TextSpan> spans, ImmutableArray<NavigationBarItem> childItems, int indent, bool bolded, bool grayed)
                : base(text, glyph, spans, spans.First(), childItems, indent, bolded, grayed)
            {
            }

            public override bool Equals(object? obj)
                => Equals(obj as InternalNavigationBarItem);

            public bool Equals(InternalNavigationBarItem? other)
                => base.Equals(other);

            public override int GetHashCode()
                => throw new NotImplementedException();
        }
    }
}
