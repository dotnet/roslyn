// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.Extensions.Logging;

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

var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync();
var jsonRpc = new LanguageServerHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), logger, exportProvider);

await jsonRpc.StartAsync().ConfigureAwait(false);

return;

void LaunchDebuggerIfEnabled(string[] args)
{
    if (args.Contains("--debug") && !Debugger.IsAttached)
    {
        logger.LogInformation("Launching debugger...");
        _ = Debugger.Launch();
    }
}

