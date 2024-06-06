// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Roslyn.Utilities;

// Setting the title can fail if the process is run without a window, such
// as when launched detached from nodejs
try
{
    Console.Title = "Microsoft.CodeAnalysis.LanguageServer";
}
catch (IOException)
{
}

WindowsErrorReporting.SetErrorModeOnWindows();

var parser = CreateCommandLineParser();
return await parser.Parse(args).InvokeAsync(CancellationToken.None);

static async Task RunAsync(ServerConfiguration serverConfiguration, CancellationToken cancellationToken)
{
    // Before we initialize the LSP server we can't send LSP log messages.
    // Create a console logger as a fallback to use before the LSP server starts.
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(serverConfiguration.MinimumLogLevel);
        builder.AddProvider(new LspLogMessageLoggerProvider(fallbackLoggerFactory:
            // Add a console logger as a fallback for when the LSP server has not finished initializing.
            LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(serverConfiguration.MinimumLogLevel);
                builder.AddConsole();
                // The console logger outputs control characters on unix for colors which don't render correctly in VSCode.
                builder.AddSimpleConsole(formatterOptions => formatterOptions.ColorBehavior = LoggerColorBehavior.Disabled);
            })
        ));
    });

    var logger = loggerFactory.CreateLogger<Program>();

    logger.Log(serverConfiguration.LaunchDebugger ? LogLevel.Critical : LogLevel.Trace, "Server started with process ID {processId}", Environment.ProcessId);
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

    using var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync(extensionManager, assemblyLoader, serverConfiguration.DevKitDependencyPath, loggerFactory);

    // The log file directory passed to us by VSCode might not exist yet, though its parent directory is guaranteed to exist.
    Directory.CreateDirectory(serverConfiguration.ExtensionLogDirectory);

    // Initialize the server configuration MEF exported value.
    exportProvider.GetExportedValue<ServerConfigurationFactory>().InitializeConfiguration(serverConfiguration);

    // Initialize the fault handler if it's available
    var telemetryReporter = exportProvider.GetExports<ITelemetryReporter>().SingleOrDefault()?.Value;
    RoslynLogger.Initialize(telemetryReporter, serverConfiguration.TelemetryLevel, serverConfiguration.SessionId);

    // Create the workspace first, since right now the language server will assume there's at least one Workspace
    var workspaceFactory = exportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();

    var analyzerPaths = new DirectoryInfo(AppContext.BaseDirectory).GetFiles("*.dll")
        .Where(f => f.Name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal) && !f.Name.Contains("LanguageServer", StringComparison.Ordinal))
        .Select(f => f.FullName)
        .ToImmutableArray();

    // Include analyzers from extension assemblies.
    analyzerPaths = analyzerPaths.AddRange(extensionManager.ExtensionAssemblyPaths);

    await workspaceFactory.InitializeSolutionLevelAnalyzersAsync(analyzerPaths, extensionManager);

    var serviceBrokerFactory = exportProvider.GetExportedValue<ServiceBrokerFactory>();
    StarredCompletionAssemblyHelper.InitializeInstance(serverConfiguration.StarredCompletionsPath, extensionManager, loggerFactory, serviceBrokerFactory);
    // TODO: Remove, the path should match exactly. Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1830914.
    Microsoft.CodeAnalysis.EditAndContinue.EditAndContinueMethodDebugInfoReader.IgnoreCaseWhenComparingDocumentNames = Path.DirectorySeparatorChar == '\\';

    var languageServerLogger = loggerFactory.CreateLogger(nameof(LanguageServerHost));

    var (clientPipeName, serverPipeName) = CreateNewPipeNames();
    var pipeServer = new NamedPipeServerStream(serverPipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: 1,
        PipeTransmissionMode.Byte,
        PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);

    // Send the named pipe connection info to the client 
    Console.WriteLine(JsonSerializer.Serialize(new NamedPipeInformation(clientPipeName)));

    // Wait for connection from client
    await pipeServer.WaitForConnectionAsync(cancellationToken);

    var server = new LanguageServerHost(pipeServer, pipeServer, exportProvider, languageServerLogger, typeRefResolver);
    server.Start();

    logger.LogInformation("Language server initialized");

    try
    {
        await server.WaitForExitAsync();
    }
    finally
    {
        // After the LSP server shutdown, report session wide telemetry
        RoslynLogger.ShutdownAndReportSessionTelemetry();

        // Server has exited, cancel our service broker service
        await serviceBrokerFactory.ShutdownAndWaitForCompletionAsync();
    }
}

