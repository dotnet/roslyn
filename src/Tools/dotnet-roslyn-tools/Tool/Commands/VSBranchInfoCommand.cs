// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.RoslynTools.VS;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class VSBranchInfoCommand
{
    private static readonly VSBranchInfoCommandDefaultHandler s_vsBranchInfoCommandHandler = new();

    private static readonly string[] s_allProductNames = [.. VSBranchInfo.AllProducts.Select(p => p.Name.ToLower()), .. new[] { "all" }];

    internal static readonly Option<string> BranchOption = new(["--branch", "-b"], () => "main", "Which VS branch to show information for (eg main, rel/d17.1)");
    internal static readonly Option<string?> TagOption = new(["--tag", "-t"], "Which VS tag to show information for (eg release/vs/17.8-preview.3). This overrides \"branch\" option if provided");
    internal static readonly Option<string> ProductOption = new Option<string>(["--product", "-p"], () => "roslyn", "Which product to get info for").FromAmong(s_allProductNames);
    internal static readonly Option ShowArtifacts = new(["--show-artifacts", "-a"], "Whether to show artifact download links for the packages product by the build (if available)");

    public static Symbol GetCommand()
    {
        var command = new Command("vsbranchinfo", "Provides information about the state of Roslyn in one or more branches of Visual Studio.")
        {
            BranchOption,
            TagOption,
            ProductOption,
            ShowArtifacts,
            VerbosityOption,
            DevDivAzDOTokenOption,
            DncEngAzDOTokenOption,
            IsCIOption,
        };
        command.Handler = s_vsBranchInfoCommandHandler;
        return command;
    }

    private class VSBranchInfoCommandDefaultHandler : ICommandHandler
    {
        public Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();
            var settings = context.ParseResult.LoadSettings(logger);

            var branch = context.ParseResult.GetValueForOption(BranchOption)!;
            var tag = context.ParseResult.GetValueForOption(TagOption);
            var product = context.ParseResult.GetValueForOption(ProductOption)!;
            var showArtifacts = context.ParseResult.WasOptionUsed([.. ShowArtifacts.Aliases]);

            // tag option overrides branch option
            var (gitVersionType, gitVersion) = tag is null ? (GitVersionType.Branch, branch) : (GitVersionType.Tag, tag);

            return VSBranchInfo.GetInfoAsync(gitVersion, gitVersionType, product, showArtifacts, settings, logger);
        }
    }
}
