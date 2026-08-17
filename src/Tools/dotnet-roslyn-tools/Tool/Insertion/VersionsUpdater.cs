// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Microsoft.RoslynTools.Insertion;

internal sealed class VersionsUpdater
{
    private const string VersionsTemplatePath = "src/ProductData/AssemblyVersions.tt";
    private readonly string _versionsTemplateOriginal;
    private string _versionsTemplateContent;

    public List<string> WarningMessages { get; }

    private VersionsUpdater(string versionsTemplate, List<string> warningMessages)
    {
        _versionsTemplateOriginal = versionsTemplate;
        _versionsTemplateContent = versionsTemplate;
        WarningMessages = warningMessages;
    }

    public static async Task<VersionsUpdater> Create(GitHttpClient gitClient, string commitId, List<string> warningMessages)
    {
        var vsRepoId = RoslynInsertionTool.VSRepoId;
        var version = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = commitId };
        var versionsTemplateContent = await gitClient.GetItemContentAsync(vsRepoId, VersionsTemplatePath, download: true, versionDescriptor: version);
        using var reader = new StreamReader(versionsTemplateContent);
        var versionsTemplate = reader.ReadToEnd();
        return new VersionsUpdater(versionsTemplate, warningMessages);
    }

    public GitChange? GetChangeOpt()
    {
        return RoslynInsertionTool.GetChangeOpt(VersionsTemplatePath, _versionsTemplateOriginal, _versionsTemplateContent);
    }

    public void UpdateComponentVersion(string assemblyName, Version newVersion)
    {
        var path = VersionsTemplatePath;
        var content = _versionsTemplateContent;
        var variableName = assemblyName switch
        {
            "Roslyn" => "MicrosoftCodeAnalysisVersion",
            _ => assemblyName.Replace(".", string.Empty) + "Version",
        };

        // the structure of the file is:
        //   header: opening tag:    <#+
        //           comments:           //
        //   values:                     const string AssemblyNameVersion = "<version-number>";
        //   footer: closing tag:    #>
        var lines = content.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        bool IsVersionLine(string line) => line.TrimStart().StartsWith("const");

        // find the variable or the appropriate insertion location
        var constExpression = "const string";
        string GetLineVariableName(string line)
        {
            var startIndex = line.IndexOf(constExpression) + constExpression.Length + 1;
            var endIndex = line.IndexOf(' ', startIndex + 1);
            var variableLength = endIndex - startIndex;
            return line.Substring(startIndex, variableLength);
        }

        // rather than get fancy, linearly search through the sorted list and update or add as appropriate
        // the file is small so this is fine
        var valueUpdated = false;
        var newLine = $@"    const string {variableName} = ""{newVersion.ToFullVersion()}"";";
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            if (string.IsNullOrWhiteSpace(line) || !IsVersionLine(line))
            {
                continue;
            }

            var currentVariableName = GetLineVariableName(line);

            // The lines are already sorted in the file, so compare by name to see where the new line needs to go.
            var comparison = String.Compare(currentVariableName, variableName, StringComparison.OrdinalIgnoreCase);
            if (comparison == 0)
            {
                // found exact match, replace this line
                var versionStart = IndexOfOrThrow(line, '"') + 1;
                var versionEnd = IndexOfOrThrow(line, '"', versionStart);
                var versionStr = line[versionStart..versionEnd];
                var oldVersion = ParseAndValidatePreviousVersion(versionStr, path, variableName, assemblyName);
                if (newVersion > oldVersion)
                {
                    newLine = line[..versionStart] + newVersion.ToFullVersion() + line[versionEnd..];
                    lines[lineIndex] = newLine;
                }
                valueUpdated = true;
                break;
            }
            else if (comparison > 0)
            {
                // we passed it, add it right before this line
                lines.Insert(lineIndex, newLine);
                valueUpdated = true;
                break;
            }
        }

        if (!valueUpdated)
        {
            // value was last alphabetically, just add it to the end
            lines.Add(newLine);
        }

        _versionsTemplateContent = string.Join("\r\n", lines);
    }

    private static int IndexOfOrThrow(string str, char value, int startIndex = 0)
    {
        var result = str.IndexOf(value, startIndex);
        if (result < 0)
        {
            throw new InvalidDataException($"The specified character '{value}' was not found in the string: {str}");
        }

        return result;
    }

    private static Version ParseAndValidatePreviousVersion(string versionStringOpt, string path, string description, string assemblyName)
    {
        // first time we run new insertion tool we need to skip checking previous version since it has a different format
        if (!Version.TryParse(versionStringOpt, out var previousVersion))
        {
            throw new InvalidDataException($"The value of {description} '{versionStringOpt}' doesn't have the expected format: '#.#.#.#' ('{path}', binding redirect for '{assemblyName}')");
        }

        return previousVersion;
    }
}
