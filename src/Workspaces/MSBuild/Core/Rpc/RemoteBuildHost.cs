// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class RemoteBuildHost
{
    private readonly RpcClient _client;

    // This is always zero, as this will be the first object registered into the server for any given process
    private const int BuildHostTargetObject = 0;

    public RemoteBuildHost(RpcClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Finds the best MSBuild instance installed for loading the given project or solution.
    /// </summary>
    /// <remarks>
    /// This will return the best MSBuild instance regardless of whether it is loadable by the BuildHost process.
    /// </remarks>
    public Task<MSBuildLocation?> FindBestMSBuildAsync(string projectOrSolutionFilePath, CancellationToken cancellationToken)
        => _client.InvokeNullableAsync<MSBuildLocation>(BuildHostTargetObject, nameof(IBuildHost.FindBestMSBuild), parameters: [projectOrSolutionFilePath], cancellationToken);

    /// <summary>
    /// Determines whether there is a MSBuild instance that is loadable by the BuildHost process.
    /// </summary>
    /// <remarks>
    /// This may return true even if the project or solution require a newer version of MSBuild.
    /// </remarks>
    public Task<bool> HasUsableMSBuildAsync(string projectOrSolutionFilePath, CancellationToken cancellationToken)
        => _client.InvokeAsync<bool>(BuildHostTargetObject, nameof(IBuildHost.HasUsableMSBuild), parameters: [projectOrSolutionFilePath], cancellationToken);

    /// <inheritdoc cref="IBuildHost.ConfigureGlobalState(ImmutableArray{string}, ImmutableDictionary{string, string}, string?)"/>
    public Task ConfigureGlobalStateAsync(ImmutableArray<string> knownCommandLineParserLanguages, ImmutableDictionary<string, string> globalProperties, string? binlogPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(BuildHostTargetObject, nameof(IBuildHost.ConfigureGlobalState), parameters: [knownCommandLineParserLanguages, globalProperties, binlogPath], cancellationToken);

    public async Task<RemoteProjectFile> LoadProjectFileAsync(string projectFilePath, string languageName, CancellationToken cancellationToken)
    {
        var remoteProjectFileTargetObject = await _client.InvokeAsync<int>(BuildHostTargetObject, nameof(IBuildHost.LoadProjectFileAsync), parameters: [projectFilePath, languageName], cancellationToken).ConfigureAwait(false);

        return new RemoteProjectFile(_client, remoteProjectFileTargetObject);
    }

    /// <summary>
    /// Permits loading a project file which only exists in-memory, for example, for file-based program scenarios.
    /// </summary>
    /// <param name="projectFilePath">A path to a project file which may or may not exist on disk. Note that an extension that is known by MSBuild, such as .csproj or .vbproj, should be used here.</param>
    /// <param name="projectContent">The project file XML content.</param>
    public async Task<RemoteProjectFile> LoadProjectAsync(string projectFilePath, string projectContent, string languageName, CancellationToken cancellationToken)
    {
        var remoteProjectFileTargetObject = await _client.InvokeAsync<int>(BuildHostTargetObject, nameof(IBuildHost.LoadProject), parameters: [projectFilePath, projectContent, languageName], cancellationToken).ConfigureAwait(false);

        return new RemoteProjectFile(_client, remoteProjectFileTargetObject);
    }

    public Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken)
        => _client.InvokeNullableAsync<string>(BuildHostTargetObject, nameof(IBuildHost.TryGetProjectOutputPathAsync), parameters: [projectFilePath], cancellationToken);

    public Task ShutdownAsync(CancellationToken cancellationToken)
        => _client.InvokeAsync(BuildHostTargetObject, nameof(IBuildHost.ShutdownAsync), parameters: [], cancellationToken);
}
