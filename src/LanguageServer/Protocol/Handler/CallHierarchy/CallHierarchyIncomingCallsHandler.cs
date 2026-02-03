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

[ExportCSharpVisualBasicStatelessLspService(typeof(CallHierarchyIncomingCallsHandler)), Shared]
[Method(LSP.Methods.CallHierarchyIncomingCallsName)]
internal sealed class CallHierarchyIncomingCallsHandler :
    ILspServiceRequestHandler<LSP.CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>,
    ITextDocumentIdentifierHandler<LSP.CallHierarchyIncomingCallsParams, LSP.TextDocumentIdentifier?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CallHierarchyIncomingCallsHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CallHierarchyIncomingCallsParams request)
    {
        // Extract text document identifier from the item's URI
        return new LSP.TextDocumentIdentifier { DocumentUri = request.Item.Uri };
    }

    public async Task<LSP.CallHierarchyIncomingCall[]?> HandleRequestAsync(
        LSP.CallHierarchyIncomingCallsParams request,
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

        var incomingCalls = await callHierarchyService.GetIncomingCallsAsync(
            solution, item, cancellationToken).ConfigureAwait(false);

        if (incomingCalls.IsEmpty)
            return [];

        using var _ = ArrayBuilder<LSP.CallHierarchyIncomingCall>.GetInstance(out var result);

        foreach (var incomingCall in incomingCalls)
        {
            var fromDocument = solution.GetRequiredDocument(incomingCall.From.DocumentId);
            var fromText = await fromDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            // Store the from item in cache
            var fromResultId = callHierarchyCache.UpdateCache(new CallHierarchyCache.CallHierarchyCacheEntry([incomingCall.From]));
            
            var lspFromItem = await PrepareCallHierarchyHandler.ConvertToLspCallHierarchyItemAsync(
                incomingCall.From, document, fromText, fromResultId, 0, resolveData.TextDocument, cancellationToken).ConfigureAwait(false);

            // Convert call locations to ranges
            using var rangesBuilder = ArrayBuilder<LSP.Range>.GetInstance(out var ranges);
            foreach (var (documentId, span) in incomingCall.FromSpans)
            {
                var callDocument = solution.GetDocument(documentId);
                if (callDocument == null)
                    continue;

                var callText = await callDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var linePositionSpan = callText.Lines.GetLinePositionSpan(span);
                ranges.Add(ProtocolConversions.LinePositionToRange(linePositionSpan));
            }

            result.Add(new LSP.CallHierarchyIncomingCall
            {
                From = lspFromItem,
                FromRanges = ranges.ToArray()
            });
        }

        return result.ToArray();
    }
}
