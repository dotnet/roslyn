// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal static class PRTaggerCommand
{
    private static readonly PRTaggerCommandDefaultHandler s_prTaggerCommandHandler = new();
    internal static readonly Option<int> MaxVsBuildCheckNumber = new(["--vsBuildCheckNumber"], () => 50, "Maximum number of VS build to check. Tagger would compare each VS build and its parent commit to find the inserted payload.");
    internal static readonly Option<string> VSBuild = new(["--build", "-b"], "VS build number");

    public static Symbol GetCommand()
    {
        var command = new Command("pr-tagger",
            @"Tags PRs inserted in a given VS build.
It works by checking VS Builds one by one to find the insertion payload.
The checking build list is created:
1. If --build is specified, it will just check this VS build to see if insertion has been made. --vsBuildCheckNumber has no effect in this case.
2. If --build is not specified, it will use the latest VS build as the head. The tail would be the last reported VSBuild in each product repo. Use --vsBuildCheckNumber to control the max number of build to check in each run.")
        {
            VSBuild,
            MaxVsBuildCheckNumber,
            GitHubTokenOption,
            DevDivAzDOTokenOption,
            DncEngAzDOTokenOption,
            IsCIOption,
        };

        command.Handler = s_prTaggerCommandHandler;
        return command;
    }

    private class PRTaggerCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();
            var settings = context.ParseResult.LoadSettings(logger);

            var isMissingAzDOToken = string.IsNullOrEmpty(settings.DevDivAzureDevOpsToken) || string.IsNullOrEmpty(settings.DncEngAzureDevOpsToken);
            if (string.IsNullOrEmpty(settings.GitHubToken) ||
                (settings.IsCI && isMissingAzDOToken))
            {
                logger.LogError("Missing authentication token.");
                return -1;
            }

            var maxFetchingVSBuildNumber = context.ParseResult.GetValueForOption(MaxVsBuildCheckNumber);
            logger.LogInformation("Check {MaxFetchingVSBuildNumber} VS Builds", maxFetchingVSBuildNumber);

            var vsBuild = context.ParseResult.GetValueForOption(VSBuild);
            if (!string.IsNullOrEmpty(vsBuild))
            {
                logger.LogInformation("Check VS Build: {VsBuild}", vsBuild);
            }

            using var remoteConnections = new RemoteConnections(settings);
            return await PRTagger.PRTagger.TagPRs(
                remoteConnections,
                logger,
                maxFetchingVSBuildNumber,
                vsBuild).ConfigureAwait(false);
        }
    }
}
