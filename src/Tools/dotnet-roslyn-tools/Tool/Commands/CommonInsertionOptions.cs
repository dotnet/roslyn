// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Insertion;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.Commands;

internal static class CommonInsertionOptions
{
    public static Option<string> CreateComponentBranchOption(bool required) => new("--component-branch")
    {
        Description = "Component source branch to insert from.",
        Required = required,
    };

    public static Option<bool> CreateDraftPrOption(bool defaultValue) => new("--create-draft-pr")
    {
        Description = "Create or update PR as draft.",
        DefaultValueFactory = _ => defaultValue,
    };

    public static readonly Option<string> InsertionNameOption = new("--insertion-name")
    {
        Description = "Friendly insertion name.",
        DefaultValueFactory = _ => "Roslyn",
    };

    public static readonly Option<string> VsBranchOption = new("--vs-branch")
    {
        Description = "Visual Studio branch to insert into.",
        Required = true,
    };

    public static readonly Option<string> ComponentBuildQueueOption = new("--component-build-queue")
    {
        Description = "Component build queue to insert from.",
        DefaultValueFactory = _ => "Roslyn-Signed",
    };

    public static readonly Option<string> SpecificBuildOption = new("--specific-build")
    {
        Description = "Specific build number to insert instead of latest passing build.",
    };

    public static readonly Option<bool> InsertToolsetOption = new("--insert-toolset")
    {
        Description = "Update Roslyn toolset package in VS.",
    };

    public static readonly Option<bool> InsertCoreXTPackagesOption = new("--insert-corext-packages")
    {
        Description = "Update CoreXT package references.",
        DefaultValueFactory = _ => true,
    };

    public static readonly Option<bool> InsertDevDivSourceFilesOption = new("--insert-devdiv-source-files")
    {
        Description = "Insert source file metadata into DevDiv paths.",
        DefaultValueFactory = _ => true,
    };

    public static readonly Option<bool> InsertWillowPackagesOption = new("--insert-willow-packages")
    {
        Description = "Update CoreXT components.json entries.",
        DefaultValueFactory = _ => true,
    };

    public static readonly Option<bool> UpdateCoreXTLibrariesOption = new("--update-corext-libraries")
    {
        Description = "Update CoreXT library links/versions.",
        DefaultValueFactory = _ => false,
    };

    public static readonly Option<bool> UpdateAssemblyVersionsOption = new("--update-assembly-versions")
    {
        Description = "Update assembly version declarations in VS.",
        DefaultValueFactory = _ => false,
    };

    public static readonly Option<bool> QueueValidationBuildOption = new("--queue-validation-build")
    {
        Description = "Queue VS validation policies for the insertion PR.",
        DefaultValueFactory = _ => false,
    };

    public static readonly Option<string> ValidationBuildQueueOption = new("--validation-build-queue")
    {
        Description = "Validation build policy display name.",
        DefaultValueFactory = _ => "DD-VS-VAL-VSALL (dev15 efforts)",
    };

    public static readonly Option<bool> RunDDRITsInValidationOption = new("--run-ddrits-in-validation")
    {
        Description = "Run DDRITs in validation.",
        DefaultValueFactory = _ => true,
    };

    public static readonly Option<bool> RunRPSInValidationOption = new("--run-rps-in-validation")
    {
        Description = "Run RPS tests in validation.",
        DefaultValueFactory = _ => true,
    };

    public static readonly Option<bool> RunSpeedometerInValidationOption = new("--run-speedometer-in-validation")
    {
        Description = "Run speedometer tests in validation.",
        DefaultValueFactory = _ => false,
    };

    public static readonly Option<bool> RetainInsertedBuildOption = new("--retain-inserted-build")
    {
        Description = "Mark inserted build as retained.",
        DefaultValueFactory = _ => true,
    };

    public static readonly Option<bool> SetAutoCompleteOption = new("--set-auto-complete")
    {
        Description = "Set insertion PR to auto-complete.",
        DefaultValueFactory = _ => false,
    };

    public static readonly Option<bool> SkipPackageVersionValidationOption = new("--skip-package-version-validation")
    {
        Description = "Skip package version monotonicity checks.",
        DefaultValueFactory = _ => false,
    };

    public static readonly Option<string> ReviewerGuidOption = new("--reviewer-guid")
    {
        Description = "Optional default reviewer GUID to assign to the PR.",
    };

    public static readonly Option<string> TitlePrefixOption = new("--title-prefix")
    {
        Description = "Optional PR title prefix.",
    };

    public static readonly Option<string> TitleSuffixOption = new("--title-suffix")
    {
        Description = "Optional PR title suffix.",
    };

    public static readonly Option<string> CherryPickOption = new("--cherry-pick")
    {
        Description = "Comma-separated VS commit SHAs to cherry-pick.",
    };

    public static readonly Option<string> SkipCoreXTPackagesOption = new("--skip-corext-packages")
    {
        Description = "Comma-separated CoreXT package names to skip.",
    };

    public static readonly Option<string> BuildConfigOption = new("--build-config")
    {
        Description = "Build configuration used when locating artifacts.",
        DefaultValueFactory = _ => "Release",
    };

    public static readonly Option<string> BuildDropPathOption = new("--build-drop-path")
    {
        Description = "Path to build drops for local artifact insertion/testing.",
        DefaultValueFactory = _ => "\\\\cpvsbuild\\drops\\Roslyn",
    };

    public static readonly Option<string> InsertionBranchPrefixOption = new("--insertion-branch-prefix")
    {
        Description = "Prefix used when creating insertion branches.",
        DefaultValueFactory = _ => "dev/dotnet-bot/insertions/",
    };

    public static readonly Option<string> VsAzdoUriOption = new("--vs-azdo-uri")
    {
        Description = "Azure DevOps URI for VS repo host.",
        DefaultValueFactory = _ => "https://dev.azure.com/devdiv",
    };

    public static readonly Option<string> VsProjectOption = new("--vs-project")
    {
        Description = "Azure DevOps project containing VS repo.",
        DefaultValueFactory = _ => "DevDiv",
    };

    public static readonly Option<string> ComponentAzdoUriOption = new("--component-azdo-uri")
    {
        Description = "Azure DevOps URI for component build host.",
        DefaultValueFactory = _ => "https://dev.azure.com/dnceng",
    };

    public static readonly Option<string> ComponentProjectOption = new("--component-project")
    {
        Description = "Azure DevOps project containing component builds.",
        DefaultValueFactory = _ => "internal",
    };

    public static async Task<int> ExecuteAsync(
        RoslynInsertionToolOptions insertionOptions,
        RemoteConnections remoteConnections,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!insertionOptions.CreateDummyPr && string.IsNullOrWhiteSpace(insertionOptions.ComponentBranchName))
        {
            logger.LogError("Component branch is required unless --dummy is specified.");
            return -1;
        }

        if (!insertionOptions.Valid)
        {
            logger.LogError("Insertion options are invalid:{NewLine}{ValidationErrors}", Environment.NewLine, insertionOptions.ValidationErrors);
            return -1;
        }

        var (success, pullRequestId) = await RoslynInsertionTool.PerformInsertionAsync(insertionOptions, remoteConnections, logger, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            return -1;
        }

        if (pullRequestId > 0)
        {
            logger.LogInformation("Insertion PR: https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/{PullRequestId}", pullRequestId);
        }

        return 0;
    }

    public static ImmutableArray<string> ParseCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return [.. value
            .Split(',')
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part))];
    }
}
