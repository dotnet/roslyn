// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Insertion;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.Commands;

internal static class UpdateInsertionCommand
{
    private static readonly UpdateInsertionCommandDefaultHandler s_handler = new();

    private static readonly Option<int> s_pullRequestIdOption = new("--pr-id")
    {
        Description = "Existing insertion PR ID to update.",
        Required = true,
    };

    private static readonly Option<bool> s_overwritePrOption = new("--overwrite-pr")
    {
        Description = "Reset insertion branch to target branch before applying updates.",
        DefaultValueFactory = _ => true,
    };

    private static readonly Option<string> s_componentBranchOption = CommonInsertionOptions.CreateComponentBranchOption(required: true);

    private static readonly Option<bool> s_createDraftPrOption = CommonInsertionOptions.CreateDraftPrOption(defaultValue: false);

    public static Command GetCommand()
    {
        var command = new Command("update-insertion", "Update an existing Visual Studio insertion PR with a new build.")
        {
            s_pullRequestIdOption,
            s_overwritePrOption,
            CommonInsertionOptions.InsertionNameOption,
            CommonInsertionOptions.VsBranchOption,
            CommonInsertionOptions.CreateComponentBranchOption(required: true),
            CommonInsertionOptions.ComponentBuildQueueOption,
            CommonInsertionOptions.SpecificBuildOption,

            CommonInsertionOptions.VsAzdoUriOption,
            CommonInsertionOptions.VsProjectOption,
            CommonInsertionOptions.ComponentAzdoUriOption,
            CommonInsertionOptions.ComponentProjectOption,

            CommonInsertionOptions.InsertToolsetOption,
            CommonInsertionOptions.InsertCoreXTPackagesOption,
            CommonInsertionOptions.SkipCoreXTPackagesOption,
            CommonInsertionOptions.InsertDevDivSourceFilesOption,
            CommonInsertionOptions.InsertWillowPackagesOption,
            CommonInsertionOptions.UpdateCoreXTLibrariesOption,
            CommonInsertionOptions.UpdateAssemblyVersionsOption,

            CommonInsertionOptions.QueueValidationBuildOption,
            CommonInsertionOptions.ValidationBuildQueueOption,
            CommonInsertionOptions.RunDDRITsInValidationOption,
            CommonInsertionOptions.RunRPSInValidationOption,
            CommonInsertionOptions.RunSpeedometerInValidationOption,
            CommonInsertionOptions.RetainInsertedBuildOption,

            CommonInsertionOptions.CreateDraftPrOption(defaultValue: false),
            CommonInsertionOptions.SetAutoCompleteOption,
            CommonInsertionOptions.ReviewerGuidOption,
            CommonInsertionOptions.TitlePrefixOption,
            CommonInsertionOptions.TitleSuffixOption,
            CommonInsertionOptions.CherryPickOption,

            CommonInsertionOptions.BuildConfigOption,
            CommonInsertionOptions.BuildDropPathOption,
            CommonInsertionOptions.InsertionBranchPrefixOption,
            CommonInsertionOptions.SkipPackageVersionValidationOption,

            CommonOptions.DevDivAzDOTokenOption,
            CommonOptions.DncEngAzDOTokenOption,
            CommonOptions.IsCIOption,
            CommonOptions.VerbosityOption,
        };

        command.Action = s_handler;
        return command;
    }

    private sealed class UpdateInsertionCommandDefaultHandler : AsynchronousCommandLineAction
    {
        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var logger = parseResult.SetupLogging();
            var settings = parseResult.LoadSettings(logger);

