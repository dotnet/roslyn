// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CallHierarchy;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.CallHierarchyOutgoingCallsName)]
[ExportRazorStatelessLspService(typeof(CohostCallHierarchyOutgoingCallsEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostCallHierarchyOutgoingCallsEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<CallHierarchyOutgoingCallsParams, CallHierarchyOutgoingCall[]?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(CallHierarchyOutgoingCallsParams request)
        => RazorCallHierarchyResolveData.Unwrap(request.Item)?.TextDocument;

    protected override async Task<CallHierarchyOutgoingCall[]?> HandleRequestAsync(CallHierarchyOutgoingCallsParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var razorData = RazorCallHierarchyResolveData.Unwrap(request.Item);
        if (razorData is null)
        {
            return null;
        }

        var unwrappedItem = RazorCallHierarchyResolveData.WithData(request.Item, razorData.OriginalData);

        var response = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteCallHierarchyService, RemoteResponse<CallHierarchyOutgoingCall[]?>>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.GetOutgoingCallsAsync(solutionInfo, razorDocument.Id, unwrappedItem, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (response.Result is not CallHierarchyOutgoingCall[] outgoingCalls)
        {
            return null;
        }

        return Array.ConvertAll(outgoingCalls, static outgoingCall => new CallHierarchyOutgoingCall
        {
            To = WrapRazorItem(outgoingCall.To),
            FromRanges = outgoingCall.FromRanges,
        });
    }

    private static CallHierarchyItem WrapRazorItem(CallHierarchyItem item)
    {
        var uri = item.Uri.GetRequiredSystemUri();
        return uri.GetDocumentFilePathFromUri().IsRazorFilePath()
            ? RazorCallHierarchyResolveData.Wrap(item, new TextDocumentIdentifier { DocumentUri = item.Uri })
            : item;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostCallHierarchyOutgoingCallsEndpoint instance)
    {
        public Task<CallHierarchyOutgoingCall[]?> HandleRequestAsync(CallHierarchyOutgoingCallsParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
