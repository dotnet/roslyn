// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.RoslynTools.NuGet;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class NuGetPrepareCommand
{
    private static readonly NuGetPrepareCommandDefaultHandler s_nuGetPrepareCommandHandler = new();

    public static Command GetCommand()
    {
        var command = new Command("nuget-prepare", "Prepares packages built from the Roslyn repo for validation.")
        {
            VerbosityOption
        };
        command.Action = s_nuGetPrepareCommandHandler;
        return command;
    }

    private class NuGetPrepareCommandDefaultHandler : AsynchronousCommandLineAction
    {
        public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var logger = parseResult.SetupLogging();

            return NuGetPrepare.PrepareAsync(logger);
        }
    }
}

