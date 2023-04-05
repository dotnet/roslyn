// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.HelloWorld;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.VisualStudio.Composition;

Console.Title = "Microsoft.CodeAnalysis.LanguageServer";
var parser = CreateCommandLineParser();
return await parser.InvokeAsync(args);

static async Task RunAsync(bool launchDebugger, string? brokeredServicePipeName, LogLevel minimumLogLevel, string? starredCompletionPath, string? projectRazorJsonFileName, CancellationToken cancellationToken)
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

    using var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync();

    // Immediately set the logger factory, so that way it'll be available for the rest of the composition
    exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

    // Allow the extension to override the razor file name to generate, in case they need to break the format
    if (projectRazorJsonFileName is not null)
    {
        RazorDynamicFileInfoProvider.SetProjectRazorJsonFileName(projectRazorJsonFileName);
    }

    // Initialize the fault handler if it's available
    try
    {
        exportProvider.GetExportedValue<ILspFaultLogger?>()?.Initialize();
    }
    catch (CompositionFailedException) { }

    // Cancellation token source that we can use to cancel on either LSP server shutdown (managed by client) or interrupt.
    using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cancellationToken = cancellationTokenSource.Token;

    var bridgeCompletionTask = Task.CompletedTask;
    if (brokeredServicePipeName != null)
    {
        var container = await BrokeredServiceContainer.CreateAsync(exportProvider, cancellationToken);

        var bridgeProvider = exportProvider.GetExportedValue<BrokeredServiceBridgeProvider>();

        bridgeCompletionTask = bridgeProvider.SetupBrokeredServicesBridgeAsync(brokeredServicePipeName, container, cancellationToken);

        // starred completions can only be initialized if brokered service pipe and relevant path are both present
        var serviceBroker = container.GetFullAccessServiceBroker();
        StarredCompletionAssemblyHelper.InitializeInstance(starredCompletionPath, loggerFactory, serviceBroker);
    }

    // Create the workspace first, since right now the language server will assume there's at least one Workspace
    var workspaceFactory = exportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();

    var analyzerPaths = new DirectoryInfo(AppContext.BaseDirectory).GetFiles("*.dll")
        .Where(f => f.Name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal) && !f.Name.Contains("LanguageServer", StringComparison.Ordinal))
        .Select(f => f.FullName)
        .ToImmutableArray();

    await workspaceFactory.InitializeSolutionLevelAnalyzersAsync(analyzerPaths);

    var server = new LanguageServerHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), exportProvider, loggerFactory.CreateLogger(nameof(LanguageServerHost)));
    server.Start();

    if (brokeredServicePipeName != null)
    {
        await exportProvider.GetExportedValue<RemoteHelloWorldProvider>().SayHelloToRemoteServerAsync(cancellationToken);
    }

    await server.WaitForExitAsync();

    // Server has exited, cancel our token.
    cancellationTokenSource.Cancel();

    await bridgeCompletionTask;

    return;
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

    var projectRazorJsonFileNameOption = new Option<string?>("--projectRazorJsonFileName")
    {
        Description = "The file name to use for the project.razor.json file (for Razor projects).",
        IsRequired = false,
    };

    var rootCommand = new RootCommand()
    {
        debugOption,
        brokeredServicePipeNameOption,
        logLevelOption,
        starredCompletionsPathOption,
        projectRazorJsonFileNameOption,
    };
    rootCommand.SetHandler(context =>
    {
        var cancellationToken = context.GetCancellationToken();
        var launchDebugger = context.ParseResult.GetValueForOption(debugOption);
        var brokeredServicePipeName = context.ParseResult.GetValueForOption(brokeredServicePipeNameOption);
        var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
        var starredCompletionsPath = context.ParseResult.GetValueForOption(starredCompletionsPathOption);
        var projectRazorJsonFileName = context.ParseResult.GetValueForOption(projectRazorJsonFileNameOption);

        return RunAsync(launchDebugger, brokeredServicePipeName, logLevel, starredCompletionsPath, projectRazorJsonFileName, cancellationToken);
    });

    return new CommandLineBuilder(rootCommand).UseDefaults().Build();
}

