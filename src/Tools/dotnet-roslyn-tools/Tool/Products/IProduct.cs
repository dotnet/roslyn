// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Products;

internal interface IProduct
{
    string Name { get; }

    string RepoBaseUrl { get; }
    string ComponentJsonFileName { get; }
    string ComponentName { get; }
    string? PackageName { get; }
    string? ArtifactsFolderName { get; }
    string[] ArtifactsSubFolderNames { get; }

    string? GetBuildPipelineName(string buildProjectName);
}
