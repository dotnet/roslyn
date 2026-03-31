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

[ExportCSharpVisualBasicStatelessLspService(typeof(CallHierarchyIncomingCallsHandler)), Shared]
[Method(Methods.CallHierarchyIncomingCallsName)]
internal sealed class CallHierarchyIncomingCallsHandler : ILspServiceDocumentRequestHandler<CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CallHierarchyIncomingCallsHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(CallHierarchyIncomingCallsParams request)
        => GetResolveData(request.Item).TextDocument;

    public async Task<LSP.CallHierarchyIncomingCall[]?> HandleRequestAsync(
        CallHierarchyIncomingCallsParams request,
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

        var incomingCalls = await Microsoft.CodeAnalysis.CallHierarchy.CallHierarchyService.Instance.GetIncomingCallsAsync(
            solution, item, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<LSP.CallHierarchyIncomingCall>.GetInstance(out var results);

        foreach (var call in incomingCalls)
        {
            var callerDocument = solution.GetDocument(call.Caller.DocumentId);
            if (callerDocument is null)
                continue;

            var lspItem = await ConvertToLspCallHierarchyItemAsync(
                callerDocument, call.Caller, cache, cancellationToken).ConfigureAwait(false);

            if (lspItem is null)
                continue;

            var callerText = await callerDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var fromRanges = call.CallSites
                .Select(span => ProtocolConversions.TextSpanToRange(span, callerText))
                .ToArray();

            results.Add(new LSP.CallHierarchyIncomingCall
            {
                From = lspItem,
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
