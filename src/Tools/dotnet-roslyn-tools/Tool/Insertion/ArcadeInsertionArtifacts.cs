// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Insertion;

internal sealed class ArcadeInsertionArtifacts(string vsSetupDirectory) : InsertionArtifacts
{
    public const string ArtifactName = "VSSetup";

    public override string RootDirectory { get; } = vsSetupDirectory;

    public static bool TryCreateFromLocalBuild(string buildDirectory, out InsertionArtifacts? artifacts)
    {
        if (buildDirectory.EndsWith(ArtifactName, StringComparison.OrdinalIgnoreCase))
        {
            RoslynInsertionTool.LogInformation($"Using artifacts provided in BuildDropPath: {buildDirectory}");
            artifacts = new ArcadeInsertionArtifacts(buildDirectory);
            return true;
        }

        artifacts = null;
        return false;
    }

    public override string GetPackagesDirectory()
    {
        var devDivPackagesPath = Path.Combine(RootDirectory, "DevDivPackages");
        if (Directory.Exists(devDivPackagesPath))
        {
            return devDivPackagesPath;
        }

        throw new InvalidOperationException($"Unable to find packages path, tried '{devDivPackagesPath}'");
    }

    public override string GetDependentAssemblyVersionsFile()
        => Path.Combine(RootDirectory, "DevDivPackages", "DependentAssemblyVersions.csv");

    public override string[] GetOptProfPropertyFiles()
    {
        var optProfPath = Path.Combine(RootDirectory, "Insertion", "OptProf");
        return Directory.Exists(optProfPath)
            ? Directory.EnumerateFiles(optProfPath, "*.props", SearchOption.TopDirectoryOnly).ToArray()
            : [];
    }
}
