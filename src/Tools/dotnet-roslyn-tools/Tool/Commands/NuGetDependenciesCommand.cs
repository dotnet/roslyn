// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine.Invocation;
using System.CommandLine;
using Microsoft.RoslynTools.NuGet;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class NuGetDependenciesCommand
{
    private static readonly NuGetDependenciesCommandDefaultHandler s_nuGetDependenciesCommandHandler = new();

    public static Symbol GetCommand()
    {
        var command = new Command("nuget-dependencies", "Lists dependencies that are missing or out of date for a folder of .nupkg files.")
        {
            VerbosityOption
        };
        command.Handler = s_nuGetDependenciesCommandHandler;
        return command;
    }

    private class NuGetDependenciesCommandDefaultHandler : ICommandHandler
    {
        public Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();
            var packageFolder = Environment.CurrentDirectory;

            return NuGetDependencyFinder.FindDependenciesAsync(packageFolder, logger);
        }
    }
}

