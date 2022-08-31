// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal class ExecuteWorkspaceCommandHandler : ILspServiceRequestHandler<ExecuteCommandParams, object?>
{
    public bool MutatesSolutionState => false;

    public static bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExecuteWorkspaceCommandHandler()
    {
    }

    public async Task<object?> HandleRequestAsync(ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var requestExecutionQueue = context.GetRequiredService<IRequestExecutionQueue<RequestContext>>();
        var lspServices = context.GetRequiredService<ILspServices>();

        var requestMethod = AbstractExecuteWorkspaceCommandHandler.GetRequestNameForCommandName(request.Command);

        var result = await requestExecutionQueue.ExecuteAsync<ExecuteCommandParams, object?>(
            request,
            requestMethod,
            lspServices,
            cancellationToken).ConfigureAwait(false);

        return result;
    }
}
