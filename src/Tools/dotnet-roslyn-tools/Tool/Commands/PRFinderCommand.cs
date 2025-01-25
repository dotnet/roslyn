// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal static class PRFinderCommand
{
    private static readonly PrFinderCommandDefaultHandler s_prFinderCommandHandler = new();

    internal static readonly string[] SupportedFormats = [PRFinder.PRFinder.DefaultFormat, PRFinder.PRFinder.OmniSharpFormat, PRFinder.PRFinder.ChangelogFormat];

    internal static readonly Option<string> StartRefOption = new(["--start", "-s"], "The ref to start looking for PRs from. This value can be a branch name, tag name, or commit SHA.") { IsRequired = true };
    internal static readonly Option<string> EndRefOption = new(["--end", "-e"], "The ref to stop looking for PRs at. This value can be a branch name, tag name, or commit SHA.") { IsRequired = true };
    internal static readonly Option<string?> PathOption = new(["--path"], "When set only PRs that touch the specified path will be included in the output.");
    internal static readonly Option<string> FormatOption = new Option<string>(["--format"], () => PRFinder.PRFinder.DefaultFormat, "The formatting to apply to the PR list.").FromAmong(SupportedFormats);
    internal static readonly Option<string?> RepoPathOption = new(["--repo"], () => null, "The directory of the repository to look for PRs. If none is provided, the current directory is used");

    public static Symbol GetCommand()
    {
        var prFinderCommand = new Command("pr-finder", "Find merged PRs between two commits")
        {
            StartRefOption,
            EndRefOption,
            PathOption,
            FormatOption,
            VerbosityOption,
            RepoPathOption
        };
        prFinderCommand.Handler = s_prFinderCommandHandler;
        return prFinderCommand;
    }

    private class PrFinderCommandDefaultHandler : ICommandHandler
    {
        public Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();

            var startRef = context.ParseResult.GetValueForOption(StartRefOption)!;
            var endRef = context.ParseResult.GetValueForOption(EndRefOption)!;
            var path = context.ParseResult.GetValueForOption(PathOption);
            var format = context.ParseResult.GetValueForOption(FormatOption)!;
            var repoPath = context.ParseResult.GetValueForOption(RepoPathOption);

            return PRFinder.PRFinder.FindPRsAsync(startRef, endRef, path, format, logger, repoPath: repoPath);
        }
    }
}
