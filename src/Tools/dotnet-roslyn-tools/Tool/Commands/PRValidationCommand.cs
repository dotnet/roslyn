// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;

namespace Microsoft.RoslynTools.Commands;

internal static class PRValidationCommand
{
    private static readonly string[] s_allProductNames = [.. VSBranchInfo.AllProducts.Where(p => p.PRValidationPipelineName != null).Select(p => p.Name.ToLower())];

    private static readonly PRValidationCommandDefaultHandler s_prValidationCommandHandler = new();
    internal static readonly Option<int> PrNumberOption = new("--prNumber", "-n")
    {
        Description = "PR number",
        Required = true,
    };

    internal static readonly Option<string> ShaOption = new("--sha", "-s")
    {
        Description = "Relevant SHA",
        Required = false,
    };

    internal static readonly Option<string> BranchOption = new("--branch", "-b")
    {
        Description = "Branch to run pipeline from",
        DefaultValueFactory = _ => "main",
        Required = false,
    };

    internal static readonly Option<string> ProductOption = new("--product", "-p")
    {
        Description = "Product to get info for",
        DefaultValueFactory = _ => "roslyn",
        Required = false,
    };

    static PRValidationCommand()
    {
        ProductOption.AcceptOnlyFromAmong(s_allProductNames);
    }

    public static Command GetCommand()
    {
        var command = new Command("pr-val",
            @"Runs the PR Validation pipeline for a given PR number and SHA.")
        {
            PrNumberOption,
            ShaOption,
            ProductOption,
            BranchOption,
            CommonOptions.GitHubTokenOption,
            CommonOptions.DevDivAzDOTokenOption,
            CommonOptions.DncEngAzDOTokenOption,
            CommonOptions.IsCIOption,
        };

        command.Action = s_prValidationCommandHandler;
        return command;
    }

    private class PRValidationCommandDefaultHandler : AsynchronousCommandLineAction
    {
        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var logger = parseResult.SetupLogging();
            var settings = parseResult.LoadSettings(logger);

            var isMissingAzDOToken = string.IsNullOrEmpty(settings.DevDivAzureDevOpsToken) || string.IsNullOrEmpty(settings.DncEngAzureDevOpsToken);
            if (string.IsNullOrEmpty(settings.GitHubToken) ||
                (settings.IsCI && isMissingAzDOToken))
            {
                logger.LogError("Missing authentication token.");
                return -1;
            }

            var pr = parseResult.GetValue(PrNumberOption);
            var shaFromPR = parseResult.GetValue(ShaOption);

            if (shaFromPR is null)
            {
                logger.LogInformation("Using most recent SHA");
            }

            var product = parseResult.GetValue(ProductOption)!;

            var branch = parseResult.GetValue(BranchOption);

            using var remoteConnections = new RemoteConnections(settings);

            return await Validation.PRValidation.RunPRValidationPipeline(
                product,
                remoteConnections,
                logger,
                pr,
                shaFromPR,
                branch).ConfigureAwait(false);
        }
    }
}
