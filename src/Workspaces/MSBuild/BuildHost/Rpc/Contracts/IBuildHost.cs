// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// RPC methods.
/// </summary>
internal interface IBuildHost
{
    bool HasUsableMSBuild(string projectOrSolutionFilePath);
    Task<int> LoadProjectFileAsync(string projectFilePath, string languageName, CancellationToken cancellationToken);

    /// <summary>Permits loading a project file which only exists in-memory, for example, for file-based program scenarios.</summary>
    /// <param name="projectFilePath">A path to a project file which may or may not exist on disk. Note that an extension that is known by MSBuild, such as .csproj or .vbproj, should be used here.</param>
    /// <param name="projectContent">The project file XML content.</param>
    int LoadProject(string projectFilePath, string projectContent, string languageName);

    Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken);
    Task ShutdownAsync();
}
