// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Text;

namespace Microsoft.RoslynTools.Insertion;

internal sealed record RoslynInsertionToolOptions(
    string VisualStudioRepoAzdoUsername,
    string VisualStudioRepoAzdoUri,
    string VisualStudioRepoProjectName,
    string ComponentBuildQueueName,
    string InsertionBranchName,
    string BuildDropPath,
    bool InsertCoreXTPackages,
    bool UpdateCoreXTLibraries,
    bool UpdateXamlRoslynVersion,
    bool InsertDevDivSourceFiles,
    bool InsertWillowPackages,
    string InsertionName,
    bool RetainInsertedBuild,
    bool QueueValidationBuild,
    bool RunDDRITsInValidation,
    bool RunRPSInValidation,
    bool RunSpeedometerInValidation,
    string LogFileLocation,
    bool CreateDraftPr,
    ImmutableArray<string> SkipCoreXTPackages)
{

    public string VisualStudioBranchName { get; init; } = string.Empty;
    public string DevDivAzdoToken { get; init; } = string.Empty;
    public string ComponentBuildAzdoUsername { get; init; } = string.Empty;
    public string DncEngAzdoToken { get; init; } = string.Empty;
    public string ComponentBuildAzdoUri { get; init; } = string.Empty;
    public string ComponentBuildProjectName { get; init; } = string.Empty;
    public string ComponentBranchName { get; init; } = string.Empty;
    public string ComponentGitHubRepoName { get; init; } = string.Empty;
    public string SpecificBuild { get; init; } = string.Empty;
    public bool UpdateAssemblyVersions { get; init; }
    public bool InsertToolset { get; init; }
    public bool CreateDummyPr { get; init; }
    public int UpdateExistingPr { get; init; }
    public bool OverwritePr { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string TitlePrefix { get; init; } = string.Empty;
    public string TitleSuffix { get; init; } = string.Empty;
    public bool SetAutoComplete { get; init; }
    public ImmutableArray<string> CherryPick { get; init; }
    public string ReviewerGUID { get; init; } = string.Empty;
    public bool SkipPackageVersionValidation { get; init; }

    public string ComponentBuildProjectNameOrFallback
        => ComponentBuildProjectName ?? VisualStudioRepoProjectName;

    public bool Valid
    {
        get
        {
            if (ComponentBuildAzdoUri != null && ComponentBuildAzdoUsername is null)
            {
                // When the Build AzDO instance is separate from the VS AzDO instance, separate credentials must be specified.
                return false;
            }

            if (CreateDummyPr)
            {
                // only InsertionName and VisualStudioBranchName are required for creating a dummy pr
                return
                    UpdateExistingPr == 0 &&
                    !OverwritePr &&
                    !string.IsNullOrEmpty(InsertionName) &&
                    !string.IsNullOrEmpty(VisualStudioBranchName);
            }
            else if (UpdateExistingPr != 0)
            {
                // only the existing pr ID, InsertionName, BranchName, and BuildQueueName are required for overwriting an existing pr
                return
                    !CreateDummyPr &&
                    (!CreateDraftPr || OverwritePr) && // Create draft PR can only be specified when overwriting an existing pr
                    !string.IsNullOrEmpty(InsertionName) &&
                    !string.IsNullOrEmpty(ComponentBranchName) &&
                    !string.IsNullOrEmpty(VisualStudioBranchName) &&
                    !string.IsNullOrEmpty(ComponentBuildQueueName);
            }
            else
            {
                return
                    !OverwritePr &&
                    !string.IsNullOrEmpty(VisualStudioRepoAzdoUsername) &&
                    !string.IsNullOrEmpty(VisualStudioBranchName) &&
                    !string.IsNullOrEmpty(ComponentBuildQueueName) &&
                    !string.IsNullOrEmpty(ComponentBranchName) &&
                    !string.IsNullOrEmpty(VisualStudioRepoAzdoUri) &&
                    !string.IsNullOrEmpty(VisualStudioRepoProjectName) &&
                    !string.IsNullOrEmpty(BuildDropPath);
            }
        }
    }

    public string ValidationErrors
    {
        get
        {
            var builder = new StringBuilder();

            if (ComponentBuildAzdoUri != VisualStudioRepoAzdoUri)
            {
                if (ComponentBuildAzdoUsername is null)
                {
                    builder.AppendLine($"When {nameof(ComponentBuildAzdoUri)} is specified you must also specify the {nameof(ComponentBuildAzdoUsername)}.");
                }
            }

            if (CreateDummyPr)
            {
                // only InsertionName and VisualStudioBranchName are required for creating a dummy pr
                if (UpdateExistingPr != 0)
                {
                    builder.AppendLine($"{nameof(CreateDummyPr).ToLowerInvariant()} and {nameof(UpdateExistingPr).ToLowerInvariant()} are mutually exclusive and cannot be specified together");
                }

                if (OverwritePr)
                {
                    builder.AppendLine($"{nameof(CreateDummyPr).ToLowerInvariant()} and {nameof(OverwritePr).ToLowerInvariant()} are mutually exclusive and cannot be specified together");
                }

                if (string.IsNullOrEmpty(InsertionName))
                {
                    builder.AppendLine($"{nameof(InsertionName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(VisualStudioBranchName))
                {
                    builder.AppendLine($"{nameof(VisualStudioBranchName).ToLowerInvariant()} is required");
                }
            }
            else if (UpdateExistingPr != 0)
            {
                // perform a regular insertion
                if (CreateDraftPr && !OverwritePr)
                {
                    builder.AppendLine($"{nameof(CreateDraftPr).ToLowerInvariant()} can only be used with {nameof(UpdateExistingPr).ToLowerInvariant()} when {nameof(OverwritePr).ToLowerInvariant()} is true.");
                }

                // only the existing pr ID, InsertionName, BranchName, and BuildQueueName are required for overwriting an existing pr
                if (string.IsNullOrEmpty(InsertionName))
                {
                    builder.AppendLine($"{nameof(InsertionName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(ComponentBranchName))
                {
                    builder.AppendLine($"{nameof(ComponentBranchName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(VisualStudioBranchName))
                {
                    builder.AppendLine($"{nameof(VisualStudioBranchName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(ComponentBuildQueueName))
                {
                    builder.AppendLine($"{nameof(ComponentBuildQueueName).ToLowerInvariant()} is required");
                }
            }
            else
            {
                // perform a regular insertion
                if (OverwritePr)
                {
                    builder.AppendLine($"{nameof(OverwritePr).ToLowerInvariant()} can only be used with {nameof(UpdateExistingPr).ToLowerInvariant()}.");
                }

                if (string.IsNullOrEmpty(VisualStudioRepoAzdoUsername))
                {
                    builder.AppendLine($"{nameof(VisualStudioRepoAzdoUsername).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(VisualStudioBranchName))
                {
                    builder.AppendLine($"{nameof(VisualStudioBranchName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(ComponentBuildQueueName))
                {
                    builder.AppendLine($"{nameof(ComponentBuildQueueName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(ComponentBranchName))
                {
                    builder.AppendLine($"{nameof(ComponentBranchName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(VisualStudioRepoAzdoUri))
                {
                    builder.AppendLine($"{nameof(VisualStudioRepoAzdoUri).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(VisualStudioRepoProjectName))
                {
                    builder.AppendLine($"{nameof(VisualStudioRepoProjectName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(BuildDropPath))
                {
                    builder.AppendLine($"{nameof(BuildDropPath).ToLowerInvariant()} is required");
                }
            }

            return builder.ToString();
        }
    }
}
