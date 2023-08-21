// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.Host.Mef;
using StreamJsonRpc;

#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

internal static class Program
{
    internal static async Task Main()
    {
        // We'll locate MSBuild right away before any other methods try to load any MSBuild types and fail in the process.
        // TODO: we should pick the appropriate instance via a command line switch.
        MSBuildLocator.RegisterInstance(MSBuildLocator.QueryVisualStudioInstances().First());

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

        var hostServices = MefHostServices.Create(MefHostServices.DefaultAssemblies.Append(thisAssembly));
        var solutionServices = new AdhocWorkspace(hostServices).Services.SolutionServices;

        var messageHandler = new HeaderDelimitedMessageHandler(sendingStream: Console.OpenStandardOutput(), receivingStream: Console.OpenStandardInput(), new JsonMessageFormatter());

        var jsonRpc = new JsonRpc(messageHandler)
        {
            ExceptionStrategy = ExceptionProcessing.CommonErrorData,
        };

        jsonRpc.AddLocalRpcTarget(new BuildHost(jsonRpc, solutionServices));
        jsonRpc.StartListening();

        await jsonRpc.Completion.ConfigureAwait(false);
    }
}
