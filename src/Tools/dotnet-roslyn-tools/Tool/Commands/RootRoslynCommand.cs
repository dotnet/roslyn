// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;

namespace Microsoft.RoslynTools.Commands;

internal static class RootRoslynCommand
{
    public static RootCommand GetRootCommand()
    {
        var command = new RootCommand()
        {
            AuthenticateCommand.GetCommand(),
            PRFinderCommand.GetCommand(),
            PRTaggerCommand.GetCommand(),
            NuGetDependenciesCommand.GetCommand(),
            NuGetPrepareCommand.GetCommand(),
            NuGetPublishCommand.GetCommand(),
            CreateReleaseTagsCommand.GetCommand(),
            VSBranchInfoCommand.GetCommand(),
            DartTestCommand.GetCommand(),
        };
        command.Name = "roslyn-tools";
        command.Description = "The command line tool for performing infrastructure tasks.";
        return command;
    }
}
