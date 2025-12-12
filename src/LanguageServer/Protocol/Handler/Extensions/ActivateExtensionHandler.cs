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

[ExportCSharpVisualBasicStatelessLspService(typeof(ActivateExtensionHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ActivateExtensionHandler()
    : AbstractExtensionHandler, ILspServiceRequestHandler<ActivateExtensionParams, ActivateExtensionResponse>
{
    private const string MethodName = "server/_vs_activateExtension";

    public async Task<ActivateExtensionResponse> HandleRequestAsync(ActivateExtensionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;
        var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();

        await service.RegisterExtensionAsync(request.AssemblyFilePath, cancellationToken).ConfigureAwait(false);
        var handlerNames = await service.GetExtensionMessageNamesAsync(request.AssemblyFilePath, cancellationToken).ConfigureAwait(false);

        // Report any exceptions the extension itself caused while registering and getting the message names.
        if (handlerNames.ExtensionException is not null)
            context.Logger.LogException(handlerNames.ExtensionException);

        return new(
            handlerNames.WorkspaceMessageHandlers,
            handlerNames.DocumentMessageHandlers,
            handlerNames.ExtensionException);
    }
}
