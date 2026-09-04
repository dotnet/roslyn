// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer;

#pragma warning disable CA1001 // The JsonRpc instance is disposed of by the AbstractLanguageServer during shutdown
internal sealed class LanguageServerHost
#pragma warning restore CA1001 // The JsonRpc instance is disposed of by the AbstractLanguageServer during shutdown
{
    private readonly AbstractLanguageServer<RequestContext> _roslynLanguageServer;
    private readonly JsonRpc _jsonRpc;
    private readonly RoslynTelemetry _telemetry;
    private LanguageServerTelemetry? _ownedTelemetry;
    private volatile bool _hasStarted;

    internal ILogger GlobalLogger { get; }
    internal bool HasStarted => _hasStarted;

    public LanguageServerHost(
        Stream inputStream,
        Stream outputStream,
        ExportProvider exportProvider,
        AbstractTypeRefResolver typeRefResolver,
        LanguageServerTelemetry? processTelemetryService)
    {
        var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();

        var handler = new HeaderDelimitedMessageHandler(outputStream, inputStream, messageFormatter);

        // If there is a jsonrpc disconnect or server shutdown, that is handled by the AbstractLanguageServer.  No need to do anything here.
        _jsonRpc = new JsonRpc(handler)
        {
            ExceptionStrategy = ExceptionProcessing.CommonErrorData,
        };

        try
        {
            var serverConfiguration = exportProvider.GetExportedValue<ServerConfiguration>();

            if (serverConfiguration.IsDaemon)
            {
                // Every daemon server needs an isolated router even when VS telemetry is disabled, so sinks
                // registered by one server cannot receive another server's events.
                _telemetry = new RoslynTelemetry();
                _ownedTelemetry = processTelemetryService?.CreatePerServerSession(_telemetry);
            }
            else
            {
                _telemetry = processTelemetryService?.Telemetry ?? RoslynTelemetry.Current;
            }

            // In daemon mode the ambient here is the process owner, not this server, so establish the server's
            // instance for everything constructed below that captures it - the RoslynTelemetry LSP service, and the
            // request queue's processing loop, which runs for the life of the server on this context.
            using var telemetryScope = RoslynTelemetry.SetCurrent(_telemetry);

            var roslynLspFactory = exportProvider.GetExportedValue<CSharpVisualBasicLanguageServerFactory>();

            var hostServices = exportProvider.GetExportedValue<HostServicesProvider>().HostServices;
            _roslynLanguageServer = roslynLspFactory.Create(
                _jsonRpc,
                messageFormatter.JsonSerializerOptions,
                WellKnownLspServerKinds.CSharpVisualBasicLspServer,
                hostServices,
                typeRefResolver);

            GlobalLogger = _roslynLanguageServer.GetLspServices().GetRequiredService<ILoggerFactory>().CreateLogger("Global");
        }
        catch
        {
            _jsonRpc.Dispose();
            DisposeOwnedTelemetry();
            throw;
        }
    }

    public void Start()
    {
        // StreamJsonRpc captures the execution context at StartListening (not at construction), and dispatches
        // every inbound message on it, so this scope - not the constructor's - is what attributes LSP requests to
        // this server. The daemon calls Start on its own context, so the ambient here is not yet this server's.
        using var telemetryScope = RoslynTelemetry.SetCurrent(_telemetry);

        Contract.ThrowIfTrue(_hasStarted);

        // Eagerly resolve the workspace factory from the per-server LSP services, since right now the language server
        // assumes there's at least one Workspace. This as a side effect creates the actual workspace object which is
        // registered by the LspWorkspaceRegistrationEventListener.
        _ = GetLspServices().GetRequiredService<LanguageServerWorkspaceFactory>();

        _jsonRpc.StartListening();
        _hasStarted = true;
    }

    public async Task WaitForExitAsync()
    {
        // The daemon supervises every server from its own context, so attribute this server's shutdown - including
        // the telemetry session flush in DisposeOwnedTelemetry - to the server rather than to the daemon.
        using var telemetryScope = RoslynTelemetry.SetCurrent(_telemetry);

        // Wait until the server exits.  Once complete, we can return and proceed with shutdown.
        // The server is responsible for cleaning up its resources and disposing of the `_jsonRpc` instance.
        //
        // Note - we specifically do not await `_jsonRpc.Completion` here.  This is safe (and preferred) for a few reasons:
        //   1.  The server exiting is the only signal we need to know that we're done.  Either the client has sent an explicit `exit`, or the
        //       server observed an unexpected disconnect which internally triggers a clean server exit.
        //   2.  On some platforms (Unix), `_jsonRpc.Completion` will not complete until the client closes its end of the transport or sends new data
        //       even if the `_jsonRpc` instance has been disposed of (due to a synchronous read syscall that does not observe disposal).  The server
        //       should still shutdown regardless - we've been told to exit, so exit.
        try
        {
            await _roslynLanguageServer.WaitForExitAsync().ConfigureAwait(false);
        }
        finally
        {
            DisposeOwnedTelemetry();
        }
    }

    public async Task AbortAsync()
    {
        // Startup is aborted from the daemon's context, so attribute the shutdown/exit of this server - which
        // never began listening, and so has no context of its own to inherit - to the server itself.
        using var telemetryScope = RoslynTelemetry.SetCurrent(_telemetry);

        try
        {
            Exception? shutdownException = null;
            try
            {
                await _roslynLanguageServer.ShutdownAsync("Aborting language server startup").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                shutdownException = ex;
            }

            await _roslynLanguageServer.ExitAsync().ConfigureAwait(false);

            if (shutdownException is not null)
                throw new InvalidOperationException("Language server cleanup failed during startup abort.", shutdownException);
        }
        finally
        {
            DisposeOwnedTelemetry();
        }
    }

    public ILspServices GetLspServices()
        => _roslynLanguageServer.GetLspServices();

    private void DisposeOwnedTelemetry()
        => Interlocked.Exchange(ref _ownedTelemetry, null)?.Dispose();
}
