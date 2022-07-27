// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Commands;

using System.CommandLine.Invocation;
using System.CommandLine;
using Microsoft.Extensions.Logging;

internal static class PRTaggerCommand
{
    private static readonly PRTaggerCommandDefaultHandler s_prTaggerCommandHandler = new();

    private static readonly Option<string> ProductName = new Option<string>(new[] { "--product-name", "-p" }, "Name of product (e.g. 'roslyn' or 'razor')") { IsRequired = true }
        .FromAmong("roslyn", "razor");
    private static readonly Option<string> VSBuild = new(new[] { "--build", "-b" }, "VS build number") { IsRequired = true };
    private static readonly Option<string> CommitId = new(new[] { "--commit", "-c" }, "VS build commit") { IsRequired = true };

    public static Symbol GetCommand()
    {
        var command = new Command("pr-tagger", "Tags PRs inserted in a given VS build.")
        {
            ProductName,
            VSBuild,
            CommitId,
            CommonOptions.GitHubTokenOption,
            CommonOptions.DevDivAzDOTokenOption,
            CommonOptions.DncEngAzDOTokenOption
        };

        command.Handler = s_prTaggerCommandHandler;
        return command;
    }

    private class PRTaggerCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();
            var productName = context.ParseResult.GetValueForOption(ProductName)!;
            var vsBuild = context.ParseResult.GetValueForOption(VSBuild)!;
            var vsCommitSha = context.ParseResult.GetValueForOption(CommitId)!;
            var settings = context.ParseResult.LoadSettings(logger);

            if (string.IsNullOrEmpty(settings.GitHubToken) ||
                string.IsNullOrEmpty(settings.DevDivAzureDevOpsToken) ||
                string.IsNullOrEmpty(settings.DncEngAzureDevOpsToken))
            {
                logger.LogError("Missing authentication token.");
                return -1;
            }

            return await PRTagger.PRTagger.TagPRs(
                productName, vsBuild, vsCommitSha, settings, logger, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
