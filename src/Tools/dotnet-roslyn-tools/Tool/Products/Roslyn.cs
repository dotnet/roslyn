// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Products;

internal class Roslyn : IProduct
{
    public string Name => "Roslyn";

    public string RepoBaseUrl => "https://github.com/dotnet/roslyn";
    public string ComponentJsonFileName => @".corext\Configs\dotnetcodeanalysis-components.json";
    public string ComponentName => "Microsoft.CodeAnalysis.LanguageServices";
    public string? PackageName => "VS.ExternalAPIs.Roslyn";
    public string? ArtifactsFolderName => "PackageArtifacts";
    public string[] ArtifactsSubFolderNames => new[] { "PackageArtifacts/PreRelease", "PackageArtifacts/Release" };

    public string? GetBuildPipelineName(string buildProjectName)
        => buildProjectName switch
        {
            "DevDiv" => "Roslyn-Signed",
            "internal" => "dotnet-roslyn CI",  // dnceng
            _ => throw new InvalidOperationException($"No idea what the build is called when the project is {buildProjectName}.")
        };
}
