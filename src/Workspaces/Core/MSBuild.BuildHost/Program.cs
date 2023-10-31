// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild.Rpc;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var binaryLogOption = new CliOption<string?>("--binlog") { Required = false };
        var command = new CliRootCommand { binaryLogOption };
        var parsedArguments = command.Parse(args);
        var binaryLogPath = parsedArguments.GetValue(binaryLogOption);

        // Create a console logger that logs everything to standard error instead of standard out; by setting the threshold to Trace
        // everything will go to standard error.
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole(configure =>
            {
                // DisableColors is deprecated in favor of us moving to simple console, but that loses the LogToStandardErrorThreshold
                // which we also need
#pragma warning disable CS0618
                configure.DisableColors = true;
#pragma warning restore CS0618
                configure.LogToStandardErrorThreshold = LogLevel.Trace;
            }));

        var logger = loggerFactory.CreateLogger(typeof(Program));

        logger.LogInformation($"BuildHost Runtime Version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

        var server = new RpcServer(sendingStream: Console.OpenStandardOutput(), receivingStream: Console.OpenStandardInput());

        var targetObject = server.AddTarget(new BuildHost(loggerFactory, binaryLogPath, server));
        Contract.ThrowIfFalse(targetObject == 0, "The first object registered should have target 0, which is assumed by the client.");

        await server.RunAsync().ConfigureAwait(false);

        logger.LogInformation("RPC channel closed; process exiting.");
    }
}
