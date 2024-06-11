// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#if !DOTNET_BUILD_FROM_SOURCE
using StreamJsonRpc;
#endif

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

internal static class Program
{
#if DOTNET_BUILD_FROM_SOURCE

    internal static void Main()
    {
        throw new NotSupportedException("This cannot currently be launched as a process in source build scenarios.");
    }

#else

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

        var messageHandler = new HeaderDelimitedMessageHandler(sendingStream: Console.OpenStandardOutput(), receivingStream: Console.OpenStandardInput(), new JsonMessageFormatter());

        var jsonRpc = new JsonRpc(messageHandler)
        {
            ExceptionStrategy = ExceptionProcessing.CommonErrorData,
        };

        jsonRpc.AddLocalRpcTarget(new BuildHost(loggerFactory, binaryLogPath));
        jsonRpc.StartListening();

        logger.LogInformation("RPC channel started.");

        await jsonRpc.Completion.ConfigureAwait(false);

        logger.LogInformation("RPC channel closed; process exiting.");
    }

#endif
}
