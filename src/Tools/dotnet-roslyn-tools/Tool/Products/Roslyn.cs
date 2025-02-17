// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Products;

internal class Roslyn : IProduct
{
    public string Name => "Roslyn";

    public string RepoHttpBaseUrl => "https://github.com/dotnet/roslyn";
    public string InternalRepoBaseUrl => "https://dnceng.visualstudio.com/internal/_git/dotnet-roslyn";

    public string RepoSshBaseUrl => "git@github.com:dotnet/roslyn.git";
    public string GitUserName => "dotnet bot";
    public string GitEmail => "dotnet-bot@microsoft.com";

    public string ComponentJsonFileName => @".corext\Configs\dotnetcodeanalysis-components.json";
    public string ComponentName => "Microsoft.CodeAnalysis.LanguageServices";
    public string? VsPackageName => "VS.ExternalAPIs.Roslyn";
    public string? VsPackagePropsFileName => "src/ConfigData/Packages/roslyn.props";
    public string? DartLabPipelineName => "Roslyn Integration CI DartLab";
    public string? ArtifactsFolderName => "PackageArtifacts";
    public string[] ArtifactsSubFolderNames => ["PackageArtifacts/PreRelease", "PackageArtifacts/Release"];

    public string SdkPackageName => "Microsoft.Net.Compilers.Toolset";

    public string? GetBuildPipelineName(string buildProjectName)
        => buildProjectName switch
        {
            "DevDiv" => "Roslyn-Signed",
            "internal" => "dotnet-roslyn-official",  // dnceng
            _ => throw new InvalidOperationException($"No idea what the build is called when the project is {buildProjectName}.")
        };
}
