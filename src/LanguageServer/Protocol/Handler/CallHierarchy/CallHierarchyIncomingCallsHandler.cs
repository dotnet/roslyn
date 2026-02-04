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

[ExportCSharpVisualBasicStatelessLspService(typeof(CallHierarchyIncomingCallsHandler)), Shared]
[Method(Methods.CallHierarchyIncomingCallsName)]
internal sealed class CallHierarchyIncomingCallsHandler : ILspServiceRequestHandler<CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CallHierarchyIncomingCallsHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<LSP.CallHierarchyIncomingCall[]?> HandleRequestAsync(
        CallHierarchyIncomingCallsParams request,
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

        // Find all incoming calls
        var incomingCalls = await callHierarchyService.FindIncomingCallsAsync(
            document, item.Symbol, cancellationToken).ConfigureAwait(false);

        if (incomingCalls.IsEmpty)
            return [];

        using var _ = ArrayBuilder<LSP.CallHierarchyIncomingCall>.GetInstance(out var result);

        foreach (var incomingCall in incomingCalls)
        {
            var fromSymbolLocation = incomingCall.From.Symbol.Locations.FirstOrDefault();
            if (fromSymbolLocation == null)
                continue;

            var fromDocument = context.Solution.GetDocument(fromSymbolLocation.SourceTree);
            if (fromDocument == null)
                continue;

            // Create a new result ID for the "from" item (in case it's expanded later)
            var fromResultId = callHierarchyCache.UpdateCache(new CallHierarchyCache.CallHierarchyCacheEntry(incomingCall.From));

            var fromItem = await PrepareCallHierarchyHandler.ConvertToLspCallHierarchyItemAsync(
                incomingCall.From, fromDocument, fromResultId, cancellationToken).ConfigureAwait(false);

            var fromRanges = incomingCall.Callsites
                .Select(loc => ProtocolConversions.LinePositionToRange(loc.GetLineSpan().Span))
                .ToArray();

            result.Add(new LSP.CallHierarchyIncomingCall
            {
                From = fromItem,
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
