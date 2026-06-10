// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal class ClientProcessMonitor(ServerConfiguration serverConfiguration, IInitializeManager initializeManager) : IClientProcessMonitor
{
    public IClientProcessMonitor.ShutdownStrategy Strategy => serverConfiguration.IsDaemon ? IClientProcessMonitor.ShutdownStrategy.LSPShutdown : IClientProcessMonitor.ShutdownStrategy.ProcessExit;

    public int? GetClientProcessId()
    {
        // Get the process id of the client process to monitor.  In single server mode this comes from the CLI arg or the initialize params.
        // In daemon mode, we only read the process id from the initialize params as the CLI arg only captures the client that launched the daemon.

        var initializeParams = initializeManager?.TryGetInitializeParams();
        Contract.ThrowIfNull(initializeParams, "Initialize has not been called yet");

        return serverConfiguration.IsDaemon ? initializeParams.ProcessId : serverConfiguration?.ClientProcessId ?? initializeParams.ProcessId;
    }

    [ExportCSharpVisualBasicLspServiceFactory(typeof(ClientProcessMonitor)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private class Factory(ServerConfiguration serverConfiguration) : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var initializeManager = lspServices.GetRequiredService<IInitializeManager>();
            return new ClientProcessMonitor(serverConfiguration, initializeManager);
        }
    }
}