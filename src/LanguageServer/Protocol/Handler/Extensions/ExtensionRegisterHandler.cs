// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

[ExportCSharpVisualBasicStatelessLspService(typeof(ExtensionRegisterHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ExtensionRegisterHandler()
    : ILspServiceRequestHandler<ExtensionRegisterParams, ExtensionRegisterResponse>
{
    private const string MethodName = "roslyn/extensionRegister";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<ExtensionRegisterResponse> HandleRequestAsync(ExtensionRegisterParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

        if (client is not null)
        {
            var response = await client.TryInvokeAsync<IRemoteExtensionMessageHandlerService, RegisterExtensionResponse>(
                solution,
                (service, solutionInfo, cancellationToken) => service.RegisterExtensionAsync(
                    solutionInfo,
                    request.AssemblyFilePath,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!response.HasValue)
            {
                throw new InvalidOperationException("The remote message handler didn't return any value.");
            }

            return new(response.Value.WorkspaceMessageHandlers, response.Value.DocumentMessageHandlers);
        }
        else
        {
            var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
            var response = await service.RegisterExtensionAsync(
                solution,
                request.AssemblyFilePath,
                cancellationToken).ConfigureAwait(false);

            return new(response.WorkspaceMessageHandlers, response.DocumentMessageHandlers);
        }
    }
}
