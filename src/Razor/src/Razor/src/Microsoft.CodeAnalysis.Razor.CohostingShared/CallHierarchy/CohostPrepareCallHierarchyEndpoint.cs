// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
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
[CohostEndpoint(Methods.PrepareCallHierarchyName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostPrepareCallHierarchyEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostPrepareCallHierarchyEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<CallHierarchyPrepareParams, CallHierarchyItem[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.CallHierarchy?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.PrepareCallHierarchyName,
                RegisterOptions = new CallHierarchyRegistrationOptions(),
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(CallHierarchyPrepareParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<CallHierarchyItem[]?> HandleRequestAsync(CallHierarchyPrepareParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var response = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteCallHierarchyService, RemoteResponse<CallHierarchyItem[]?>>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.PrepareCallHierarchyAsync(solutionInfo, razorDocument.Id, request.Position, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (response.Result is not CallHierarchyItem[] items)
        {
            return null;
        }

        return Array.ConvertAll(items, item => WrapRazorItem(item, request.TextDocument));
    }

    private static CallHierarchyItem WrapRazorItem(CallHierarchyItem item, TextDocumentIdentifier textDocument)
    {
        var uri = item.Uri.GetRequiredParsedUri();
        return uri.GetDocumentFilePathFromUri().IsRazorFilePath()
            ? RazorCallHierarchyResolveData.Wrap(item, textDocument.WithUri(uri))
            : item;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostPrepareCallHierarchyEndpoint instance)
    {
        public Task<CallHierarchyItem[]?> HandleRequestAsync(CallHierarchyPrepareParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
