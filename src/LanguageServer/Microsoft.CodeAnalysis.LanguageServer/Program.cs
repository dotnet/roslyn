// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CommandLine;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
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

static async Task<int> RunAsync(ServerConfiguration serverConfiguration, CancellationToken cancellationToken)
{
    if (serverConfiguration.IsDaemon)
    {
        Contract.ThrowIfTrue(serverConfiguration.UseStdIo, "Server cannot be started with --daemon together with --stdio.");
        Contract.ThrowIfNull(serverConfiguration.ServerPipeName, "Server started with --daemon must also specify --pipe.");
        Contract.ThrowIfTrue(serverConfiguration.ClientProcessId is not null, "Server cannot be started with --daemon together with --clientProcessId.");
    }
    else if (serverConfiguration.UseStdIo)
    {
        Contract.ThrowIfFalse(serverConfiguration.ServerPipeName is null, "Server cannot be started with --stdio together with --pipe.");
    }
    else
    {
        Contract.ThrowIfNull(serverConfiguration.ServerPipeName, "Server must be started with either --stdio or --pipe option.");
    }

    if (serverConfiguration.UseStdIo)
    {
        // Redirect Console.Out to try prevent the standard output stream from being corrupted.
        // This should be done before the logger is created as it can write to the standard output.
        Console.SetOut(new StreamWriter(Console.OpenStandardError()));
    }

    var connectionManager = new LanguageServerConnectionManager();

    // Create a console logger as a fallback to use before the LSP server starts.
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        // The actual logger is responsible for deciding whether to log based on the current log level.
        // The factory should be configured to log everything.
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddProvider(new GlobalLogMessageLoggerProvider(fallbackLoggerFactory:
            // Add a console logger as a fallback for when an LSP server is not available.
            LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddConsole();
                // The console logger outputs control characters on unix for colors which don't render correctly in VSCode.
                builder.AddSimpleConsole(formatterOptions => formatterOptions.ColorBehavior = LoggerColorBehavior.Disabled);
            }), connectionManager, new(serverConfiguration.InitialLogLevel)
        ));
    });

    var logger = loggerFactory.CreateLogger<Program>();

    logger.LogInformation("Server information:");
    logger.LogInformation("  Assembly informational version: {assemblyInformationalVersion}", typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<unknown>");
    logger.LogInformation("  Executable path: {processPath}", Environment.ProcessPath ?? "<unknown>");
    logger.LogInformation("  Process ID: {processId}", Environment.ProcessId);

    if (serverConfiguration.IsDaemon)
    {
        // We are the shared daemon. A short-lived bootstrap process (in the thin client) started us and then exited,
        // orphaning us out of the editor's process tree so a teardown of that tree can't take us down. On Unix,
        // additionally move into a new session so signals aimed at the launching client's session/process group (e.g.
        // terminal-close SIGHUP) don't reach the shared daemon. A no-op on Windows, where leaving the editor's
        // job/tree is handled entirely by the bootstrap orphaning us.
        DaemonProcessDetach.DetachIntoNewSessionIfUnix(logger);
    }

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

    using var exportProvider = await LanguageServerExportProviderBuilder.CreateExportProviderAsync(AppContext.BaseDirectory, extensionManager, assemblyLoader, serverConfiguration, cacheDirectory, loggerFactory, cancellationToken);

    var globalOptionService = exportProvider.GetExportedValue<Microsoft.CodeAnalysis.Options.IGlobalOptionService>();
    globalOptionService.SetGlobalOption(WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, serverConfiguration.SourceGeneratorExecutionPreference);
    logger.LogTrace("Source generator execution preference set to {preference}", serverConfiguration.SourceGeneratorExecutionPreference);

    // The log file directory passed to us by VSCode might not exist yet, though its parent directory is guaranteed to exist.
    if (serverConfiguration.ExtensionLogDirectory is not null)
    {
        Directory.CreateDirectory(serverConfiguration.ExtensionLogDirectory);
    }

    // Initialize the fault handler if it's available
    var telemetryReporter = exportProvider.GetExports<ITelemetryReporter>().SingleOrDefault()?.Value;
    RoslynLogger.Initialize(telemetryReporter, serverConfiguration.TelemetryLevel, serverConfiguration.SessionId);

    // Build the connection source for the configured mode. Single-server mode (stdio / connect-out pipe) yields
    // exactly one connection; daemon mode accepts many and manages its own idle timeout. Both run through the same
    // connection manager loop.
    ILanguageServerConnectionSource connectionSource;

    if (serverConfiguration.IsDaemon)
    {
        if (!NamedPipeDaemonConnectionSource.TryCreate(
                serverConfiguration.ServerPipeName!, serverConfiguration.DaemonKeepAlive, logger, out var daemonSource))
        {
            // Another daemon already owns this pipe. With the thin client holding its startup mutex through
            // the connect, this generally only happens when a '--daemon' process is started outside that
            // protocol (e.g. manually, or a stale instance). It's recoverable - the client connects to the
            // existing daemon - so we exit with a distinct non-zero code rather than throwing, which would
            // surface a stack trace in the editor's output for a benign condition.
            return ServerExitCodes.DaemonAlreadyRunning;
        }

        connectionSource = daemonSource;
    }
    else if (serverConfiguration.UseStdIo)
    {
        connectionSource = new SingleLanguageServerConnectionSource(
            new LanguageServerConnection(Console.OpenStandardInput(), Console.OpenStandardOutput()));
    }
    else
    {
        // The VS Code LSP client passes a full pipe path (e.g. \\.\pipe\<guid> on Windows, /tmp/<id>.sock on Unix).
        // NamedPipeClientStream expects just the pipe name on Windows (it prepends \\.\pipe\ itself),
        // and the full socket path on Unix.
        var pipeName = serverConfiguration.ServerPipeName!;
        const string windowsPipePrefix = @"\\.\pipe\";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            pipeName.StartsWith(windowsPipePrefix, StringComparison.OrdinalIgnoreCase))
        {
            pipeName = pipeName[windowsPipePrefix.Length..];
        }

        var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync(cancellationToken);
        connectionSource = new SingleLanguageServerConnectionSource(new LanguageServerConnection(pipeClient, pipeClient, pipeClient));
    }

    // Monitor the client process in single-server mode only; a shared daemon must not exit when one client
    // dies (and the thin client doesn't forward --clientProcessId to the daemon).
    if (!serverConfiguration.IsDaemon &&
        serverConfiguration.ClientProcessId is int clientProcessId &&
        RoslynLanguageServer.TryRegisterClientProcessId(clientProcessId))
    {
        logger.LogInformation("Monitoring client process {clientProcessId} for exit", clientProcessId);
    }

    logger.LogInformation("Language server initialized");
    RoslynLog.Logger.Log(RoslynLog.FunctionId.VSCode_LanguageServer_Started, logLevel: RoslynLog.LogLevel.Information);

    try
    {
        using (connectionSource as IDisposable)
        {
            await connectionManager.RunAsync(connectionSource, exportProvider, typeRefResolver, logger, cancellationToken);
        }
    }
    finally
    {
        // After the LSP server shutdown, report session wide telemetry
        RoslynLogger.ShutdownAndReportSessionTelemetry();
    }

    return ServerExitCodes.Success;
}
