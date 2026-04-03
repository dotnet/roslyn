// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

[ExportCSharpVisualBasicStatelessLspService(typeof(CallHierarchyOutgoingCallsHandler)), Shared]
[Method(LSP.Methods.CallHierarchyOutgoingCallsName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CallHierarchyOutgoingCallsHandler() : ILspServiceDocumentRequestHandler<LSP.CallHierarchyOutgoingCallsParams, LSP.CallHierarchyOutgoingCall[]?>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CallHierarchyOutgoingCallsParams request)
        => CallHierarchyHelpers.GetResolveData(request.Item).TextDocument;

    public async Task<LSP.CallHierarchyOutgoingCall[]?> HandleRequestAsync(LSP.CallHierarchyOutgoingCallsParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var solution = document.Project.Solution;
        var resolveData = CallHierarchyHelpers.GetResolveData(request.Item);

        var service = document.GetRequiredLanguageService<ICallHierarchyService>();
        var results = await service.SearchOutgoingCallsAsync(
            solution,
            resolveData.GetItemId(),
            ImmutableHashSet.Create(document),
            cancellationToken).ConfigureAwait(false);

        var outgoingCalls = new List<LSP.CallHierarchyOutgoingCall>();
        foreach (var result in results.Where(result => result.Item != null))
        {
            var toItem = await CallHierarchyHelpers.CreateItemAsync(result.Item!, solution, cancellationToken).ConfigureAwait(false);
            if (toItem == null)
                continue;

            var ranges = await CallHierarchyHelpers.ConvertLocationsToRangesAsync(result.ReferenceLocations, document, cancellationToken).ConfigureAwait(false);
            outgoingCalls.Add(new LSP.CallHierarchyOutgoingCall
            {
                To = toItem,
                FromRanges = [.. ranges],
            });
        }

        return [.. outgoingCalls];
    }
}
