// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

namespace Microsoft.CodeAnalysis.MSBuild.Rpc;

internal sealed class RemoteBuildHost
{
    private readonly RpcClient _client;

    // This is always zero, as this will be the first object registered into the server for any given process
    private const int BuildHostTargetObject = 0;

    public RemoteBuildHost(RpcClient client)
    {
        _client = client;
    }

    public Task<bool> HasUsableMSBuildAsync(string projectOrSolutionFilePath, CancellationToken cancellationToken)
        => _client.InvokeAsync<bool>(BuildHostTargetObject, nameof(BuildHost.HasUsableMSBuild), parameters: [projectOrSolutionFilePath], cancellationToken);

    public Task<ImmutableArray<(string ProjectPath, string ProjectGuid)>> GetProjectsInSolutionAsync(string solutionFilePath, CancellationToken cancellationToken)
        => _client.InvokeAsync<ImmutableArray<(string ProjectPath, string ProjectGuid)>>(BuildHostTargetObject, nameof(BuildHost.GetProjectsInSolution), parameters: [solutionFilePath], cancellationToken);

    public async Task<RemoteProjectFile> LoadProjectFileAsync(string projectFilePath, string languageName, CancellationToken cancellationToken)
    {
        var remoteProjectFileTargetObject = await _client.InvokeAsync<int>(BuildHostTargetObject, nameof(BuildHost.LoadProjectFileAsync), parameters: [projectFilePath, languageName], cancellationToken).ConfigureAwait(false);

        return new RemoteProjectFile(_client, remoteProjectFileTargetObject);
    }

    public Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken)
        => _client.InvokeNullableAsync<string>(BuildHostTargetObject, nameof(BuildHost.TryGetProjectOutputPathAsync), parameters: [projectFilePath], cancellationToken);

    public Task ShutdownAsync(CancellationToken cancellationToken)
        => _client.InvokeAsync(BuildHostTargetObject, nameof(BuildHost.ShutdownAsync), parameters: [], cancellationToken);

}
