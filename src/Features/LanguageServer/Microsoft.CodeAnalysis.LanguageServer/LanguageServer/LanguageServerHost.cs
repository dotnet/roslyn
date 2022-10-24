// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LanguageServerHost
{
    private readonly JsonRpc _jsonRpc;
    private readonly ILogger _logger;
    private readonly AbstractLanguageServer<RequestContext> _roslynLanguageServer;

    public LanguageServerHost(Stream inputStream, Stream outputStream, ILogger logger, ExportProvider exportProvider)
    {
        _logger = logger;

        var handler = new HeaderDelimitedMessageHandler(outputStream, inputStream, new JsonMessageFormatter());

        // If there is a jsonrpc disconnect or server shutdown, that is handled by the AbstractLanguageServer.  No need to do anything here.
        _jsonRpc = new JsonRpc(handler)
        {
            ExceptionStrategy = ExceptionProcessing.CommonErrorData,
        };

        var roslynLspFactory = exportProvider.GetExportedValue<ILanguageServerFactory>();
        var capabilitiesProvider = new ServerCapabilitiesProvider();
        var lspLogger = new HostLspLogger(logger);
        _roslynLanguageServer = roslynLspFactory.Create(_jsonRpc, capabilitiesProvider, WellKnownLspServerKinds.CSharpVisualBasicLspServer, lspLogger);
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting server...");
        _jsonRpc.StartListening();
        await _jsonRpc.Completion.ConfigureAwait(false);
        await _roslynLanguageServer.WaitForExitAsync().ConfigureAwait(false);
    }
}
