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
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(CallHierarchyOutgoingCallsHandler)), Shared]
[Method(LSP.Methods.CallHierarchyOutgoingCallsName)]
internal sealed class CallHierarchyOutgoingCallsHandler :
    ILspServiceRequestHandler<LSP.CallHierarchyOutgoingCallsParams, LSP.CallHierarchyOutgoingCall[]?>,
    ITextDocumentIdentifierHandler<LSP.CallHierarchyOutgoingCallsParams, LSP.TextDocumentIdentifier?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CallHierarchyOutgoingCallsHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CallHierarchyOutgoingCallsParams request)
    {
        // Extract text document identifier from the item's URI
        return new LSP.TextDocumentIdentifier { DocumentUri = request.Item.Uri };
    }

    public async Task<LSP.CallHierarchyOutgoingCall[]?> HandleRequestAsync(
        LSP.CallHierarchyOutgoingCallsParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var solution = context.Solution;
        Contract.ThrowIfNull(solution);

        // Get the item from the cache
        var resolveData = CallHierarchyHelpers.GetCallHierarchyResolveData(request.Item);
        if (resolveData == null)
            return null;

        var callHierarchyCache = context.GetRequiredLspService<CallHierarchyCache>();
        var item = CallHierarchyHelpers.GetCallHierarchyItem(resolveData, callHierarchyCache);
        if (item == null)
            return null;

        var document = solution.GetDocument(item.DocumentId);
        if (document == null)
            return null;

        var callHierarchyService = document.GetLanguageService<ICallHierarchyService>();
        if (callHierarchyService == null)
            return null;

        var outgoingCalls = await callHierarchyService.GetOutgoingCallsAsync(
            solution, item, cancellationToken).ConfigureAwait(false);

        if (outgoingCalls.IsEmpty)
            return [];

        using var _ = ArrayBuilder<LSP.CallHierarchyOutgoingCall>.GetInstance(out var result);

        foreach (var outgoingCall in outgoingCalls)
        {
            var toDocument = solution.GetRequiredDocument(outgoingCall.To.DocumentId);
            var toText = await toDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            // Store the to item in cache
            var toResultId = callHierarchyCache.UpdateCache(new CallHierarchyCache.CallHierarchyCacheEntry([outgoingCall.To]));

            var lspToItem = await PrepareCallHierarchyHandler.ConvertToLspCallHierarchyItemAsync(
                outgoingCall.To, document, toText, toResultId, 0, resolveData.TextDocument, cancellationToken).ConfigureAwait(false);

            // Convert call locations to ranges
            using var rangesBuilder = ArrayBuilder<LSP.Range>.GetInstance(out var ranges);
            foreach (var (documentId, span) in outgoingCall.FromSpans)
            {
                var callDocument = solution.GetDocument(documentId);
                if (callDocument == null)
                    continue;

                var callText = await callDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var linePositionSpan = callText.Lines.GetLinePositionSpan(span);
                ranges.Add(ProtocolConversions.LinePositionToRange(linePositionSpan));
            }

            result.Add(new LSP.CallHierarchyOutgoingCall
            {
                To = lspToItem,
                FromRanges = ranges.ToArray()
            });
        }

        return result.ToArray();
    }
}
