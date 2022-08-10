// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.WorkspaceCommand;

[ExportGeneralStatelessLspService(typeof(ExecuteWorkspaceCommandHandler)), Shared]
[Method(Methods.WorkspaceExecuteCommandName)]
internal class ExecuteWorkspaceCommandHandler : IRoslynRequestHandler<ExecuteCommandParams, object?>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExecuteWorkspaceCommandHandler()
    {
    }

    public object? GetTextDocumentIdentifier(ExecuteCommandParams request)
    {
        return null;
    }

    public async Task<object?> HandleRequestAsync(ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var requestDispatcher = context.GetRequiredService<IRequestDispatcher<RequestContext>>();
        var requestExecutionQueue = context.GetRequiredService<IRequestExecutionQueue<RequestContext>>();

        var requestMethod = AbstractExecuteWorkspaceCommandHandler.GetRequestNameForCommandName(request.Command);

        var result = await requestDispatcher.ExecuteRequestAsync<ExecuteCommandParams, object?>(
            requestMethod,
            request,
            requestExecutionQueue,
            cancellationToken).ConfigureAwait(false);

        return result;
    }
}
