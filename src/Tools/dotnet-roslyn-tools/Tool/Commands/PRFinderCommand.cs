// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal static class PRFinderCommand
{
    private static readonly PrFinderCommandDefaultHandler s_prFinderCommandHandler = new();

    internal static readonly string[] SupportedFormats = [PRFinder.PRFinder.DefaultFormat, PRFinder.PRFinder.OmniSharpFormat, PRFinder.PRFinder.ChangelogFormat];

    internal static readonly Option<string> StartRefOption = new("--start", "-s")
    {
        Description = "The ref to start looking for PRs from. This value can be a branch name, tag name, or commit SHA.",
        Required = true,
    };
    internal static readonly Option<string> EndRefOption = new("--end", "-e")
    {
        Description = "The ref to stop looking for PRs at. This value can be a branch name, tag name, or commit SHA.",
        Required = true,
    };
    internal static readonly Option<string?> PathOption = new("--path")
    {
        Description = "When set only PRs that touch the specified path will be included in the output.",
    };
    internal static readonly Option<string> FormatOption = new("--format")
    {
        Description = "The formatting to apply to the PR list.",
        DefaultValueFactory = _ => PRFinder.PRFinder.DefaultFormat,
    };
    internal static readonly Option<string?> RepoPathOption = new("--repo")
    {
        Description = "The directory of the repository to look for PRs. If none is provided, the current directory is used",
    };
    internal static readonly Option<string[]> LabelsOption = new("--label")
    {
        Description = "Filter PRs to those with one of the specified labels. Can be specified multiple times.",
        DefaultValueFactory = _ => [],
    };

    static PRFinderCommand()
    {
        FormatOption.AcceptOnlyFromAmong(SupportedFormats);
    }

    public static Command GetCommand()
    {
        var prFinderCommand = new Command("pr-finder", "Find merged PRs between two commits")
        {
            StartRefOption,
            EndRefOption,
            PathOption,
            FormatOption,
            VerbosityOption,
            RepoPathOption,
            LabelsOption,
            GitHubTokenOption,
        };
        prFinderCommand.Action = s_prFinderCommandHandler;
        return prFinderCommand;
    }

    private class PrFinderCommandDefaultHandler : AsynchronousCommandLineAction
    {
        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var logger = parseResult.SetupLogging();

            var startRef = parseResult.GetValue(StartRefOption)!;
            var endRef = parseResult.GetValue(EndRefOption)!;
            var path = parseResult.GetValue(PathOption);
            var format = parseResult.GetValue(FormatOption)!;
            var repoPath = parseResult.GetValue(RepoPathOption);
            var labels = parseResult.GetValue(LabelsOption);
            ArgumentNullException.ThrowIfNull(labels);

            var settings = parseResult.LoadSettings(logger);
            if (string.IsNullOrEmpty(settings.GitHubToken))
            {
                logger.LogWarning("Missing GitHub authentication token.");
            }

            using var remoteConnections = new RemoteConnections(settings, loginToAzureDevOps: false);
            return await PRFinder.PRFinder.FindPRsAsync(startRef, endRef, path, format, labels, remoteConnections, logger, repoPath);
        }
    }
}
