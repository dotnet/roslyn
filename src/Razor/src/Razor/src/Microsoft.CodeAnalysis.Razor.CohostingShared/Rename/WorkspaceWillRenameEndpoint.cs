// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[ExportLspWillRenameListener("**/*.razor")]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class WorkspaceWillRenameEndpoint(
    IRemoteServiceInvoker remoteServiceInvoker,
    ILoggerFactory loggerFactory) : ILspWillRenameListener
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<WorkspaceWillRenameEndpoint>();

    public Task<WorkspaceEdit?> HandleWillRenameAsync(RenameFilesParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var solution = context.Solution;
        if (solution is null)
        {
            _logger.LogWarning($"Got a didRenameFiles notification but didn't get a solution to work with.");
            return SpecializedTasks.Null<WorkspaceEdit>();
        }

        return HandleRequestAsync(request, solution, cancellationToken);
    }

    private async Task<WorkspaceEdit?> HandleRequestAsync(RenameFilesParams request, Solution solution, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Got a didRenameFiles notification with {request.Files.Length} renames.");

        var edit = await _remoteServiceInvoker.TryInvokeAsync<IRemoteRenameService, WorkspaceEdit?>(
            solution,
            (service, solutionInfo, cancellationToken) => service.GetFileRenameEditAsync(solutionInfo, request, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (edit is null)
        {
            _logger.LogDebug($"Remote service did not send back an edit to apply.");
            return null;
        }

        return edit;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(WorkspaceWillRenameEndpoint instance)
    {
        public Task<WorkspaceEdit?> HandleRequestAsync(RenameFilesParams request, Solution solution, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, solution, cancellationToken);
    }
}
