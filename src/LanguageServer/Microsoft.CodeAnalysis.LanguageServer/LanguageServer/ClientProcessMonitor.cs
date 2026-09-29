// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportCSharpVisualBasicLspService(typeof(ClientProcessMonitor)), Shared(LspServiceComposition.SharingBoundary)]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used directly by tests")]
internal sealed class ClientProcessMonitor(ServerConfiguration serverConfiguration, IInitializeManager initializeManager) : IClientProcessMonitor
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ClientProcessMonitor(ServerConfiguration serverConfiguration, LspService<IInitializeManager> initializeManager)
        : this(serverConfiguration, initializeManager.Value)
    {
    }

    public IClientProcessMonitor.ShutdownStrategy Strategy => serverConfiguration.IsDaemon ? IClientProcessMonitor.ShutdownStrategy.LSPShutdown : IClientProcessMonitor.ShutdownStrategy.ProcessExit;

    public int? GetClientProcessId()
    {
        // In daemon mode, monitor the process from this connection's initialize params. In single-server mode,
        // Program monitors the command-line process ID before LSP initialization, so only start this logical-server
        // monitor when the command-line ID was absent and initialize supplied one.

        var initializeParams = initializeManager.TryGetInitializeParams();
        Contract.ThrowIfNull(initializeParams, "Initialize has not been called yet");

        var processId = serverConfiguration.IsDaemon || serverConfiguration.ClientProcessId is null
            ? initializeParams.ProcessId
            : null;

        return processId == RoslynLanguageServer.ServerProcessId ? null : processId;
    }

}
