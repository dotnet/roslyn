// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.Commands;

internal static class PRValidationSuiteCommand
{
    private static readonly string[] s_allProductNames = [.. Product.AllProducts.Where(p => p.PRValidationPipelineName != null && p.DartLabPipelineName != null).Select(p => p.Name.ToLower())];

    private static readonly PRValidationSuiteCommandDefaultHandler s_prValidationSuiteCommandHandler = new();
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

    static PRValidationSuiteCommand()
    {
        ProductOption.AcceptOnlyFromAmong(s_allProductNames);
    }

    public static Command GetCommand()
    {
        var command = new Command("pr-suite",
            @"Runs the PR Validation pipeline and Dartlab pipeline for a given PR number and SHA.")
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

        command.Action = s_prValidationSuiteCommandHandler;
        return command;
    }

    private class PRValidationSuiteCommandDefaultHandler : AsynchronousCommandLineAction
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
            var dartResult = await Validation.DartTest.RunDartPipeline(product, remoteConnections, logger, pr, shaFromPR).ConfigureAwait(false);
            var prValResult = await Validation.PRValidation.RunPRValidationPipeline(product, remoteConnections, logger, pr, shaFromPR, branch).ConfigureAwait(false);

            return dartResult == 0 && prValResult == 0
                ? 0
                : -1;
        }
    }
}
