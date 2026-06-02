// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CommandLine;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using RoslynLog = Microsoft.CodeAnalysis.Internal.Log;

WindowsErrorReporting.SetErrorModeOnWindows();

var command = LanguageServerCommandLine.CreateCommand(RunAsync);
var invocationConfiguration = new InvocationConfiguration()
{
    // By default, System.CommandLine will catch all exceptions, log them to the console, and return a non-zero exit code.
    // Unfortunately this makes .NET's crash dump collection environment variables (e.g. 'DOTNET_DbgEnableMiniDump')
    // entirely useless as it never detects an actual crash.  Disable this behavior so we can collect crash dumps when asked to.
    EnableDefaultExceptionHandler = false
};
return await command.Parse(args).InvokeAsync(invocationConfiguration, CancellationToken.None);

static async Task RunAsync(ServerConfiguration serverConfiguration, CancellationToken cancellationToken)
{
    if (serverConfiguration.UseStdIo && serverConfiguration.ServerPipeName is not null)
    {
        throw new InvalidOperationException("Server cannot be started with both --stdio and --pipe options.");
    }

    if (!serverConfiguration.UseStdIo && serverConfiguration.ServerPipeName is null)
    {
        throw new InvalidOperationException("Server must be started with either --stdio or --pipe option.");
    }

    if (serverConfiguration.UseStdIo)
    {
        // Redirect Console.Out to try prevent the standard output stream from being corrupted.
        // This should be done before the logger is created as it can write to the standard output.
        Console.SetOut(new StreamWriter(Console.OpenStandardError()));
    }

    // Create a console logger as a fallback to use before the LSP server starts.
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        // The actual logger is responsible for deciding whether to log based on the current log level.
        // The factory should be configured to log everything.
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddProvider(new LspLogMessageLoggerProvider(fallbackLoggerFactory:
            // Add a console logger as a fallback for when the LSP server has not finished initializing.
            LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddConsole();
                // The console logger outputs control characters on unix for colors which don't render correctly in VSCode.
                builder.AddSimpleConsole(formatterOptions => formatterOptions.ColorBehavior = LoggerColorBehavior.Disabled);
            }), serverConfiguration
        ));
    });

    var logger = loggerFactory.CreateLogger<Program>();

    logger.LogInformation("Server started with process ID {processId}", Environment.ProcessId);
    if (serverConfiguration.LaunchDebugger)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Debugger.Launch() only works on Windows.
            _ = Debugger.Launch();
        }
        else
        {
            var timeout = TimeSpan.FromMinutes(2);
            logger.LogCritical($"Waiting {timeout:g} for a debugger to attach");
            using var timeoutSource = new CancellationTokenSource(timeout);
            while (!Debugger.IsAttached && !timeoutSource.Token.IsCancellationRequested)
            {
                await Task.Delay(100, CancellationToken.None);
            }
        }
    }

    logger.LogTrace($".NET Runtime Version: {RuntimeInformation.FrameworkDescription}");
    var extensionManager = ExtensionAssemblyManager.Create(serverConfiguration, loggerFactory);
    var assemblyLoader = new CustomExportAssemblyLoader(extensionManager, loggerFactory);
    var typeRefResolver = new ExtensionTypeRefResolver(assemblyLoader, loggerFactory);

    var cacheDirectory = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "cache");

    using var exportProvider = await LanguageServerExportProviderBuilder.CreateExportProviderAsync(AppContext.BaseDirectory, extensionManager, assemblyLoader, serverConfiguration.DevKitDependencyPath, cacheDirectory, loggerFactory, cancellationToken);

    var globalOptionService = exportProvider.GetExportedValue<Microsoft.CodeAnalysis.Options.IGlobalOptionService>();
    globalOptionService.SetGlobalOption(WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, serverConfiguration.SourceGeneratorExecutionPreference);
    logger.LogTrace("Source generator execution preference set to {preference}", serverConfiguration.SourceGeneratorExecutionPreference);

    // The log file directory passed to us by VSCode might not exist yet, though its parent directory is guaranteed to exist.
    if (serverConfiguration.ExtensionLogDirectory is not null)
    {
        Directory.CreateDirectory(serverConfiguration.ExtensionLogDirectory);
    }

    // Initialize the server configuration MEF exported value.
    exportProvider.GetExportedValue<ServerConfigurationFactory>().InitializeConfiguration(serverConfiguration);

    // Initialize the fault handler if it's available
    var telemetryReporter = exportProvider.GetExports<ITelemetryReporter>().SingleOrDefault()?.Value;
    RoslynLogger.Initialize(telemetryReporter, serverConfiguration.TelemetryLevel, serverConfiguration.SessionId);

    LanguageServerHost server;
    if (serverConfiguration.UseStdIo)
    {
        server = new LanguageServerHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), exportProvider, loggerFactory, typeRefResolver);
    }
    else
    {
        // The VS Code LSP client passes a full pipe path (e.g. \\.\pipe\<guid> on Windows, /tmp/<id>.sock on Unix).
        // NamedPipeClientStream expects just the pipe name on Windows (it prepends \\.\pipe\ itself),
        // and the full socket path on Unix.
        var pipeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? serverConfiguration.ServerPipeName!.Replace(@"\\.\pipe\", "")
            : serverConfiguration.ServerPipeName!;
        var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync(cancellationToken);
        server = new LanguageServerHost(pipeClient, pipeClient, exportProvider, loggerFactory, typeRefResolver);
    }

    // Eagerly resolve the workspace factory from the per-server LSP services, since right now the language server
    // assumes there's at least one Workspace. This as a side effect creates the actual workspace object which is
    // registered by the LspWorkspaceRegistrationEventListener.
    var workspaceFactory = server.GetLspServices().GetRequiredService<LanguageServerWorkspaceFactory>();

    server.Start();

    logger.LogInformation("Language server initialized");
    RoslynLog.Logger.Log(RoslynLog.FunctionId.VSCode_LanguageServer_Started, logLevel: RoslynLog.LogLevel.Information);

    try
    {
        if (serverConfiguration.ClientProcessId is int clientProcessId && RoslynLanguageServer.TryRegisterClientProcessId(clientProcessId))
            logger.LogInformation("Monitoring client process {clientProcessId} for exit", clientProcessId);

        await server.WaitForExitAsync();
    }
    finally
    {
        // After the LSP server shutdown, report session wide telemetry
        RoslynLogger.ShutdownAndReportSessionTelemetry();
    }
}
