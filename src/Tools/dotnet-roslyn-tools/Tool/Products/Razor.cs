// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Products;

internal class Razor : IProduct
{
    public string Name => "Razor";

    public string RepoBaseUrl => "https://github.com/dotnet/razor-tooling";
    public string ComponentJsonFileName => @".corext\Configs\aspnet-components.json";
    public string ComponentName => "Microsoft.VisualStudio.RazorExtension";
    public string? PackageName => null;
    public string? ArtifactsFolderName => null;
    public string[] ArtifactsSubFolderNames => Array.Empty<string>();

    public string? GetBuildPipelineName(string buildProjectName)
        => buildProjectName switch
        {
            "internal" => "razor-tooling-ci-official",
            _ => null
        };
}
