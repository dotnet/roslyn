// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer;

#pragma warning disable CA1001 // The JsonRpc instance is disposed of by the AbstractLanguageServer during shutdown
internal sealed class LanguageServerHost
#pragma warning restore CA1001 // The JsonRpc instance is disposed of by the AbstractLanguageServer during shutdown
{
    // TODO: replace this with a MEF part instead
    /// <summary>
    /// A static reference to the server instance.
    /// Used by components to send notifications and requests back to the client.
    /// </summary>
    internal static LanguageServerHost? Instance { get; private set; }

    private readonly ILogger _logger;
    private readonly AbstractLanguageServer<RequestContext> _roslynLanguageServer;
    private readonly JsonRpc _jsonRpc;

    public LanguageServerHost(Stream inputStream, Stream outputStream, ExportProvider exportProvider, ILoggerFactory loggerFactory, AbstractTypeRefResolver typeRefResolver)
    {
        var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();

        var handler = new HeaderDelimitedMessageHandler(outputStream, inputStream, messageFormatter);

        // If there is a jsonrpc disconnect or server shutdown, that is handled by the AbstractLanguageServer.  No need to do anything here.
        _jsonRpc = new JsonRpc(handler)
        {
            ExceptionStrategy = ExceptionProcessing.CommonErrorData,
        };

        var roslynLspFactory = exportProvider.GetExportedValue<ILanguageServerFactory>();

        _logger = loggerFactory.CreateLogger("LSP");
        var lspLogger = new LspServiceLogger(_logger);

        var hostServices = exportProvider.GetExportedValue<HostServicesProvider>().HostServices;
        _roslynLanguageServer = roslynLspFactory.Create(
            _jsonRpc,
            messageFormatter.JsonSerializerOptions,
            WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            lspLogger,
            hostServices,
            typeRefResolver);
    }

    public void Start()
    {
        _jsonRpc.StartListening();

        // Now that the server is started, update the our instance reference
        Instance = this;
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

    public ILspServices GetLspServices()
        => _roslynLanguageServer.GetLspServices();
}
