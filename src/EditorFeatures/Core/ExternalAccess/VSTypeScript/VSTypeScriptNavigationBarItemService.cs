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
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLanguageService(typeof(INavigationBarItemService), InternalLanguageNames.TypeScript), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VSTypeScriptNavigationBarItemService(
    IThreadingContext threadingContext,
    IVSTypeScriptNavigationBarItemService service) : INavigationBarItemService
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IVSTypeScriptNavigationBarItemService _service = service;

    public Task<ImmutableArray<NavigationBarItem>> GetItemsAsync(
        Document document, ITextVersion textVersion, CancellationToken cancellationToken)
    {
        return ((INavigationBarItemService)this).GetItemsAsync(
            document, workspaceSupportsDocumentChanges: true, frozenPartialSemantics: false, textVersion, cancellationToken);
    }

    async Task<ImmutableArray<NavigationBarItem>> INavigationBarItemService.GetItemsAsync(
        Document document,
        bool workspaceSupportsDocumentChanges,
        bool forceFrozenPartialSemanticsForCrossProcessOperations,
        ITextVersion textVersion,
        CancellationToken cancellationToken)
    {
        var items = await _service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
        return ConvertItems(items, textVersion);
    }

    private static ImmutableArray<NavigationBarItem> ConvertItems(ImmutableArray<VSTypescriptNavigationBarItem> items, ITextVersion textVersion)
        => items.SelectAsArray(x => !x.Spans.IsEmpty, x => ConvertToNavigationBarItem(x, textVersion));

    public async Task<bool> TryNavigateToItemAsync(
        Document document, NavigationBarItem item, ITextView view, ITextVersion textVersion, CancellationToken cancellationToken)
    {
        // Spans.First() is safe here as we filtered out any items with no spans above in ConvertItems.
        var navigationSpan = item.GetCurrentItemSpan(textVersion, item.Spans.First());

        var workspace = document.Project.Solution.Workspace;
        var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();
        return await navigationService.TryNavigateToPositionAsync(
            _threadingContext, workspace, document.Id, navigationSpan.Start, cancellationToken).ConfigureAwait(false);
    }

    public bool ShowItemGrayedIfNear(NavigationBarItem item)
    {
        return true;
    }

    private static NavigationBarItem ConvertToNavigationBarItem(VSTypescriptNavigationBarItem item, ITextVersion textVersion)
    {
        Contract.ThrowIfTrue(item.Spans.IsEmpty);
        return new SimpleNavigationBarItem(
            textVersion,
            item.Text,
            VSTypeScriptGlyphHelpers.ConvertTo(item.Glyph),
            item.Spans,
            ConvertItems(item.ChildItems, textVersion),
            item.Indent,
            item.Bolded,
            item.Grayed);
    }
}
