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
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigationBar;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor;

/// <summary>
/// Feeds F# symbols into the Features/Core/Portable-layer <see cref="INavigationBarItemService"/>, the
/// contract <see cref="Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentSymbolsHandler"/> uses to answer
/// textDocument/documentSymbol (and therefore what backs LSP-hosted UI such as the Document Outline tool
/// window). This is separate from <see cref="Microsoft.CodeAnalysis.Editor.INavigationBarItemService"/>, which
/// only drives the in-editor dropdown bar and lives in the WPF-dependent EditorFeatures layer that
/// DocumentSymbolsHandler's portable project cannot reference.
/// </summary>
[Shared]
[ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.FSharp)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FSharpDocumentSymbolNavigationBarItemService(
    [Import(AllowDefault = true)] IFSharpNavigationBarItemService? service) : INavigationBarItemService
{
    private readonly IFSharpNavigationBarItemService? _service = service;

    public async Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsAsync(
        Document document, bool supportsCodeGeneration, bool frozenPartialSemantics, CancellationToken cancellationToken)
    {
        if (_service == null)
            return [];

        var items = await _service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
        return items == null
            ? []
            : ConvertItems(items);
    }

    private static ImmutableArray<RoslynNavigationBarItem> ConvertItems(IList<FSharpNavigationBarItem> items)
        => (items ?? SpecializedCollections.EmptyList<FSharpNavigationBarItem>()).SelectAsArray(x => x.Spans.Any(), ConvertToSymbolItem);

    private static RoslynNavigationBarItem ConvertToSymbolItem(FSharpNavigationBarItem item)
    {
        var spans = item.Spans.ToImmutableArrayOrEmpty();
        var location = new RoslynNavigationBarItem.SymbolItemLocation(inDocumentInfo: (spans, spans[0]), otherDocumentInfo: null);

        return new RoslynNavigationBarItem.SymbolItem(
            name: item.Text,
            text: item.Text,
            glyph: FSharpGlyphHelpers.ConvertTo(item.Glyph),
            isObsolete: false,
            location: location,
            childItems: ConvertItems(item.ChildItems),
            indent: item.Indent,
            bolded: item.Bolded);
    }
}
