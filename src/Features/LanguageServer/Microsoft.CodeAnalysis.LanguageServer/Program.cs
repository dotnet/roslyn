// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

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

var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync().ConfigureAwait(false);
var hostServices = MefV1HostServices.Create(exportProvider.AsExportProvider());

using (var workspace = await LanguageServerWorkspace.CreateWorkspaceAsync(solutionPath, exportProvider, hostServices, loggerFactory).ConfigureAwait(false))
{
    var server = new LanguageServerHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), exportProvider, hostServices, loggerFactory.CreateLogger(nameof(LanguageServerHost)));

    await server.StartAsync().ConfigureAwait(false);
}

loggerFactory.Dispose();

return;

static LogLevel GetLogLevel(string[] args)
{
    var logLevelIndex = Array.IndexOf(args, "--logLevel") + 1;
    if (logLevelIndex > 0)
    {
        // Map VSCode log level to the LogLevel we can use with ILogger APIs.
        var level = args[logLevelIndex];
        return level switch
        {
            "off" => LogLevel.None,
            "minimal" => LogLevel.Information,
            "messages" => LogLevel.Debug,
            "verbose" => LogLevel.Trace,
            _ => throw new InvalidOperationException($"Unexpected logLevel argument {level}"),
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
    var solutionPathIndex = Array.IndexOf(args, "--solutionPath") + 1;
    if (solutionPathIndex == 0 || solutionPathIndex >= args.Length)
    {
        throw new InvalidOperationException($"Missing valid --solutionPath argument, got {string.Join(",", args)}");
    }

    return args[solutionPathIndex];
}

