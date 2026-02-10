// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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

        // If we are running in process with the client, then we don't need to listen for it to exit.
        if (request.ProcessId.HasValue && request.ProcessId.Value != Process.GetCurrentProcess().Id)
        {
            // We were given a client process ID. Monitor that process and exit if it exits.
            _ = WaitForClientProcessExitAsync(request.ProcessId.Value);
        }

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

    static async Task WaitForClientProcessExitAsync(int clientProcessId)
    {
        try
        {
            var clientProcessExitTask = new TaskCompletionSource<bool>();

            using var clientProcess = Process.GetProcessById(clientProcessId);
            clientProcess.EnableRaisingEvents = true;
            clientProcess.Exited += (sender, args) => clientProcessExitTask.SetResult(true);

            if (!clientProcess.HasExited)
            {
                // Wait for the client process to exit.
                await clientProcessExitTask.Task.ConfigureAwait(false);
            }
        }
        catch (ArgumentException)
        {
            // The process has already exited or was never running.
        }

        Process.GetCurrentProcess().Kill();
    }
}
