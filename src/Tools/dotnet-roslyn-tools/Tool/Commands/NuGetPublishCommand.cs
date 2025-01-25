// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.RoslynTools.NuGet;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class NuGetPublishCommand
{
    private static readonly NuGetPublishCommandDefaultHandler s_nuGetPublishCommandHandler = new();

    internal static readonly string[] SupportedRepos = [NuGetPublish.RoslynRepo, NuGetPublish.RoslynSdkRepo];

    internal static readonly Argument<string> RepoNameArgument = new Argument<string>("repo-name", () => NuGetPublish.RoslynRepo, "The name of the repo whose packages are being publishing.").FromAmong(SupportedRepos);

    internal static readonly Option<string> SourceOption = new Option<string>(["--source", "-s"], () => "https://www.nuget.org", "Package source (URL, UNC/folder path or package source name) to use.");
    internal static readonly Option<string> ApiKeyOption = new Option<string>(["--api-key", "-k"], "The API key for the server.") { IsRequired = true };
    internal static readonly Option<bool> UnlistedOption = new Option<bool>(["--unlisted", "-u"], "Whether to publish the packages as unlisted.");
    internal static readonly Option<bool> SkipDuplicateOption = new Option<bool>(["--skip-duplicate"], "Whether to skip packages that have already been published.");

    public static Symbol GetCommand()
    {
        var command = new Command("nuget-publish", "Publishes packages built from a Roslyn repo.")
        {
            RepoNameArgument,
            SourceOption,
            ApiKeyOption,
            UnlistedOption,
            VerbosityOption,
            SkipDuplicateOption
        };
        command.Handler = s_nuGetPublishCommandHandler;
        return command;

    }

    private class NuGetPublishCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();

            var repoName = context.ParseResult.GetValueForArgument(RepoNameArgument)!;
            var source = context.ParseResult.GetValueForOption(SourceOption)!;
            var apiKey = context.ParseResult.GetValueForOption(ApiKeyOption)!;
            var unlisted = context.ParseResult.GetValueForOption(UnlistedOption)!;
            var skipDuplicate = context.ParseResult.GetValueForOption(SkipDuplicateOption);

            return await NuGetPublish.PublishAsync(repoName, source, apiKey, unlisted, skipDuplicate, logger);
        }
    }
}
