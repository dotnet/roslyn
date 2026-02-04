// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
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
        var callHierarchyService = document.GetRequiredLanguageService<ICallHierarchyService>();
        var callHierarchyCache = context.GetRequiredLspService<CallHierarchyCache>();

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var position = ProtocolConversions.PositionToLinePosition(request.Position);
        var textPosition = text.Lines.GetPosition(position);

        var item = await callHierarchyService.GetCallHierarchyItemAsync(
            document, textPosition, cancellationToken).ConfigureAwait(false);

        if (item == null)
            return null;

        // Store the item in the cache for future incoming/outgoing calls requests
        var resultId = callHierarchyCache.UpdateCache(new CallHierarchyCache.CallHierarchyCacheEntry(item));

        var lspItem = await ConvertToLspCallHierarchyItemAsync(
            item, document, resultId, cancellationToken).ConfigureAwait(false);

        return [lspItem];
    }

    internal static async Task<LSP.CallHierarchyItem> ConvertToLspCallHierarchyItemAsync(
        Microsoft.CodeAnalysis.CallHierarchy.CallHierarchyItem item,
        Document document,
        long resultId,
        CancellationToken cancellationToken)
    {
        var symbol = item.Symbol;

        // Get the location for this symbol
        var location = symbol.Locations.FirstOrDefault();
        Contract.ThrowIfNull(location);

        var linePositionSpan = location.GetLineSpan().Span;
        var range = ProtocolConversions.LinePositionToRange(linePositionSpan);
        var uri = document.GetURI();

        // For the selection range, use the same as the range for now
        // Getting the exact name token would require language-specific logic
        var selectionRange = range;

        var resolveData = new CallHierarchyResolveData(
            resultId,
            new TextDocumentIdentifier { DocumentUri = uri },
            symbol.Name,
            (int)symbol.Kind);

        return new LSP.CallHierarchyItem
        {
            Name = item.GetDisplayName(),
            Kind = ProtocolConversions.GlyphToSymbolKind(symbol.GetGlyph()),
            Tags = [],
            Detail = item.GetDetailText(),
            Uri = uri,
            Range = range,
            SelectionRange = selectionRange,
            Data = resolveData
        };
    }
}
