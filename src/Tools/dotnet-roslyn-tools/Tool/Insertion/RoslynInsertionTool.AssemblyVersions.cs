// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Microsoft.RoslynTools.Insertion;

internal static partial class RoslynInsertionTool
{
    private const string VersionEqualsPrefix = "Version=";

    private static async Task<GitChange?> UpdateAssemblyVersionsOpt(GitHttpClient gitClient, string commitId, InsertionArtifacts artifacts)
    {
        var versionsUpdater = await VersionsUpdater.Create(gitClient, commitId, WarningMessages);

        var pathToDependentAssemblyVersionsFile = artifacts.GetDependentAssemblyVersionsFile();
        if (File.Exists(pathToDependentAssemblyVersionsFile))
        {
            foreach (var nameAndVersion in ReadAssemblyVersions(pathToDependentAssemblyVersionsFile))
            {
                versionsUpdater.UpdateComponentVersion(nameAndVersion.Key, nameAndVersion.Value);
            }
        }
        else
        {
            LogInformation($"No dependent-assembly-versions file found at path '{pathToDependentAssemblyVersionsFile}'");
        }

        return versionsUpdater.GetChangeOpt();
    }

    private static IEnumerable<KeyValuePair<string, Version>> ReadAssemblyVersions(string path)
    {
        return from line in File.ReadAllLines(path)
               let columns = line.Split(',')
               let versionFull = columns[1].Trim()
               let versionProper = versionFull.StartsWith(VersionEqualsPrefix) ? versionFull[VersionEqualsPrefix.Length..] : versionFull
               let version = Version.Parse(versionProper)
               let fullVersion = new Version(version.Major, Math.Max(version.Minor, 0), Math.Max(version.Build, 0), Math.Max(version.Revision, 0))
               select new KeyValuePair<string, Version>(columns[0].Trim(), fullVersion);
    }
}
