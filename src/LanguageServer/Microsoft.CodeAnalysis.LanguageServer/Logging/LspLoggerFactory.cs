// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using RoslynTelemetry = Microsoft.CodeAnalysis.Internal.Log.RoslynTelemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

[ExportCSharpVisualBasicLspServiceFactory(typeof(LspLoggerFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspLoggerFactoryFactory(ServerConfiguration serverConfiguration) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new LspLoggerFactory(
            lspServices.GetRequiredService<IClientLanguageServerManager>(),
            serverConfiguration,
            lspServices.GetRequiredService<RoslynTelemetry>());
}

internal sealed class LspLoggerFactory : ILoggerFactory, ILspService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly LogConfiguration _logConfiguration;
    private readonly IDisposable _telemetryRegistration;

    public LspLoggerFactory(
        IClientLanguageServerManager clientLanguageServerManager,
        ServerConfiguration serverConfiguration,
        RoslynTelemetry telemetry)
    {
        _logConfiguration = new(serverConfiguration.InitialLogLevel);
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(new LspLogMessageLoggerProvider(clientLanguageServerManager, _logConfiguration));
        });

        _telemetryRegistration = telemetry.AddEventSink(new LoggerEventSink(_loggerFactory.CreateLogger(nameof(RoslynTelemetry))));
    }

    public LogConfiguration LogConfiguration => _logConfiguration;

    public void AddProvider(ILoggerProvider provider)
        => _loggerFactory.AddProvider(provider);

    public ILogger CreateLogger(string categoryName)
        => _loggerFactory.CreateLogger(categoryName);

    public void Dispose()
    {
        _telemetryRegistration.Dispose();
        _loggerFactory.Dispose();
    }
}
