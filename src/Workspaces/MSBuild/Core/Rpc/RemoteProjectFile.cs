// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class RemoteProjectFile
{
    private readonly RpcClient _client;
    private readonly int _remoteProjectFileTargetObject;

    public RemoteProjectFile(RpcClient client, int remoteProjectFileTargetObject)
    {
        _client = client;
        _remoteProjectFileTargetObject = remoteProjectFileTargetObject;
    }

    public async Task<ImmutableArray<DiagnosticLogItem>> GetDiagnosticLogItemsAsync(CancellationToken cancellationToken)
    {
        var diagnostics = await _client.InvokeAsync<DiagnosticLogItem[]>(_remoteProjectFileTargetObject, nameof(IProjectFile.GetDiagnosticLogItems), parameters: [], cancellationToken).ConfigureAwait(false);
        return diagnostics.ToImmutableArray();
    }

    public async Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken)
    {
        var projectFileInfos = await _client.InvokeAsync<ProjectFileInfo[]>(_remoteProjectFileTargetObject, nameof(IProjectFile.GetProjectFileInfosAsync), parameters: [], cancellationToken).ConfigureAwait(false);
        return projectFileInfos.ToImmutableArray();
    }

    public Task AddDocumentAsync(string filePath, string? logicalPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.AddDocument), parameters: [filePath, logicalPath], cancellationToken);

    public Task RemoveDocumentAsync(string filePath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.RemoveDocument), parameters: [filePath], cancellationToken);

    public Task AddMetadataReferenceAsync(string metadataReferenceIdentity, ImmutableArray<string> aliases, string? hintPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.AddMetadataReference), parameters: [metadataReferenceIdentity, aliases.ToArray(), hintPath], cancellationToken);

    public Task RemoveMetadataReferenceAsync(string shortAssemblyName, string fullAssemblyName, string filePath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.RemoveMetadataReference), parameters: [shortAssemblyName, fullAssemblyName, filePath], cancellationToken);

    public Task AddProjectReferenceAsync(string projectName, ProjectFileReference projectFileReference, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.AddProjectReference), parameters: [projectName, projectFileReference], cancellationToken);

    public Task RemoveProjectReferenceAsync(string projectName, string projectFilePath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.RemoveProjectReference), parameters: [projectName, projectFilePath], cancellationToken);

    public Task AddAnalyzerReferenceAsync(string fullPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.AddAnalyzerReference), parameters: [fullPath], cancellationToken);

    public Task RemoveAnalyzerReferenceAsync(string fullPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.RemoveAnalyzerReference), parameters: [fullPath], cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(IProjectFile.Save), parameters: [], cancellationToken);
}
