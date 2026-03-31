// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using RoslynCallHierarchyItem = Microsoft.CodeAnalysis.CallHierarchy.CallHierarchyItem;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(PrepareCallHierarchyHandler)), Shared]
[Method(Methods.PrepareCallHierarchyName)]
internal sealed class PrepareCallHierarchyHandler : ILspServiceDocumentRequestHandler<CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PrepareCallHierarchyHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(CallHierarchyPrepareParams request)
        => request.TextDocument;

    public async Task<LSP.CallHierarchyItem[]?> HandleRequestAsync(
        CallHierarchyPrepareParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var cache = context.GetRequiredLspService<CallHierarchyCache>();

        var position = await document.GetPositionFromLinePositionAsync(
            ProtocolConversions.PositionToLinePosition(request.Position),
            cancellationToken).ConfigureAwait(false);

        var item = await Microsoft.CodeAnalysis.CallHierarchy.CallHierarchyService.Instance.PrepareCallHierarchyAsync(
            document, position, cancellationToken).ConfigureAwait(false);

        if (item is null)
            return null;

        var lspItem = await ConvertToLspCallHierarchyItemAsync(
            document, item.Value, 0, cache, cancellationToken).ConfigureAwait(false);

        return lspItem is null ? null : [lspItem];
    }

    internal static async Task<LSP.CallHierarchyItem?> ConvertToLspCallHierarchyItemAsync(
        Document document,
        RoslynCallHierarchyItem item,
        int index,
        CallHierarchyCache cache,
        CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        // Cache the item so we can retrieve it later for incoming/outgoing calls
        var resultId = cache.UpdateCache(new CallHierarchyCache.CallHierarchyCacheEntry([item]));

        var range = ProtocolConversions.TextSpanToRange(item.Span, text);
        var selectionRange = ProtocolConversions.TextSpanToRange(item.SelectionSpan, text);

        return new LSP.CallHierarchyItem
        {
            Name = item.Name,
            Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph),
            Detail = item.Detail,
            Uri = document.GetURI(),
            Range = range,
            SelectionRange = selectionRange,
            Data = new CallHierarchyItemResolveData(resultId, index, CreateTextDocumentIdentifier(document))
        };
    }

    internal static TextDocumentIdentifier CreateTextDocumentIdentifier(Document document)
        => new() { DocumentUri = document.GetURI() };
}
