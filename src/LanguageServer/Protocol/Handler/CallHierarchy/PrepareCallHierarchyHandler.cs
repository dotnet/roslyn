// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(PrepareCallHierarchyHandler)), Shared]
[Method(LSP.Methods.PrepareCallHierarchyName)]
internal sealed class PrepareCallHierarchyHandler : ILspServiceDocumentRequestHandler<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PrepareCallHierarchyHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CallHierarchyPrepareParams request)
        => request.TextDocument;

    public async Task<LSP.CallHierarchyItem[]?> HandleRequestAsync(
        LSP.CallHierarchyPrepareParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var document = context.Document;
        Contract.ThrowIfNull(document);

        var callHierarchyService = document.GetLanguageService<ICallHierarchyService>();
        if (callHierarchyService == null)
            return null;

        var position = await document.GetPositionFromLinePositionAsync(
            ProtocolConversions.PositionToLinePosition(request.Position),
            cancellationToken).ConfigureAwait(false);

        var items = await callHierarchyService.PrepareCallHierarchyAsync(
            document, position, cancellationToken).ConfigureAwait(false);

        if (items.IsEmpty)
            return null;

        // Store the items in the cache for later resolution
        var callHierarchyCache = context.GetRequiredLspService<CallHierarchyCache>();
        var resultId = callHierarchyCache.UpdateCache(new CallHierarchyCache.CallHierarchyCacheEntry(items));

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<LSP.CallHierarchyItem>.GetInstance(out var result);

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var lspItem = await ConvertToLspCallHierarchyItemAsync(
                item, document, text, resultId, i, request.TextDocument, cancellationToken).ConfigureAwait(false);
            result.Add(lspItem);
        }

        return result.ToArray();
    }

    internal static async Task<LSP.CallHierarchyItem> ConvertToLspCallHierarchyItemAsync(
        CodeAnalysis.CallHierarchy.CallHierarchyItem item,
        Document document,
        SourceText text,
        long resultId,
        int itemIndex,
        LSP.TextDocumentIdentifier textDocument,
        CancellationToken cancellationToken)
    {
        var itemDocument = document.Project.Solution.GetRequiredDocument(item.DocumentId);
        var itemText = await itemDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var uri = itemDocument.GetURI();

        // Get the line position for the span
        var linePosition = itemText.Lines.GetLinePosition(item.Span.Start);
        var linePositionSpan = itemText.Lines.GetLinePositionSpan(item.Span);

        // For selection range, we want just the name span, which we'll approximate as the first line
        var selectionRange = ProtocolConversions.LinePositionToRange(new LinePositionSpan(linePosition, linePosition));

        // Get the symbol to determine the glyph/kind
        var compilation = await itemDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation == null)
            throw new InvalidOperationException("Could not get compilation");

        var resolution = item.SymbolKey.Resolve(compilation, cancellationToken: cancellationToken);
        var symbol = resolution.Symbol;
        if (symbol == null)
            throw new InvalidOperationException("Could not resolve symbol");

        var glyph = symbol.GetGlyph();
        var lspKind = ProtocolConversions.GlyphToSymbolKind(glyph);

        return new LSP.CallHierarchyItem
        {
            Name = item.Name,
            Kind = lspKind,
            Tags = [],
            Detail = item.Detail,
            Uri = uri,
            Range = ProtocolConversions.LinePositionToRange(linePositionSpan),
            SelectionRange = selectionRange,
            Data = new CallHierarchyResolveData(resultId, itemIndex, textDocument)
        };
    }
}
