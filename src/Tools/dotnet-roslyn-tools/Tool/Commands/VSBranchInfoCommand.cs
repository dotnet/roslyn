// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.VS;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class VSBranchInfoCommand
{
    private static readonly VSBranchInfoCommandDefaultHandler s_vsBranchInfoCommandHandler = new();

    private static readonly string[] s_allProductNames = [.. Product.AllProducts.Select(p => p.Name.ToLower()), .. new[] { "all" }];

    internal static readonly Option<string> BranchOption = new("--branch", "-b")
    {
        Description = "Which VS branch to show information for (eg main, rel/d17.1)",
        DefaultValueFactory = _ => "main",
    };
    internal static readonly Option<string?> TagOption = new("--tag", "-t")
    {
        Description = "Which VS tag to show information for (eg release/vs/17.8-preview.3). This overrides \"branch\" option if provided",
    };
    internal static readonly Option<string> ProductOption = new("--product", "-p")
    {
        Description = "Which product to get info for",
        DefaultValueFactory = _ => "roslyn",
    };
    internal static readonly Option<bool> ShowArtifacts = new("--show-artifacts", "-a")
    {
        Description = "Whether to show artifact download links for the packages product by the build (if available)",
    };

    static VSBranchInfoCommand()
    {
        ProductOption.AcceptOnlyFromAmong(s_allProductNames);
    }

    public static Command GetCommand()
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
        command.Action = s_vsBranchInfoCommandHandler;
        return command;
    }

    private class VSBranchInfoCommandDefaultHandler : AsynchronousCommandLineAction
    {
        public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var logger = parseResult.SetupLogging();
            var settings = parseResult.LoadSettings(logger);

            var branch = parseResult.GetValue(BranchOption)!;
            var tag = parseResult.GetValue(TagOption);
            var product = parseResult.GetValue(ProductOption)!;
            var showArtifacts = parseResult.GetValue(ShowArtifacts);

            // tag option overrides branch option
            var (gitVersionType, gitVersion) = tag is null ? (GitVersionType.Branch, branch) : (GitVersionType.Tag, tag);

            return VSBranchInfo.GetInfoAsync(gitVersion, gitVersionType, product, showArtifacts, settings, logger);
        }
    }
}
