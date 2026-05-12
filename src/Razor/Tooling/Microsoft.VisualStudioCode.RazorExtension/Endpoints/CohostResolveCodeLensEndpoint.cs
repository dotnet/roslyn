// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces.CodeLens;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.CodeLensResolveName)]
[ExportRazorStatelessLspService(typeof(CohostResolveCodeLensEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostResolveCodeLensEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<LspCodeLens, LspCodeLens>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;
    protected override bool RequiresLSPSolution => true;

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(LspCodeLens request)
        => RazorCodeLensResolveData.Unwrap(request).TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<LspCodeLens?> HandleRequestAsync(LspCodeLens request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var result = await _remoteServiceInvoker.TryInvokeAsync<IRemoteCodeLensService, LspCodeLens?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.ResolveCodeLensAsync(solutionInfo, razorDocument.Id, request, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostResolveCodeLensEndpoint instance)
    {
        public RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(LspCodeLens request)
            => instance.GetRazorTextDocumentIdentifier(request);

        public Task<LspCodeLens?> HandleRequestAsync(LspCodeLens request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
