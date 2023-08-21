// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

internal sealed class BuildHost : IBuildHost
{
    private readonly JsonRpc _jsonRpc;
    private readonly ProjectFileLoaderRegistry _projectFileLoaderRegistry;
    private readonly ProjectBuildManager _buildManager;

    public BuildHost(JsonRpc jsonRpc, SolutionServices solutionServices)
    {
        _jsonRpc = jsonRpc;
        _projectFileLoaderRegistry = new ProjectFileLoaderRegistry(solutionServices, new DiagnosticReporter(new AdhocWorkspace()));
        _buildManager = new ProjectBuildManager(System.Collections.Immutable.ImmutableDictionary<string, string>.Empty);
        _buildManager.StartBatchBuild();
    }

    public Task<bool> IsProjectFileSupportedAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        return Task.FromResult(_projectFileLoaderRegistry.TryGetLoaderFromProjectPath(projectFilePath, DiagnosticReportingMode.Ignore, out var _));
    }

    public async Task<IRemoteProjectFile> LoadProjectFileAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(_projectFileLoaderRegistry.TryGetLoaderFromProjectPath(projectFilePath, out var projectLoader));
        return new RemoteProjectFile(await projectLoader.LoadProjectFileAsync(projectFilePath, _buildManager, cancellationToken).ConfigureAwait(false));
    }

    public void Shutdown()
    {
        _buildManager.EndBatchBuild();
        _jsonRpc.Dispose();
    }
}
