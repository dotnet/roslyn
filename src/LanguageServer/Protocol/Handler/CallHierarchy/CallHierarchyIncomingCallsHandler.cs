// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

[ExportCSharpVisualBasicStatelessLspService(typeof(CallHierarchyIncomingCallsHandler)), Shared]
[Method(LSP.Methods.CallHierarchyIncomingCallsName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CallHierarchyIncomingCallsHandler() : ILspServiceDocumentRequestHandler<LSP.CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CallHierarchyIncomingCallsParams request)
        => CallHierarchyHelpers.GetResolveData(request.Item).TextDocument;

    public async Task<LSP.CallHierarchyIncomingCall[]?> HandleRequestAsync(LSP.CallHierarchyIncomingCallsParams request, RequestContext context, CancellationToken cancellationToken)
        => await GetIncomingCallsAsync(context.GetRequiredDocument(), request.Item, allowRazorSourceGeneratedDocuments: false, cancellationToken).ConfigureAwait(false);

    internal static async Task<LSP.CallHierarchyIncomingCall[]?> GetIncomingCallsAsync(Document document, LSP.CallHierarchyItem item, bool allowRazorSourceGeneratedDocuments, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var resolveData = CallHierarchyHelpers.GetResolveData(item);

        var service = document.GetRequiredLanguageService<ICallHierarchyService>();
        var results = await service.SearchIncomingCallsAsync(
            solution,
            new CallHierarchySearchDescriptor(CallHierarchyRelationshipKind.Callers, resolveData.GetItemId()),
            documents: null,
            cancellationToken).ConfigureAwait(false);

        var incomingCalls = new List<LSP.CallHierarchyIncomingCall>();
        foreach (var result in results.Where(result => result.Item != null))
        {
            var locationsByDocumentId = result.ReferenceLocations
                .Where(location => location.IsInSource || (allowRazorSourceGeneratedDocuments && solution.GetDocument(location.SourceTree).IsRazorSourceGeneratedDocument()))
                .GroupBy(location => solution.GetDocument(location.SourceTree)?.Id);

            foreach (var locationGroup in locationsByDocumentId)
            {
                if (locationGroup.Key == null)
                    continue;

                // Including source generated documents here is relatively safe because only Razor source generated documents would be seen, due to the filtering
                // above, and Razor knows how to remap generated document locations back to the original Razor document locations.
                var callerDocument = await solution.GetDocumentAsync(locationGroup.Key, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                if (callerDocument == null)
                    continue;

                var fromItem = await CallHierarchyHelpers.CreateItemAsync(result.Item!, solution, locationGroup.Key, cancellationToken).ConfigureAwait(false);
                if (fromItem == null)
                    continue;

                var ranges = await CallHierarchyHelpers.ConvertLocationsToRangesAsync([.. locationGroup], callerDocument, cancellationToken).ConfigureAwait(false);
                incomingCalls.Add(new LSP.CallHierarchyIncomingCall
                {
                    From = fromItem,
                    FromRanges = [.. ranges],
                });
            }
        }

        return [.. incomingCalls];
    }
}
