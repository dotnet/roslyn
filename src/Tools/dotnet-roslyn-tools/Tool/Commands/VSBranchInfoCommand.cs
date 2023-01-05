// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.RoslynTools.VS;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class VSBranchInfoCommand
{
    private static readonly VSBranchInfoCommandDefaultHandler s_vsBranchInfoCommandHandler = new();

    private static readonly string[] s_allProductNames = VSBranchInfo.AllProducts.Select(p => p.Name.ToLower()).Concat(new[] { "all" }).ToArray();

    internal static readonly Option<string> BranchOption = new(new[] { "--branch", "-b" }, () => "main", "Which VS branch to show information for (eg main, rel/d17.1)");
    internal static readonly Option<string> ProductOption = new Option<string>(new[] { "--product", "-p" }, () => "roslyn", "Which product to get info for").FromAmong(s_allProductNames);
    internal static readonly Option ShowArtifacts = new(new[] { "--show-artifacts", "-a" }, "Whether to show artifact download links for the packages product by the build (if available)");

    public static Symbol GetCommand()
    {
        var command = new Command("vsbranchinfo", "Provides information about the state of Roslyn in one or more branches of Visual Studio.")
        {
            BranchOption,
            ProductOption,
            ShowArtifacts,
            VerbosityOption,
            DevDivAzDOTokenOption,
            DncEngAzDOTokenOption
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
            var product = context.ParseResult.GetValueForOption(ProductOption)!;
            var showArtifacts = context.ParseResult.WasOptionUsed(ShowArtifacts.Aliases.ToArray());

            return VSBranchInfo.GetInfoAsync(branch, product, showArtifacts, settings, logger);
        }
    }
}

