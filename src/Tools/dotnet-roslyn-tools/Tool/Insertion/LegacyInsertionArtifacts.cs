// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Insertion;

internal sealed class LegacyInsertionArtifacts : InsertionArtifacts
{
    public override string RootDirectory { get; }

    public LegacyInsertionArtifacts(string binariesDirectory)
    {
        RootDirectory = binariesDirectory;
    }

    public static string GetArtifactName(string buildNumber) => buildNumber;

    public static bool TryCreateFromLocalBuild(string buildDirectory, out InsertionArtifacts? artifacts)
    {
        if (buildDirectory.EndsWith(@"Binaries\Debug", StringComparison.OrdinalIgnoreCase) ||
            buildDirectory.EndsWith(@"Binaries\Release", StringComparison.OrdinalIgnoreCase))
        {
            artifacts = new LegacyInsertionArtifacts(buildDirectory);
            return true;
        }

        artifacts = null;
        return false;
    }

    public override string GetPackagesDirectory()
    {
        // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Main-Signed-Release\20160315.3\DevDivPackages"
        var devDivPackagesPath = Path.Combine(RootDirectory, "DevDivPackages");
        if (Directory.Exists(devDivPackagesPath))
        {
            return devDivPackagesPath;
        }

        // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Project-System\DotNet-Project-System\20180111.1\packages"
        var packagesPath = Path.Combine(RootDirectory, "packages");
        if (Directory.Exists(packagesPath))
        {
            return packagesPath;
        }

        throw new InvalidOperationException($"Unable to find packages path, tried '{devDivPackagesPath}' and '{packagesPath}'");
    }

    public override string GetDependentAssemblyVersionsFile()
        => Path.Combine(RootDirectory, "DevDivInsertionFiles", "DependentAssemblyVersions.csv");

    public override string[] GetOptProfPropertyFiles()
        => [];
}
