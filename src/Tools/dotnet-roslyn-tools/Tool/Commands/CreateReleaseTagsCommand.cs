// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine.Invocation;
using System.CommandLine;
using Microsoft.RoslynTools.NuGet;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class CreateReleaseTagsCommand
{
    private static readonly CreateReleaseTagsCommandDefaultHandler s_defaultHandler = new();

    public static Symbol GetCommand()
    {
        var command = new Command("create-release-tags", "Generates git tags for VS releases in the Roslyn repo.")
        {
            VerbosityOption
        };
        command.Handler = s_defaultHandler;
        return command;
    }

    private class CreateReleaseTagsCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();

            return await CreateReleaseTags.CreateReleaseTags.CreateReleaseTagsAsync(logger);
        }
    }
}

