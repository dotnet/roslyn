// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentPrepareRenameName)]
[ExportRazorStatelessLspService(typeof(CohostPrepareRenameEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostPrepareRenameEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker)
    : AbstractCohostDocumentEndpoint<PrepareRenameParams, LspRange?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(PrepareRenameParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<LspRange?> HandleRequestAsync(PrepareRenameParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => await HandleRequestAsync(razorDocument, request, cancellationToken).ConfigureAwait(false);

    private async Task<LspRange?> HandleRequestAsync(TextDocument razorDocument, Position position, CancellationToken cancellationToken)
        => await HandleRequestAsync(
            razorDocument,
            new PrepareRenameParams
            {
                TextDocument = new TextDocumentIdentifier { DocumentUri = razorDocument.CreateDocumentUri() },
                Position = position
            },
            cancellationToken).ConfigureAwait(false);

    private async Task<LspRange?> HandleRequestAsync(TextDocument razorDocument, PrepareRenameParams request, CancellationToken cancellationToken)
    {
        var result = await _remoteServiceInvoker.TryInvokeAsync<IRemoteRenameService, RemoteResponse<LspRange?>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetPrepareRenameRangeAsync(solutionInfo, razorDocument.Id, request.Position, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (result.Result is { } range)
        {
            return range;
        }

        if (result.StopHandling)
        {
            return null;
        }

        return await _requestInvoker.MakeHtmlLspRequestAsync<PrepareRenameParams, LspRange>(
            razorDocument,
            Methods.TextDocumentPrepareRenameName,
            request,
            cancellationToken).ConfigureAwait(false);
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostPrepareRenameEndpoint instance)
    {
        public Task<LspRange?> HandleRequestAsync(TextDocument razorDocument, Position position, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(razorDocument, position, cancellationToken);
    }
}
