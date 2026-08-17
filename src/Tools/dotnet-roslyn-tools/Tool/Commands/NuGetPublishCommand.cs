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

    internal static readonly Argument<string> RepoNameArgument = new("repo-name")
    {
        Description = "The name of the repo whose packages are being publishing.",
        DefaultValueFactory = _ => NuGetPublish.RoslynRepo,
    };
    internal static readonly Option<string> SourceOption = new("--source", "-s")
    {
        Description = "Package source (URL, UNC/folder path or package source name) to use.",
        DefaultValueFactory = _ => "https://www.nuget.org",
    };
    internal static readonly Option<string> ApiKeyOption = new("--api-key", "-k")
    {
        Description = "The API key for the server.",
        Required = true,
    };
    internal static readonly Option<bool> UnlistedOption = new("--unlisted", "-u")
    {
        Description = "Whether to publish the packages as unlisted.",
    };
    internal static readonly Option<bool> SkipDuplicateOption = new("--skip-duplicate")
    {
        Description = "Whether to skip packages that have already been published.",
    };

    static NuGetPublishCommand()
    {
        RepoNameArgument.AcceptOnlyFromAmong(SupportedRepos);
    }

    public static Command GetCommand()
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
        command.Action = s_nuGetPublishCommandHandler;
        return command;
    }

    private class NuGetPublishCommandDefaultHandler : AsynchronousCommandLineAction
    {
        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var logger = parseResult.SetupLogging();

            var repoName = parseResult.GetValue(RepoNameArgument)!;
            var source = parseResult.GetValue(SourceOption)!;
            var apiKey = parseResult.GetValue(ApiKeyOption)!;
            var unlisted = parseResult.GetValue(UnlistedOption)!;
            var skipDuplicate = parseResult.GetValue(SkipDuplicateOption);

            return await NuGetPublish.PublishAsync(repoName, source, apiKey, unlisted, skipDuplicate, logger);
        }
    }
}
