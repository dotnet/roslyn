// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;

namespace Microsoft.RoslynTools.Insertion;

internal static partial class RoslynInsertionTool
{
    private const string TestsDropJsonFileName = "TestsDrop.json";

    /// <summary>
    /// Updates the runsettingsuri field in the VS test stage configuration file
    /// with the vsdrop link to the specified runsettings file from the build.
    /// </summary>
    /// <param name="gitClient">The Git client for accessing VS repo.</param>
    /// <param name="baseCommitId">The base commit to compare against.</param>
    /// <param name="artifacts">The insertion artifacts containing the TestsDrop.json file.</param>
    /// <returns>A GitChange if the file needs to be updated, null otherwise.</returns>
    internal static async Task<GitChange?> UpdateRunSettingsUriAsync(
        GitHttpClient gitClient,
        string baseCommitId,
        InsertionArtifacts artifacts)
    {
        // Find the Tests vsdrop link and stage config path from the TestsDrop.json file
        // If the file doesn't exist, this feature is not enabled for this component
        var testsDropInfo = ReadTestsDropInfo(artifacts);
        if (testsDropInfo == null)
        {
            return null;
        }

        var (testsVsDropUri, stageConfigPath) = testsDropInfo.Value;

        LogInformation($"Found runsettings vsdrop URI: {testsVsDropUri}");
        LogInformation($"Target stage config path: {stageConfigPath}");

        // Read the current stage.yml file
        var version = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = baseCommitId };
        string originalContent;
        try
        {
            using var stream = await gitClient.GetItemContentAsync(
                VSRepoId,
                stageConfigPath,
                download: true,
                versionDescriptor: version);
            originalContent = await new StreamReader(stream).ReadToEndAsync();
        }
        catch (Exception ex)
        {
            LogWarning($"Could not read '{stageConfigPath}': {ex.Message}");
            return null;
        }

        // Update the runsettingsuri field in the YAML
        var newContent = UpdateRunSettingsUriInYaml(originalContent, testsVsDropUri);
        if (newContent == null)
        {
            LogWarning($"Could not find runsettingsuri field in '{stageConfigPath}'.");
            return null;
        }

        return GetChangeOpt(stageConfigPath, originalContent, newContent);
    }

    /// <summary>
    /// Reads the TestsDrop.json file and returns the runsettings URI and stage config path.
    /// </summary>
    private static (string testsRunsettingsUri, string stageConfigPath)? ReadTestsDropInfo(InsertionArtifacts artifacts)
    {
        // Search for TestsDrop.json anywhere in the artifacts directory (same pattern as vsman files)
        var jsonFiles = Directory.EnumerateFiles(
            artifacts.RootDirectory,
            TestsDropJsonFileName,
            SearchOption.AllDirectories);

        foreach (var jsonPath in jsonFiles)
        {
            try
            {
                LogInformation($"Reading Tests drop info from: {jsonPath}");
                var jsonContent = File.ReadAllText(jsonPath);
                var testsDropInfo = JsonConvert.DeserializeAnonymousType(jsonContent, new
                {
                    testsRunsettingsUri = "",
                    stageConfigPath = ""
                });

                if (testsDropInfo is null)
                {
                    LogInformation($"Warning: Could not parse '{jsonPath}'");
                    continue;
                }

                if (string.IsNullOrEmpty(testsDropInfo.testsRunsettingsUri))
                {
                    LogWarning($"'{jsonPath}' is missing 'testsRunsettingsUri' field.");
                    return null;
                }

                if (string.IsNullOrEmpty(testsDropInfo.stageConfigPath))
                {
                    LogWarning($"'{jsonPath}' is missing 'stageConfigPath' field.");
                    return null;
                }

                return (testsDropInfo.testsRunsettingsUri, testsDropInfo.stageConfigPath);
            }
            catch (Exception ex)
            {
                // JSON exists but is malformed - this is a warning
                LogWarning($"Could not parse '{jsonPath}': {ex.Message}");
            }
        }

        // No TestsDrop.json found - this is expected for components that haven't opted in
        LogInformation($"No '{TestsDropJsonFileName}' found in build artifacts. Skipping runsettingsuri update.");
        return null;
    }

    /// <summary>
    /// Updates the runsettingsuri field in the YAML content.
    /// </summary>
    /// <param name="yamlContent">The original YAML content.</param>
    /// <param name="newUri">The new runsettings URI to set.</param>
    /// <returns>The updated YAML content, or null if the field was not found.</returns>
    internal static string? UpdateRunSettingsUriInYaml(string yamlContent, string newUri)
    {
        const string FieldName = "runsettingsuri:";
        var lines = yamlContent.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var fieldIndex = line.IndexOf(FieldName, StringComparison.OrdinalIgnoreCase);
            if (fieldIndex >= 0)
            {
                var prefix = line[..(fieldIndex + FieldName.Length)];
                lines[i] = $"{prefix} {newUri}";
                return string.Join("\n", lines);
            }
        }

        return null;
    }
}
