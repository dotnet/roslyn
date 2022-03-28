// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.VS.Products;

internal class Roslyn : IProduct
{
    public string Name => "Roslyn";

    public string RepoBaseUrl => "https://github.com/dotnet/roslyn";
    public string ComponentJsonFileName => @".corext\Configs\dotnetcodeanalysis-components.json";
    public string ComponentName => "Microsoft.CodeAnalysis.LanguageServices";
    public string BuildPipelineName => "dotnet-roslyn CI";
    public string? PackageName => "VS.ExternalAPIs.Roslyn";
    public string? ArtifactsFolderName => "PackageArtifacts";
    public string[] ArtifactsSubFolderNames => new[] { "PackageArtifacts/PreRelease", "PackageArtifacts/Release" };
}
