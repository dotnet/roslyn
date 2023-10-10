// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

/// <summary>
/// The RPC interface implemented by this host; called via JSON-RPC.
/// </summary>
internal interface IBuildHost
{
    /// <summary>
    /// Returns true if this build host was able to discover a usable MSBuild instance. This should be called before calling other methods.
    /// </summary>
    Task<bool> HasUsableMSBuildAsync(string projectOrSolutionFilePath, CancellationToken cancellationToken);

    Task<ImmutableArray<(string ProjectPath, string ProjectGuid)>> GetProjectsInSolutionAsync(string solutionFilePath, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether this project's is supported by this host.
    /// </summary>
    Task<bool> IsProjectFileSupportedAsync(string projectFilePath, CancellationToken cancellationToken);

    Task<IRemoteProjectFile> LoadProjectFileAsync(string projectFilePath, CancellationToken cancellationToken);

    Task ShutdownAsync();
}
