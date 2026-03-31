// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using RoslynCallHierarchyItem = Microsoft.CodeAnalysis.CallHierarchy.CallHierarchyItem;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(CallHierarchyOutgoingCallsHandler)), Shared]
[Method(Methods.CallHierarchyOutgoingCallsName)]
internal sealed class CallHierarchyOutgoingCallsHandler : ILspServiceDocumentRequestHandler<CallHierarchyOutgoingCallsParams, LSP.CallHierarchyOutgoingCall[]?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CallHierarchyOutgoingCallsHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(CallHierarchyOutgoingCallsParams request)
        => GetResolveData(request.Item).TextDocument;

    public async Task<LSP.CallHierarchyOutgoingCall[]?> HandleRequestAsync(
        CallHierarchyOutgoingCallsParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var solution = document.Project.Solution;
        var cache = context.GetRequiredLspService<CallHierarchyCache>();
        var resolveData = GetResolveData(request.Item);

        // Try to get the cached item
        var cacheEntry = cache.GetCachedEntry(resolveData.ResultId);
        if (cacheEntry is null || resolveData.ListIndex >= cacheEntry.Items.Length)
            return null;

        var item = cacheEntry.Items[resolveData.ListIndex];

        var outgoingCalls = await Microsoft.CodeAnalysis.CallHierarchy.CallHierarchyService.Instance.GetOutgoingCallsAsync(
            solution, item, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<LSP.CallHierarchyOutgoingCall>.GetInstance(out var results);

        // Get the source document for converting call site spans
        var sourceDocument = solution.GetDocument(item.DocumentId);
        if (sourceDocument is null)
            return null;

        var sourceText = await sourceDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        foreach (var call in outgoingCalls)
        {
            var calleeDocument = solution.GetDocument(call.Callee.DocumentId);
            if (calleeDocument is null)
                continue;

            var lspItem = await ConvertToLspCallHierarchyItemAsync(
                calleeDocument, call.Callee, cache, cancellationToken).ConfigureAwait(false);

            if (lspItem is null)
                continue;

            // FromRanges are relative to the source document (the caller)
            var fromRanges = call.CallSites
                .Select(span => ProtocolConversions.TextSpanToRange(span, sourceText))
                .ToArray();

            results.Add(new LSP.CallHierarchyOutgoingCall
            {
                To = lspItem,
                FromRanges = fromRanges
            });
        }

        return results.ToArray();
    }

    private static async Task<LSP.CallHierarchyItem?> ConvertToLspCallHierarchyItemAsync(
        Document document,
        RoslynCallHierarchyItem item,
        CallHierarchyCache cache,
        CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        // Cache the item for potential future requests
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
            Data = new CallHierarchyItemResolveData(resultId, 0, PrepareCallHierarchyHandler.CreateTextDocumentIdentifier(document))
        };
    }

    private static CallHierarchyItemResolveData GetResolveData(LSP.CallHierarchyItem item)
    {
        Contract.ThrowIfNull(item.Data);
        var resolveData = JsonSerializer.Deserialize<CallHierarchyItemResolveData>(
            (JsonElement)item.Data, ProtocolConversions.LspJsonSerializerOptions);
        Contract.ThrowIfNull(resolveData, "Missing data for call hierarchy request");
        return resolveData;
    }
}
