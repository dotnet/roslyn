// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

Console.Title = "Microsoft.CodeAnalysis.LanguageServer";

// TODO - Decide how and where we're logging.  For now just logging stderr (vscode reads stdout for LSP messages).
//     1.  File logs for feedback
//     2.  Logs to vscode output window.
//     3.  Telemetry
// Also decide how we configure logging (env variables, extension settings, etc.)
// https://github.com/microsoft/vscode-csharp-next/issues/12
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
});
var logger = loggerFactory.CreateLogger<ILogger>();

LaunchDebuggerIfEnabled(args);

var solutionPath = GetSolutionPath(args);

// Register and load the appropriate MSBuild assemblies before we create the MEF composition.
// This is required because we need to include types from MS.CA.Workspaces.MSBuild which has a dependency on MSBuild dlls being loaded.
var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk, WorkingDirectory = Path.GetDirectoryName(solutionPath) });
MSBuildLocator.RegisterInstance(msbuildInstances.First());

var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync();

using (var workspace = CreateWorkspaceAsync(solutionPath, exportProvider, logger))
{
    var jsonRpc = new LanguageServerHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), logger, exportProvider);

    await jsonRpc.StartAsync().ConfigureAwait(false);
}

return;

void LaunchDebuggerIfEnabled(string[] args)
{
    if (args.Contains("--debug") && !Debugger.IsAttached)
    {
        logger.LogInformation("Launching debugger...");
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

static async Task<Workspace> CreateWorkspaceAsync(string solutionPath, ExportProvider exportProvider, ILogger logger)
{
    try
    {
        var msbuildWorkspace = MSBuildWorkspace.Create(MefV1HostServices.Create(exportProvider.AsExportProvider()));
        await msbuildWorkspace.OpenSolutionAsync(solutionPath);
        return msbuildWorkspace;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"Failed to load workspace for {solutionPath}");
        throw;
    }
}

