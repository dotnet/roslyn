// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

[ExportCSharpVisualBasicStatelessLspService(typeof(DispatchWorkspaceExtensionMessageHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DispatchWorkspaceExtensionMessageHandler()
    : AbstractExtensionHandler, ILspServiceRequestHandler<DispatchWorkspaceExtensionMessageParams, DispatchExtensionMessageResponse>
{
    private const string MethodName = "workspace/_vs_dispatchExtensionMessage";

    public async Task<DispatchExtensionMessageResponse> HandleRequestAsync(DispatchWorkspaceExtensionMessageParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;

        var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
        var (response, extensionWasUnloaded, exception) = await service.HandleExtensionWorkspaceMessageAsync(
            solution, request.MessageName, request.Message, cancellationToken).ConfigureAwait(false);

        // Report any exceptions the extension itself caused while handling the request.
        if (exception is not null)
            context.Logger.LogException(exception);

        return new DispatchExtensionMessageResponse(response, extensionWasUnloaded, exception);
    }
}
