// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.RoslynTools.VS;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class CreateReleaseTagsCommand
{
    private static readonly CreateReleaseTagsCommandDefaultHandler s_defaultHandler = new();

    // Filter the product to only those with git credentials, as we need to be able to commit to the repo to add tags
    private static readonly string[] s_allProductNames = [.. VSBranchInfo.AllProducts.Where(p => p.GitUserName.Length > 0).Select(p => p.Name.ToLower())];

    internal static readonly Option<string> ProductOption = new Option<string>(["--product", "-p"], () => "roslyn", "Which product to get info for").FromAmong(s_allProductNames);

    public static Symbol GetCommand()
    {
        var command = new Command("create-release-tags", "Generates git tags for VS releases in the repo.")
        {
            ProductOption,
            VerbosityOption,
            DevDivAzDOTokenOption,
            DncEngAzDOTokenOption,
            IsCIOption,
        };
        command.Handler = s_defaultHandler;
        return command;
    }

    private class CreateReleaseTagsCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();
            var settings = context.ParseResult.LoadSettings(logger);

            var product = context.ParseResult.GetValueForOption(ProductOption)!;

            return await CreateReleaseTags.CreateReleaseTags.CreateReleaseTagsAsync(product, settings, logger);
        }
    }
}

