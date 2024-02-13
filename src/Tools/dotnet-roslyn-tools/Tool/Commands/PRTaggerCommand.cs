// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.Commands;

internal static class PRTaggerCommand
{
    private static readonly PRTaggerCommandDefaultHandler s_prTaggerCommandHandler = new();
    private static readonly Option<int> maxVsBuildCheckNumber = new(new[] { "--vsBuildCheckNumber" }, () => 50, "Maximum number of VS build to check. Tagger would compare each VS build and its parent commit to find the inserted payload.");

    public static Symbol GetCommand()
    {
        var command = new Command("pr-tagger", "Tags PRs inserted in a given VS build.")
        {
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

            // Setup devdiv connection, dnceng connect, and GitHub Client.
            using var devdivConnection = new AzDOConnection(settings.DevDivAzureDevOpsBaseUri, "DevDiv", settings.DevDivAzureDevOpsToken);
            using var dncengConnection = new AzDOConnection(settings.DncEngAzureDevOpsBaseUri, "internal", settings.DncEngAzureDevOpsToken);
            var client = new HttpClient
            {
                BaseAddress = new("https://api.github.com/")
            };

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", "roslyn-tool-tagger");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                settings.GitHubToken);

            return await PRTagger.PRTagger.TagPRs(
                settings,
                devdivConnection,
                dncengConnection,
                client,
                logger,
                maxFetchingVSBuildNumber).ConfigureAwait(false);
        }
    }
}
