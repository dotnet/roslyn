// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Xml.Linq;
using Microsoft.RoslynTools.Utilities;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json.Linq;

namespace Microsoft.RoslynTools.VS;

internal static class VisualStudioRepository
{
    public static async Task<string> GetBuildNumberFromComponentJsonFileAsync(string branch, AzDOConnection devdiv, string jsonFile, string componentName)
    {
        var fileContents = await GetFileContentsAsync(branch, devdiv, jsonFile);
        var componentsJson = JObject.Parse(fileContents);

        var url = componentsJson["Components"]?[componentName]?["url"]?.ToString();

        var parts = url?.Split(';');
        if (parts?.Length != 2)
        {
            throw new Exception($"Couldn't get URL and manifest. Got: {parts}");
        }

        if (!parts[1].EndsWith(".vsman"))
        {
            throw new Exception($"Couldn't get URL and manifest. Not a vsman file? Got: {parts}");
        }

        var buildNumber = new Uri(parts[0]).Segments.Last();
        return buildNumber;
    }

    public static async Task<string> GetPackageVersionFromDefaultConfigAsync(string branch, AzDOConnection devdiv, string packageName)
    {
        var fileContents = await GetFileContentsAsync(branch, devdiv, @".corext\Configs\default.config");

        var defaultConfig = XDocument.Parse(fileContents);

        var packageVersion = defaultConfig.Root?.Descendants("package").Where(p => p.Attribute("id")?.Value == packageName).Select(p => p.Attribute("version")?.Value).FirstOrDefault();

        if (packageVersion is null)
        {
            throw new Exception($"Couldn't find the {packageName} package for branch: {branch}");
        }

        return packageVersion;
    }

    public static async Task<string> GetFileContentsAsync(string branch, AzDOConnection devdiv, string jsonFile)
    {
        var commit = new GitVersionDescriptor { VersionType = GitVersionType.Branch, Version = branch };
        var vsRepository = await devdiv.GitClient.GetRepositoryAsync("DevDiv", "VS");

        using var componentsJsonStream = await devdiv.GitClient.GetItemContentAsync(
            vsRepository.Id,
            jsonFile,
            download: true,
            versionDescriptor: commit);

        using var streamReader = new StreamReader(componentsJsonStream);
        return await streamReader.ReadToEndAsync();
    }
}
