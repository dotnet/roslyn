// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class CreateReleaseTagsCommand
{
    private static readonly CreateReleaseTagsCommandDefaultHandler s_defaultHandler = new();

    // Filter the product to only those with git credentials, as we need to be able to commit to the repo to add tags
    private static readonly string[] s_allProductNames = [.. Product.AllProducts.Where(p => p.GitUserName.Length > 0).Select(p => p.Name.ToLower())];

    internal static readonly Option<string> ProductOption = new("--product", "-p")
    {
        Description = "Which product to get info for",
        DefaultValueFactory = _ => "roslyn",
    };

    static CreateReleaseTagsCommand()
    {
        ProductOption.AcceptOnlyFromAmong(s_allProductNames);
    }

    public static Command GetCommand()
    {
        var command = new Command("create-release-tags", "Generates git tags for VS releases in the repo.")
        {
            ProductOption,
            VerbosityOption,
            DevDivAzDOTokenOption,
            DncEngAzDOTokenOption,
            IsCIOption,
        };
        command.Action = s_defaultHandler;
        return command;
    }

    private class CreateReleaseTagsCommandDefaultHandler : AsynchronousCommandLineAction
    {
        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var logger = parseResult.SetupLogging();
            var settings = parseResult.LoadSettings(logger);

            var product = parseResult.GetValue(ProductOption)!;

            using var remoteConnections = new RemoteConnections(settings);
            return await CreateReleaseTags.CreateReleaseTags.CreateReleaseTagsAsync(
                product,
                remoteConnections,
                logger);
        }
    }
}

