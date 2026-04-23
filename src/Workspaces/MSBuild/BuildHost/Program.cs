// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        // Note: we should limit the data passed through via command line strings, and pass information through IBuildHost.ConfigureGlobalState whenever possible.
        // This is because otherwise we might run into escaping issues, or command line length limits.

        var logger = new BuildHostLogger(Console.Error);

        if (args is not [var pipeName, var cultureName])
        {
            logger.LogCritical($"Expected arguments: <pipe name> <culture>");
            return -1;
        }

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            // We couldn't find the culture, log a warning and fallback to the OS configured value.
            logger.LogWarning($"Culture '{cultureName}' was not found, falling back to OS culture");
        }

        logger.LogInformation($"BuildHost Runtime Version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

        var pipeServer = NamedPipeUtil.CreateServer(pipeName, PipeDirection.InOut);
        await pipeServer.WaitForConnectionAsync().ConfigureAwait(false);

        var server = new RpcServer(pipeServer);

        var targetObject = server.AddTarget(new BuildHost(logger, server));
        Contract.ThrowIfFalse(targetObject == 0, "The first object registered should have target 0, which is assumed by the client.");

        await server.RunAsync().ConfigureAwait(false);

        logger.LogInformation("RPC channel closed; process exiting.");

        return 0;
    }
}
