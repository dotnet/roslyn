// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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

var minimumLogLevel = GetLogLevel(args);

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

LaunchDebuggerIfEnabled(args);

var solutionPath = GetSolutionPath(args);

// Register and load the appropriate MSBuild assemblies before we create the MEF composition.
// This is required because we need to include types from MS.CA.Workspaces.MSBuild which has a dependency on MSBuild dlls being loaded.
var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk, WorkingDirectory = Path.GetDirectoryName(solutionPath) });
MSBuildLocator.RegisterInstance(msbuildInstances.First());

using var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync();

// Immediately set the logger factory, so that way it'll be available for the rest of the composition
exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

var brokeredServicePipeName = GetBrokeredServicePipeName(args);
var bridgeCompletionTask = Task.CompletedTask;
if (brokeredServicePipeName != null)
{
    var container = await BrokeredServiceContainer.CreateAsync(exportProvider, CancellationToken.None);

    var bridgeProvider = exportProvider.GetExportedValue<BrokeredServiceBridgeProvider>();

    bridgeCompletionTask = bridgeProvider.SetupBrokeredServicesBridgeAsync(brokeredServicePipeName, container, CancellationToken.None);
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
    await exportProvider.GetExportedValue<RemoteHelloWorldProvider>().SayHelloToRemoteServerAsync(CancellationToken.None);
}

await server.WaitForExitAsync();

await bridgeCompletionTask;

return;

static LogLevel GetLogLevel(string[] args)
{
    var logLevel = GetArgumentValue(args, "--logLevel");
    if (logLevel != null)
    {
        return logLevel switch
        {
            "off" => LogLevel.None,
            "minimal" => LogLevel.Information,
            "messages" => LogLevel.Debug,
            "verbose" => LogLevel.Trace,
            _ => throw new InvalidOperationException($"Unexpected logLevel argument {logLevel}"),
        };
    }

    return LogLevel.Information;
}

static void LaunchDebuggerIfEnabled(string[] args)
{
    if (args.Contains("--debug") && !Debugger.IsAttached)
    {
        _ = Debugger.Launch();
    }
}

static string GetSolutionPath(string[] args)
{
    return GetArgumentValue(args, "--solutionPath") ?? throw new InvalidOperationException($"Missing valid --solutionPath argument, got {string.Join(",", args)}");
}

static string? GetBrokeredServicePipeName(string[] args)
{
    return GetArgumentValue(args, "--brokeredServicePipeName");
}

static string? GetArgumentValue(string[] args, string argumentName)
{
    var argumentValueIndex = Array.IndexOf(args, argumentName) + 1;
    if (argumentValueIndex > 0 && argumentValueIndex < args.Length)
    {
        return args[argumentValueIndex];
    }

    return null;
}

