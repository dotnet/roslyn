// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// RPC methods.
/// </summary>
internal interface IBuildHost
{
    bool HasUsableMSBuild(string projectOrSolutionFilePath);
    ImmutableArray<(string ProjectPath, string ProjectGuid)> GetProjectsInSolution(string solutionFilePath);
    Task<int> LoadProjectFileAsync(string projectFilePath, string languageName, CancellationToken cancellationToken);
    Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken);
    Task ShutdownAsync();
}
