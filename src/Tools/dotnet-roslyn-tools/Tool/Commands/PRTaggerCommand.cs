// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.Commands;

internal static class PRTaggerCommand
{
    private static readonly PRTaggerCommandDefaultHandler s_prTaggerCommandHandler = new();
    public static readonly Option<int> maxVsBuildCheckNumber = new(new[] { "--vsBuildCheckNumber" }, () => 20, "Maximum number of VS build to check. Tagger would compare each VS build and its parent commit to find the inserted payload.");

    public static Symbol GetCommand()
    {
        var command = new Command("pr-tagger", "Tags PRs inserted in a given VS build.")
        {
            CommonOptions.GitHubTokenOption,
            CommonOptions.DevDivAzDOTokenOption,
            CommonOptions.DncEngAzDOTokenOption,
            maxVsBuildCheckNumber
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

            var vsBuildNumber = context.ParseResult.GetValueForOption(maxVsBuildCheckNumber);
            logger.LogInformation($"Check {vsBuildNumber} VS Builds");

            using var devdivConnection = new AzDOConnection(settings.DevDivAzureDevOpsBaseUri, "DevDiv", settings.DevDivAzureDevOpsToken);
            var buildsAndCommits = await PRTagger.PRTagger.GetVSBuildsAndCommitsAsync(devdivConnection, logger, vsBuildNumber, CancellationToken.None).ConfigureAwait(false);

            var client = new GitHubClient(new ProductHeaderValue("roslyn-tool-pr-tagger"))
            {
                Credentials = new Credentials(settings.GitHubToken)
            };
            return await PRTagger.PRTagger.TagPRs(
                vsBuildsAndCommitSha: buildsAndCommits,
                settings,
                devdivConnection,
                client,
                logger).ConfigureAwait(false);
        }
    }
}