            var insertionOptions = new RoslynInsertionToolOptions(
                VisualStudioRepoAzdoUsername: "dn-bot@microsoft.com",
                VisualStudioRepoAzdoUri: parseResult.GetValue(CommonInsertionOptions.VsAzdoUriOption)!,
                VisualStudioRepoProjectName: parseResult.GetValue(CommonInsertionOptions.VsProjectOption)!,
                ComponentBuildQueueName: parseResult.GetValue(CommonInsertionOptions.ComponentBuildQueueOption)!,
                BuildConfig: parseResult.GetValue(CommonInsertionOptions.BuildConfigOption)!,
                InsertionBranchName: parseResult.GetValue(CommonInsertionOptions.InsertionBranchPrefixOption)!,
                BuildDropPath: parseResult.GetValue(CommonInsertionOptions.BuildDropPathOption)!,
                InsertCoreXTPackages: parseResult.GetValue(CommonInsertionOptions.InsertCoreXTPackagesOption),
                UpdateCoreXTLibraries: parseResult.GetValue(CommonInsertionOptions.UpdateCoreXTLibrariesOption),
                UpdateXamlRoslynVersion: false,
                InsertDevDivSourceFiles: parseResult.GetValue(CommonInsertionOptions.InsertDevDivSourceFilesOption),
                InsertWillowPackages: parseResult.GetValue(CommonInsertionOptions.InsertWillowPackagesOption),
                InsertionName: parseResult.GetValue(CommonInsertionOptions.InsertionNameOption)!,
                RetainInsertedBuild: parseResult.GetValue(CommonInsertionOptions.RetainInsertedBuildOption),
                QueueValidationBuild: parseResult.GetValue(CommonInsertionOptions.QueueValidationBuildOption),
                ValidationBuildQueueName: parseResult.GetValue(CommonInsertionOptions.ValidationBuildQueueOption)!,
                RunDDRITsInValidation: parseResult.GetValue(CommonInsertionOptions.RunDDRITsInValidationOption),
                RunRPSInValidation: parseResult.GetValue(CommonInsertionOptions.RunRPSInValidationOption),
                RunSpeedometerInValidation: parseResult.GetValue(CommonInsertionOptions.RunSpeedometerInValidationOption),
                LogFileLocation: string.Empty,
                CreateDraftPr: parseResult.GetValue(s_createDraftPrOption),
                SkipCoreXTPackages: CommonInsertionOptions.ParseCsv(parseResult.GetValue(CommonInsertionOptions.SkipCoreXTPackagesOption)))
            {
                VisualStudioBranchName = parseResult.GetValue(CommonInsertionOptions.VsBranchOption)!,
                VisualStudioRepoAzdoPassword = settings.DevDivAzureDevOpsToken,
                ComponentBuildAzdoUsername = "dn-bot@microsoft.com",
                ComponentBuildAzdoPassword = settings.DncEngAzureDevOpsToken,
                ComponentBuildAzdoUri = parseResult.GetValue(CommonInsertionOptions.ComponentAzdoUriOption) ?? string.Empty,
                ComponentBuildProjectName = parseResult.GetValue(CommonInsertionOptions.ComponentProjectOption) ?? string.Empty,
                ComponentBranchName = parseResult.GetValue(s_componentBranchOption) ?? string.Empty,
                SpecificBuild = parseResult.GetValue(CommonInsertionOptions.SpecificBuildOption) ?? string.Empty,
                UpdateAssemblyVersions = parseResult.GetValue(CommonInsertionOptions.UpdateAssemblyVersionsOption),
                InsertToolset = parseResult.GetValue(CommonInsertionOptions.InsertToolsetOption),
                CreateDummyPr = false,
                UpdateExistingPr = parseResult.GetValue(s_pullRequestIdOption),
                OverwritePr = parseResult.GetValue(s_overwritePrOption),
                TitlePrefix = parseResult.GetValue(CommonInsertionOptions.TitlePrefixOption) ?? string.Empty,
                TitleSuffix = parseResult.GetValue(CommonInsertionOptions.TitleSuffixOption) ?? string.Empty,
                SetAutoComplete = parseResult.GetValue(CommonInsertionOptions.SetAutoCompleteOption),
                CherryPick = CommonInsertionOptions.ParseCsv(parseResult.GetValue(CommonInsertionOptions.CherryPickOption)),
                ReviewerGUID = parseResult.GetValue(CommonInsertionOptions.ReviewerGuidOption) ?? string.Empty,
                SkipPackageVersionValidation = parseResult.GetValue(CommonInsertionOptions.SkipPackageVersionValidationOption),
            };

            if (string.IsNullOrWhiteSpace(settings.DevDivAzureDevOpsToken))
            {
                logger.LogError("Missing DevDiv Azure DevOps token.");
                return -1;
            }

            if (string.IsNullOrWhiteSpace(settings.DncEngAzureDevOpsToken))
            {
                logger.LogError("Missing DncEng Azure DevOps token.");
                return -1;
            }

            using var remoteConnections = new RemoteConnections(settings);

            return await CommonInsertionOptions.ExecuteAsync(
                insertionOptions,
                remoteConnections,
                logger,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
