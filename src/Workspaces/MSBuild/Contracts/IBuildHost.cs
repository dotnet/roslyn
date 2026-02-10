// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// RPC methods.
/// </summary>
internal interface IBuildHost
{
    /// <summary>
    /// Finds the best MSBuild instance installed for loading the given project or solution.
    /// </summary>
    /// <remarks>
    /// This may return MSBuild instances that are not loadable by the BuildHost process.
    /// </remarks>
    MSBuildLocation? FindBestMSBuild(string projectOrSolutionFilePath);

    /// <summary>
    /// Determines whether there is a MSBuild instance that is loadable by the BuildHost process.
    /// </summary>
    /// <remarks>
    /// This may return true even if the project or solution require a newer version of MSBuild.
    /// </remarks>
    bool HasUsableMSBuild(string projectOrSolutionFilePath);

    /// <summary>
    /// Called once on a new process to configure some global state. This is used for these rather than passing through command line strings, since these contain data that might
    /// contain paths (which can have escaping issues) or could be quite large (which could run into length limits).
    /// </summary>
    void ConfigureGlobalState(Dictionary<string, string> globalProperties, string? binlogPath);

    Task<int> LoadProjectFileAsync(string projectFilePath, string languageName, CancellationToken cancellationToken);

    /// <summary>
    /// Permits loading a project file which only exists in-memory, for example, for file-based program scenarios.
    /// </summary>
    /// <param name="projectFilePath">A path to a project file which may or may not exist on disk. Note that an extension that is known by MSBuild, such as .csproj or .vbproj, should be used here.</param>
    /// <param name="projectContent">The project file XML content.</param>
    int LoadProject(string projectFilePath, string projectContent, string languageName);

    Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken);
    Task ShutdownAsync();
}
