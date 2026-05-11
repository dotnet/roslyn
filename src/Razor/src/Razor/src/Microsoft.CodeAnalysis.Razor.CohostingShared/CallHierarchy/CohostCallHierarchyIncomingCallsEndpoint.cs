// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CallHierarchy;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.CallHierarchyIncomingCallsName)]
[ExportRazorStatelessLspService(typeof(CohostCallHierarchyIncomingCallsEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostCallHierarchyIncomingCallsEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<CallHierarchyIncomingCallsParams, CallHierarchyIncomingCall[]?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(CallHierarchyIncomingCallsParams request)
        => RazorCallHierarchyResolveData.Unwrap(request.Item)?.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<CallHierarchyIncomingCall[]?> HandleRequestAsync(CallHierarchyIncomingCallsParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var razorData = RazorCallHierarchyResolveData.Unwrap(request.Item);
        if (razorData is null)
        {
            return null;
        }

        var unwrappedItem = RazorCallHierarchyResolveData.WithData(request.Item, razorData.OriginalData);

        var response = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteCallHierarchyService, RemoteResponse<CallHierarchyIncomingCall[]?>>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) => service.GetIncomingCallsAsync(solutionInfo, razorDocument.Id, unwrappedItem, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (response.Result is not CallHierarchyIncomingCall[] incomingCalls)
        {
            return null;
        }

        return Array.ConvertAll(incomingCalls, static incomingCall => new CallHierarchyIncomingCall
        {
            From = WrapRazorItem(incomingCall.From),
            FromRanges = incomingCall.FromRanges,
        });
    }

    private static CallHierarchyItem WrapRazorItem(CallHierarchyItem item)
    {
        var uri = item.Uri.GetRequiredParsedUri();
        return uri.GetDocumentFilePathFromUri().IsRazorFilePath()
            ? RazorCallHierarchyResolveData.Wrap(item, new TextDocumentIdentifier { DocumentUri = item.Uri })
            : item;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostCallHierarchyIncomingCallsEndpoint instance)
    {
        public Task<CallHierarchyIncomingCall[]?> HandleRequestAsync(CallHierarchyIncomingCallsParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
