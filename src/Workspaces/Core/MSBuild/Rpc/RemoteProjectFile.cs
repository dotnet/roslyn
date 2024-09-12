// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Logging;

namespace Microsoft.CodeAnalysis.MSBuild.Rpc;

internal sealed class RemoteProjectFile
{
    private readonly RpcClient _client;
    private readonly int _remoteProjectFileTargetObject;

    public RemoteProjectFile(RpcClient client, int remoteProjectFileTargetObject)
    {
        _client = client;
        _remoteProjectFileTargetObject = remoteProjectFileTargetObject;
    }

    public Task<ImmutableArray<DiagnosticLogItem>> GetDiagnosticLogItemsAsync(CancellationToken cancellationToken)
        => _client.InvokeAsync<ImmutableArray<DiagnosticLogItem>>(_remoteProjectFileTargetObject, nameof(ProjectFile.GetDiagnosticLogItems), parameters: [], cancellationToken);

    public Task<string> GetDocumentExtensionAsync(SourceCodeKind sourceCodeKind, CancellationToken cancellationToken)
        => _client.InvokeAsync<string>(_remoteProjectFileTargetObject, nameof(ProjectFile.GetDocumentExtension), parameters: [sourceCodeKind], cancellationToken);

    public Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken)
        => _client.InvokeAsync<ImmutableArray<ProjectFileInfo>>(_remoteProjectFileTargetObject, nameof(ProjectFile.GetProjectFileInfosAsync), parameters: [], cancellationToken);

    public Task AddDocumentAsync(string filePath, string? logicalPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.AddDocument), parameters: [filePath, logicalPath], cancellationToken);

    public Task RemoveDocumentAsync(string filePath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.RemoveDocument), parameters: [filePath], cancellationToken);

    public Task AddMetadataReferenceAsync(string metadataReferenceIdentity, MetadataReferenceProperties properties, string? hintPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.AddMetadataReference), parameters: [metadataReferenceIdentity, properties, hintPath], cancellationToken);

    public Task RemoveMetadataReferenceAsync(string shortAssemblyName, string fullAssemblyName, string filePath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.RemoveMetadataReference), parameters: [shortAssemblyName, fullAssemblyName, filePath], cancellationToken);

    public Task AddProjectReferenceAsync(string projectName, ProjectFileReference projectFileReference, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.AddProjectReference), parameters: [projectName, projectFileReference], cancellationToken);

    public Task RemoveProjectReferenceAsync(string projectName, string projectFilePath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.RemoveProjectReference), parameters: [projectName, projectFilePath], cancellationToken);

    public Task AddAnalyzerReferenceAsync(string fullPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.AddAnalyzerReference), parameters: [fullPath], cancellationToken);

    public Task RemoveAnalyzerReferenceAsync(string fullPath, CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.RemoveAnalyzerReference), parameters: [fullPath], cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken)
        => _client.InvokeAsync(_remoteProjectFileTargetObject, nameof(ProjectFile.Save), parameters: [], cancellationToken);
}
