// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

[ExportCSharpVisualBasicLspService(typeof(LspLoggerFactory)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class LspLoggerFactory : ILoggerFactory, ILspService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly LogConfiguration _logConfiguration;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LspLoggerFactory(LspService<IClientLanguageServerManager> clientLanguageServerManager, ServerConfiguration serverConfiguration)
    {
        var clientLanguageServerManagerValue = clientLanguageServerManager.Value;
        _logConfiguration = new(serverConfiguration.InitialLogLevel);
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(new LspLogMessageLoggerProvider(clientLanguageServerManagerValue, _logConfiguration));
        });
    }

    public LogConfiguration LogConfiguration => _logConfiguration;

    public void AddProvider(ILoggerProvider provider)
        => _loggerFactory.AddProvider(provider);

    public ILogger CreateLogger(string categoryName)
        => _loggerFactory.CreateLogger(categoryName);

    public void Dispose()
        => _loggerFactory.Dispose();
}
