// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.PRTagger;

namespace Microsoft.RoslynTools.Commands;

internal static class PRTaggerCommand
{
    private static readonly PRTaggerCommandDefaultHandler s_prTaggerCommandHandler = new();
    private static readonly Option<int> maxVsBuildCheckNumber = new(new[] { "--vsBuildCheckNumber" }, () => 50, "Maximum number of VS build to check. Tagger would compare each VS build and its parent commit to find the inserted payload.");
    private static readonly Option<string> VSBuild = new(new[] { "--build", "-b" }, "VS build number");

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
            maxVsBuildCheckNumber,
            CommonOptions.GitHubTokenOption,
            CommonOptions.DevDivAzDOTokenOption,
            CommonOptions.DncEngAzDOTokenOption,
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

            if (string.IsNullOrEmpty(settings.GitHubToken) ||
                string.IsNullOrEmpty(settings.DevDivAzureDevOpsToken) ||
                string.IsNullOrEmpty(settings.DncEngAzureDevOpsToken))
            {
                logger.LogError("Missing authentication token.");
                return -1;
            }

            var maxFetchingVSBuildNumber = context.ParseResult.GetValueForOption(maxVsBuildCheckNumber);
            logger.LogInformation($"Check {maxFetchingVSBuildNumber} VS Builds");

            var vsBulid = context.ParseResult.GetValueForOption(VSBuild);
            if (!string.IsNullOrEmpty(vsBulid))
            {
                logger.LogInformation($"Check VS Build: {vsBulid}");
            }

            using var remoteConnections = new RemoteConnections(settings);
            return await PRTagger.PRTagger.TagPRs(
                remoteConnections,
                logger,
                maxFetchingVSBuildNumber,
                vsBulid).ConfigureAwait(false);
        }
    }
}