static CliRootCommand CreateCommandLineParser()
{
    var debugOption = new CliOption<bool>("--debug")
    {
        Description = "Flag indicating if the debugger should be launched on startup.",
        Required = false,
        DefaultValueFactory = _ => false,
    };
    var brokeredServicePipeNameOption = new CliOption<string?>("--brokeredServicePipeName")
    {
        Description = "The name of the pipe used to connect to a remote process (if one exists).",
        Required = false,
    };

    var logLevelOption = new CliOption<LogLevel>("--logLevel")
    {
        Description = "The minimum log verbosity.",
        Required = true,
    };
    var starredCompletionsPathOption = new CliOption<string?>("--starredCompletionComponentPath")
    {
        Description = "The location of the starred completion component (if one exists).",
        Required = false,
    };

    var telemetryLevelOption = new CliOption<string?>("--telemetryLevel")
    {
        Description = "Telemetry level, Defaults to 'off'. Example values: 'all', 'crash', 'error', or 'off'.",
        Required = false,
    };
    var extensionLogDirectoryOption = new CliOption<string>("--extensionLogDirectory")
    {
        Description = "The directory where we should write log files to",
        Required = true,
    };

    var sessionIdOption = new CliOption<string?>("--sessionId")
    {
        Description = "Session Id to use for telemetry",
        Required = false
    };

    var extensionAssemblyPathsOption = new CliOption<string[]?>("--extension")
    {
        Description = "Full paths of extension assemblies to load (optional).",
        Required = false
    };

    var devKitDependencyPathOption = new CliOption<string?>("--devKitDependencyPath")
    {
        Description = "Full path to the Roslyn dependency used with DevKit (optional).",
        Required = false
    };

    var razorSourceGeneratorOption = new CliOption<string?>("--razorSourceGenerator")
    {
        Description = "Full path to the Razor source generator (optional).",
        Required = false
    };

    var razorDesignTimePathOption = new CliOption<string?>("--razorDesignTimePath")
    {
        Description = "Full path to the Razor design time target path (optional).",
        Required = false
    };

    var rootCommand = new CliRootCommand()
    {
        debugOption,
        brokeredServicePipeNameOption,
        logLevelOption,
        starredCompletionsPathOption,
        telemetryLevelOption,
        sessionIdOption,
        extensionAssemblyPathsOption,
        devKitDependencyPathOption,
        razorSourceGeneratorOption,
        razorDesignTimePathOption,
        extensionLogDirectoryOption
    };
    rootCommand.SetAction((parseResult, cancellationToken) =>
    {
        var launchDebugger = parseResult.GetValue(debugOption);
        var logLevel = parseResult.GetValue(logLevelOption);
        var starredCompletionsPath = parseResult.GetValue(starredCompletionsPathOption);
        var telemetryLevel = parseResult.GetValue(telemetryLevelOption);
        var sessionId = parseResult.GetValue(sessionIdOption);
        var extensionAssemblyPaths = parseResult.GetValue(extensionAssemblyPathsOption) ?? [];
        var devKitDependencyPath = parseResult.GetValue(devKitDependencyPathOption);
        var razorSourceGenerator = parseResult.GetValue(razorSourceGeneratorOption);
        var razorDesignTimePath = parseResult.GetValue(razorDesignTimePathOption);
        var extensionLogDirectory = parseResult.GetValue(extensionLogDirectoryOption)!;

        var serverConfiguration = new ServerConfiguration(
            LaunchDebugger: launchDebugger,
            MinimumLogLevel: logLevel,
            StarredCompletionsPath: starredCompletionsPath,
            TelemetryLevel: telemetryLevel,
            SessionId: sessionId,
            ExtensionAssemblyPaths: extensionAssemblyPaths,
            DevKitDependencyPath: devKitDependencyPath,
            RazorSourceGenerator: razorSourceGenerator,
            RazorDesignTimePath: razorDesignTimePath,
            ExtensionLogDirectory: extensionLogDirectory);

        return RunAsync(serverConfiguration, cancellationToken);
    });
    return rootCommand;
}

static (string clientPipe, string serverPipe) CreateNewPipeNames()
{
    // On windows, .NET and Nodejs use different formats for the pipe name
    const string WINDOWS_NODJS_PREFIX = @"\\.\pipe\";
    const string WINDOWS_DOTNET_PREFIX = @"\\.\";

    // The pipe name constructed by some systems is very long (due to temp path).
    // Shorten the unique id for the pipe. 
    var newGuid = Guid.NewGuid().ToString();
    var pipeName = newGuid.Split('-')[0];

    return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? (WINDOWS_NODJS_PREFIX + pipeName, WINDOWS_DOTNET_PREFIX + pipeName)
        : (GetUnixTypePipeName(pipeName), GetUnixTypePipeName(pipeName));
}

static string GetUnixTypePipeName(string pipeName)
{
    // Unix-type pipes are actually writing to a file
    return Path.Combine(Path.GetTempPath(), pipeName + ".sock");
}
