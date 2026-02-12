// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[Method(Methods.InitializeName)]
internal sealed class InitializeHandler() : ILspServiceRequestHandler<InitializeParams, InitializeResult>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    public async Task<InitializeResult> HandleRequestAsync(InitializeParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var clientCapabilitiesManager = context.GetRequiredLspService<IInitializeManager>();
        var clientCapabilities = request.Capabilities;
        clientCapabilitiesManager.SetInitializeParams(request);

        if (request.ProcessId is int clientProcessId && RoslynLanguageServer.TryRegisterClientProcessId(clientProcessId))
            context.Logger.LogInformation("Monitoring client process {clientProcessId} for exit", clientProcessId);

        var capabilitiesProvider = context.GetRequiredLspService<ICapabilitiesProvider>();
        var serverCapabilities = capabilitiesProvider.GetCapabilities(clientCapabilities);

        // Record a telemetry event indicating what capabilities are being provided by the server.
        // Useful for figuring out if a particular session is opted into an LSP feature.
        Logger.Log(FunctionId.LSP_Initialize, KeyValueLogMessage.Create(m =>
        {
            m["serverKind"] = context.ServerKind.ToTelemetryString();
            m["capabilities"] = JsonSerializer.Serialize(serverCapabilities, ProtocolConversions.LspJsonSerializerOptions);
        }));

        return new InitializeResult
        {
            Capabilities = serverCapabilities,
        };
    }
}
