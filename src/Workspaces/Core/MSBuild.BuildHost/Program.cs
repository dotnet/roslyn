// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using StreamJsonRpc;
using System.CommandLine;
using Microsoft.Extensions.Logging;

#if NETCOREAPP
using System.Runtime.Loader;
#endif

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
                configure.LogToStandardErrorThreshold = LogLevel.Trace;
            }));

        var logger = loggerFactory.CreateLogger(typeof(Program));

        // Create a MEF container so we can create our SolutionServices to use for discovering language services; we'll include our own assembly in since that contains the services used
        // for actually loading MSBuild projects.
        var thisAssembly = typeof(Program).Assembly;

#if NETCOREAPP

        // In the .NET Core case, the dependencies we want to dynamically load are not in our deps.json file, so we won't find them when MefHostServices tries to load them
        // with Assembly.Load. To work around this, we'll use LoadFrom instead by hooking our AssemblyLoadContext Resolving.
        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            try
            {
                return Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(thisAssembly.Location)!, assemblyName.Name! + ".dll"));
            }
            catch (Exception)
            {
                return null;
            }
        };

#endif

        var messageHandler = new HeaderDelimitedMessageHandler(sendingStream: Console.OpenStandardOutput(), receivingStream: Console.OpenStandardInput(), new JsonMessageFormatter());

        var jsonRpc = new JsonRpc(messageHandler)
        {
            ExceptionStrategy = ExceptionProcessing.CommonErrorData,
        };

        jsonRpc.AddLocalRpcTarget(new BuildHost(loggerFactory, jsonRpc, binaryLogPath));
        jsonRpc.StartListening();

        logger.LogInformation("RPC channel started.");

        await jsonRpc.Completion.ConfigureAwait(false);

        logger.LogInformation("RPC channel closed; process exiting.");
    }
}
