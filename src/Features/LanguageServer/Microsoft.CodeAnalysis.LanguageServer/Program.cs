// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.HelloWorld;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.Extensions.Logging;

Console.Title = "Microsoft.CodeAnalysis.LanguageServer";
var parser = CreateCommandLineParser();
return await parser.InvokeAsync(args);

static async Task RunAsync(bool launchDebugger, string solutionPath, string? brokeredServicePipeName, LogLevel minimumLogLevel, CancellationToken cancellationToken)
{
    if (launchDebugger)
    {
        _ = Debugger.Launch();
    }

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
            })
        ));
    });

    // Register and load the appropriate MSBuild assemblies before we create the MEF composition.
    // This is required because we need to include types from MS.CA.Workspaces.MSBuild which has a dependency on MSBuild dlls being loaded.
    var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk, WorkingDirectory = Path.GetDirectoryName(solutionPath) });
    MSBuildLocator.RegisterInstance(msbuildInstances.First());

    using var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync();

    // Immediately set the logger factory, so that way it'll be available for the rest of the composition
    exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

    // Cancellation token source that we can use to cancel on either LSP server shutdown (managed by client) or interrupt.
    using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cancellationToken = cancellationTokenSource.Token;

    var bridgeCompletionTask = Task.CompletedTask;
    if (brokeredServicePipeName != null)
    {
        var container = await BrokeredServiceContainer.CreateAsync(exportProvider, cancellationToken);

        var bridgeProvider = exportProvider.GetExportedValue<BrokeredServiceBridgeProvider>();

        bridgeCompletionTask = bridgeProvider.SetupBrokeredServicesBridgeAsync(brokeredServicePipeName, container, cancellationToken);
    }

    // Create the project system first, since right now the language server will assume there's at least one Workspace
    var projectSystem = exportProvider.GetExportedValue<LanguageServerProjectSystem>();

    var analyzerPaths = new DirectoryInfo(AppContext.BaseDirectory).GetFiles("*.dll")
        .Where(f => f.Name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal) && !f.Name.Contains("LanguageServer", StringComparison.Ordinal))
        .Select(f => f.FullName)
        .ToImmutableArray();

    await projectSystem.InitializeSolutionLevelAnalyzersAsync(analyzerPaths);

    var server = new LanguageServerHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), exportProvider, loggerFactory.CreateLogger(nameof(LanguageServerHost)));
    server.Start();

    projectSystem.OpenSolution(solutionPath);

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
    var solutionPathOption = new Option<string>("--solutionPath")
    {
        Description = "The solution path to initialize the server with.",
        IsRequired = true,
    };
    var brokeredServicePipeNameOption = new Option<string?>("--brokeredServicePipeName")
    {
        Description = "The name of the pipe used to connect to a remote process (if one exists).",
        IsRequired = false,
    };
    var logLevelOption = new Option<LogLevel>("--logLevel", description: "The minimum log verbosity.", parseArgument: result => result.Tokens.Single().Value switch
    {
        "off" => LogLevel.None,
        "minimal" => LogLevel.Information,
        "messages" => LogLevel.Debug,
        "verbose" => LogLevel.Trace,
        _ => throw new InvalidOperationException($"Unexpected logLevel argument {result}"),
    })
    {
        IsRequired = true,
    };

    var rootCommand = new RootCommand()
    {
        debugOption,
        solutionPathOption,
        brokeredServicePipeNameOption,
        logLevelOption,
    };
    rootCommand.SetHandler(context =>
    {
        var cancellationToken = context.GetCancellationToken();
        var launchDebugger = context.ParseResult.GetValueForOption(debugOption);
        var solutionPath = context.ParseResult.GetValueForOption(solutionPathOption)!;
        var brokeredServicePipeName = context.ParseResult.GetValueForOption(brokeredServicePipeNameOption);
        var logLevel = context.ParseResult.GetValueForOption(logLevelOption);

        return RunAsync(launchDebugger, solutionPath, brokeredServicePipeName, logLevel, cancellationToken);
    });

    return new CommandLineBuilder(rootCommand).UseDefaults().Build();
}

