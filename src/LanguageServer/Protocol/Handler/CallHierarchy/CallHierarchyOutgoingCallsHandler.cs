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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(CallHierarchyOutgoingCallsHandler)), Shared]
[Method(Methods.CallHierarchyOutgoingCallsName)]
internal sealed class CallHierarchyOutgoingCallsHandler : ILspServiceRequestHandler<CallHierarchyOutgoingCallsParams, LSP.CallHierarchyOutgoingCall[]?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CallHierarchyOutgoingCallsHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<LSP.CallHierarchyOutgoingCall[]?> HandleRequestAsync(
        CallHierarchyOutgoingCallsParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var callHierarchyCache = context.GetRequiredLspService<CallHierarchyCache>();

        // Extract the resolve data
        var resolveData = GetResolveData(request.Item);
        if (resolveData == null)
            return null;

        // Get the cached item
        var cacheEntry = callHierarchyCache.GetCachedEntry(resolveData.ResultId);
        if (cacheEntry == null)
            return null;

        var item = cacheEntry.CallHierarchyItem;
        var documents = context.Solution!.GetTextDocuments(resolveData.TextDocument.DocumentUri);
        var document = documents.FirstOrDefault() as Document;
        if (document == null)
            return null;

        var callHierarchyService = document.GetRequiredLanguageService<ICallHierarchyService>();

        // Find all outgoing calls
        var outgoingCalls = await callHierarchyService.FindOutgoingCallsAsync(
            document, item.Symbol, cancellationToken).ConfigureAwait(false);

        if (outgoingCalls.IsEmpty)
            return [];

        using var _ = ArrayBuilder<LSP.CallHierarchyOutgoingCall>.GetInstance(out var result);

        foreach (var outgoingCall in outgoingCalls)
        {
            var toSymbolLocation = outgoingCall.To.Symbol.Locations.FirstOrDefault();
            if (toSymbolLocation == null)
                continue;

            var toDocument = context.Solution.GetDocument(toSymbolLocation.SourceTree);
            if (toDocument == null)
                continue;

            // Create a new result ID for the "to" item (in case it's expanded later)
            var toResultId = callHierarchyCache.UpdateCache(new CallHierarchyCache.CallHierarchyCacheEntry(outgoingCall.To));

            var toItem = await PrepareCallHierarchyHandler.ConvertToLspCallHierarchyItemAsync(
                outgoingCall.To, toDocument, toResultId, cancellationToken).ConfigureAwait(false);

            var fromRanges = outgoingCall.Callsites
                .Select(loc => ProtocolConversions.LinePositionToRange(loc.GetLineSpan().Span))
                .ToArray();

            result.Add(new LSP.CallHierarchyOutgoingCall
            {
                To = toItem,
                FromRanges = fromRanges
            });
        }

        return result.ToArray();
    }

    private static CallHierarchyResolveData? GetResolveData(LSP.CallHierarchyItem item)
    {
        if (item.Data is not Newtonsoft.Json.Linq.JToken token)
            return null;

        return token.ToObject<CallHierarchyResolveData>();
    }
}
