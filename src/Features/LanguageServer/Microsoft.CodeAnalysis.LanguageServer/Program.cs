// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.HelloWorld;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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
return await parser.InvokeAsync(args);

static async Task RunAsync(
    bool launchDebugger,
    LogLevel minimumLogLevel,
    string? starredCompletionPath,
    string? telemetryLevel,
    string? sessionId,
    string? sharedDependenciesPath,
    IEnumerable<string> extensionAssemblyPaths,
    CancellationToken cancellationToken)
{
    // Before we initialize the LSP server we can't send LSP log messages.
    // Create a console logger as a fallback to use before the LSP server starts.
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(minimumLogLevel);
        builder.AddProvider(new LspLogMessageLoggerProvider(fallbackLoggerFactory:
            // Add a console logger as a fallback for when the LSP server has not finished initializing.
            LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(minimumLogLevel);
                builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
                // The console logger outputs control characters on unix for colors which don't render correctly in VSCode.
                builder.AddSimpleConsole(formatterOptions => formatterOptions.ColorBehavior = LoggerColorBehavior.Disabled);
            })
        ));
    });

    if (launchDebugger)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Debugger.Launch() only works on Windows.
            _ = Debugger.Launch();
        }
        else
        {
            var logger = loggerFactory.CreateLogger<Program>();
            var timeout = TimeSpan.FromMinutes(1);
            logger.LogCritical($"Server started with process ID {Environment.ProcessId}");
            logger.LogCritical($"Waiting {timeout:g} for a debugger to attach");
            using var timeoutSource = new CancellationTokenSource(timeout);
            while (!Debugger.IsAttached && !timeoutSource.Token.IsCancellationRequested)
            {
                await Task.Delay(100, CancellationToken.None);
            }
        }
    }

    using var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync(extensionAssemblyPaths, sharedDependenciesPath, loggerFactory);

    // Initialize the fault handler if it's available
    var telemetryReporter = exportProvider.GetExports<ITelemetryReporter>().SingleOrDefault()?.Value;
    RoslynLogger.Initialize(telemetryReporter, telemetryLevel, sessionId);

    // Create the workspace first, since right now the language server will assume there's at least one Workspace
    var workspaceFactory = exportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();

    var analyzerPaths = new DirectoryInfo(AppContext.BaseDirectory).GetFiles("*.dll")
        .Where(f => f.Name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal) && !f.Name.Contains("LanguageServer", StringComparison.Ordinal))
        .Select(f => f.FullName)
        .ToImmutableArray();

    await workspaceFactory.InitializeSolutionLevelAnalyzersAsync(analyzerPaths);

    var serviceBrokerFactory = exportProvider.GetExportedValue<ServiceBrokerFactory>();
    StarredCompletionAssemblyHelper.InitializeInstance(starredCompletionPath, loggerFactory, serviceBrokerFactory);

    // TODO: Remove, the path should match exactly. Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1830914.
    Microsoft.CodeAnalysis.EditAndContinue.EditAndContinueMethodDebugInfoReader.IgnoreCaseWhenComparingDocumentNames = Path.DirectorySeparatorChar == '\\';

    var server = new LanguageServerHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), exportProvider, loggerFactory.CreateLogger(nameof(LanguageServerHost)));
    server.Start();

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

static Parser CreateCommandLineParser()
{
    var debugOption = new Option<bool>("--debug", getDefaultValue: () => false)
    {
        Description = "Flag indicating if the debugger should be launched on startup.",
        IsRequired = false,
    };
    var brokeredServicePipeNameOption = new Option<string?>("--brokeredServicePipeName")
    {
        Description = "The name of the pipe used to connect to a remote process (if one exists).",
        IsRequired = false,
    };

    var logLevelOption = new Option<LogLevel>("--logLevel", description: "The minimum log verbosity.", parseArgument: result =>
    {
        var value = result.Tokens.Single().Value;
        return !Enum.TryParse<LogLevel>(value, out var logLevel)
            ? throw new InvalidOperationException($"Unexpected logLevel argument {result}")
            : logLevel;
    })
    {
        IsRequired = true,
    };
    var starredCompletionsPathOption = new Option<string?>("--starredCompletionComponentPath")
    {
        Description = "The location of the starred completion component (if one exists).",
        IsRequired = false,
    };

    var telemetryLevelOption = new Option<string?>("--telemetryLevel")
    {
        Description = "Telemetry level, Defaults to 'off'. Example values: 'all', 'crash', 'error', or 'off'.",
        IsRequired = false,
    };

    var sessionIdOption = new Option<string?>("--sessionId")
    {
        Description = "Session Id to use for telemetry",
        IsRequired = false
    };

    var sharedDependenciesOption = new Option<string?>("--sharedDependencies")
    {
        Description = "Full path of the directory containing shared assemblies (optional).",
        IsRequired = false
    };

    var extensionAssemblyPathsOption = new Option<string[]?>(new string[] { "--extension", "--extensions" }) // TODO: remove plural form
    {
        Description = "Full paths of extension assemblies to load (optional).",
        IsRequired = false
    };

    var rootCommand = new RootCommand()
    {
        debugOption,
        brokeredServicePipeNameOption,
        logLevelOption,
        starredCompletionsPathOption,
        telemetryLevelOption,
        sessionIdOption,
        sharedDependenciesOption,
        extensionAssemblyPathsOption,
    };
    rootCommand.SetHandler(context =>
    {
        var cancellationToken = context.GetCancellationToken();
        var launchDebugger = context.ParseResult.GetValueForOption(debugOption);
        var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
        var starredCompletionsPath = context.ParseResult.GetValueForOption(starredCompletionsPathOption);
        var telemetryLevel = context.ParseResult.GetValueForOption(telemetryLevelOption);
        var sessionId = context.ParseResult.GetValueForOption(sessionIdOption);
        var sharedDependenciesPath = context.ParseResult.GetValueForOption(sharedDependenciesOption);
        var extensionAssemblyPaths = context.ParseResult.GetValueForOption(extensionAssemblyPathsOption) ?? Array.Empty<string>();

        return RunAsync(launchDebugger, logLevel, starredCompletionsPath, telemetryLevel, sessionId, sharedDependenciesPath, extensionAssemblyPaths, cancellationToken);
    });

    return new CommandLineBuilder(rootCommand).UseDefaults().Build();
}

