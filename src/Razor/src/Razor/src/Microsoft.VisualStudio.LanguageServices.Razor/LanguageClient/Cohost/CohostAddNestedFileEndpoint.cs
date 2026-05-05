// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol.NestedFiles;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(RazorLSPConstants.AddNestedFileName)]
[ExportCohostStatelessLspService(typeof(CohostAddNestedFileEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostAddNestedFileEndpoint(
    IRemoteServiceInvoker remoteServiceInvoker,
    IIncompatibleProjectService incompatibleProjectService,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<AddNestedFileParams, VoidResult>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostAddNestedFileEndpoint>();

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(AddNestedFileParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override Task<VoidResult> HandleRequestAsync(
        AddNestedFileParams request,
        TextDocument razorDocument,
        CancellationToken cancellationToken)
        => Assumed.Unreachable<Task<VoidResult>>();

    protected override async Task<VoidResult> HandleRequestAsync(
        AddNestedFileParams request,
        RazorCohostRequestContext context,
        TextDocument razorDocument,
        CancellationToken cancellationToken)
    {
        var workspaceEdit = await _remoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(
                solutionInfo,
                razorDocument.Id,
                request.FileKind,
                ct),
            cancellationToken).ConfigureAwait(false);

        if (workspaceEdit is null)
        {
            _logger.LogWarning($"Remote service returned no edit for addNestedFile.");
            return new();
        }

        var razorCohostClientLanguageServerManager = context.GetRequiredService<IRazorClientLanguageServerManager>();
        var response = await razorCohostClientLanguageServerManager.SendRequestAsync<ApplyWorkspaceEditParams, ApplyWorkspaceEditResponse>(
            Methods.WorkspaceApplyEditName,
            new ApplyWorkspaceEditParams { Edit = workspaceEdit },
            cancellationToken).ConfigureAwait(false);

        if (!response.Applied)
        {
            _logger.LogWarning($"Failed to apply workspace edit for addNestedFile: {response.FailureReason}");
        }

        return new();
    }
}
