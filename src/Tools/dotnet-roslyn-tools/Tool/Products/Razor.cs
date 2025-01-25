// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Products;

internal class Razor : IProduct
{
    public string Name => "Razor";

    public string RepoHttpBaseUrl => "https://github.com/dotnet/razor";
    public string InternalRepoBaseUrl => "";

    public string RepoSshBaseUrl => "git@github.com:dotnet/razor.git";
    public string GitUserName => "dotnet bot";
    public string GitEmail => "dotnet-bot@microsoft.com";

    public string ComponentJsonFileName => @".corext\Configs\aspnet-components.json";
    public string ComponentName => "Microsoft.VisualStudio.RazorExtension";
    public string? PackageName => null;
    public string? PackagePropsFileName => null;
    public string? DartLabPipelineName => null;
    public string? ArtifactsFolderName => null;
    public string[] ArtifactsSubFolderNames => [];

    public string? GetBuildPipelineName(string buildProjectName)
        => buildProjectName switch
        {
            "internal" => "razor-ci-official",
            _ => null
        };
}
