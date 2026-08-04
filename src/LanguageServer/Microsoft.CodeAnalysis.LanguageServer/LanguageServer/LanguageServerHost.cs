// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
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
    private volatile bool _hasStarted;

    internal ILogger GlobalLogger { get; }
    internal bool HasStarted => _hasStarted;

    public LanguageServerHost(
        Stream inputStream,
        Stream outputStream,
        ExportProvider exportProvider,
        AbstractTypeRefResolver typeRefResolver)
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
            throw;
        }
    }

    public void Start()
    {
        Contract.ThrowIfTrue(_hasStarted);

        // Eagerly resolve the workspace factory from the per-server LSP services, since right now the language server
        // assumes there's at least one Workspace. This as a side effect creates the actual workspace object which is
        // registered by the LspWorkspaceRegistrationEventListener.
        _ = GetLspServices().GetRequiredService<LanguageServerWorkspaceFactory>();

        _jsonRpc.StartListening();
        _hasStarted = true;
    }

    public Task WaitForExitAsync()
    {
        // Wait until the server exits.  Once complete, we can return and proceed with shutdown.
        // The server is responsible for cleaning up its resources and disposing of the `_jsonRpc` instance.
        //
        // Note - we specifically do not await `_jsonRpc.Completion` here.  This is safe (and preferred) for a few reasons:
        //   1.  The server exiting is the only signal we need to know that we're done.  Either the client has sent an explicit `exit`, or the
        //       server observed an unexpected disconnect which internally triggers a clean server exit.
        //   2.  On some platforms (Unix), `_jsonRpc.Completion` will not complete until the client closes its end of the transport or sends new data
        //       even if the `_jsonRpc` instance has been disposed of (due to a synchronous read syscall that does not observe disposal).  The server
        //       should still shutdown regardless - we've been told to exit, so exit.
        return _roslynLanguageServer.WaitForExitAsync();
    }

    public async Task AbortAsync()
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

    public ILspServices GetLspServices()
        => _roslynLanguageServer.GetLspServices();
}
