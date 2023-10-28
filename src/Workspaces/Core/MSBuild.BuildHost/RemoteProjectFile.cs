// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Logging;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

internal class RemoteProjectFile : IRemoteProjectFile
{
    private readonly ProjectFile _projectFile;

    public RemoteProjectFile(ProjectFile projectFile)
    {
        _projectFile = projectFile;
    }

    public void Dispose()
    {
    }

    public Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken)
        => _projectFile.GetProjectFileInfosAsync(cancellationToken);

    public Task<ImmutableArray<DiagnosticLogItem>> GetDiagnosticLogItemsAsync(CancellationToken cancellationToken)
        => Task.FromResult(_projectFile.Log.ToImmutableArray());

    public Task<string> GetDocumentExtensionAsync(SourceCodeKind sourceCodeKind, CancellationToken cancellationToken)
        => Task.FromResult(_projectFile.GetDocumentExtension(sourceCodeKind));

    public Task AddDocumentAsync(string filePath, string? logicalPath, CancellationToken cancellationToken)
    {
        _projectFile.AddDocument(filePath, logicalPath);
        return Task.CompletedTask;
    }

    public Task RemoveDocumentAsync(string filePath, CancellationToken cancellationToken)
    {
        _projectFile.RemoveDocument(filePath);
        return Task.CompletedTask;
    }

    public Task AddMetadataReferenceAsync(string metadataReferenceIdentity, MetadataReferenceProperties properties, string? hintPath, CancellationToken cancellationToken)
    {
        _projectFile.AddMetadataReference(metadataReferenceIdentity, properties, hintPath);
        return Task.CompletedTask;
    }

    public Task RemoveMetadataReferenceAsync(string shortAssemblyName, string fullAssemblyName, string filePath, CancellationToken cancellationToken)
    {
        _projectFile.RemoveMetadataReference(shortAssemblyName, fullAssemblyName, filePath);
        return Task.CompletedTask;
    }

    public Task AddProjectReferenceAsync(string projectName, ProjectFileReference reference, CancellationToken cancellationToken)
    {
        _projectFile.AddProjectReference(projectName, reference);
        return Task.CompletedTask;
    }

    public Task RemoveProjectReferenceAsync(string projectName, string projectFilePath, CancellationToken cancellationToken)
    {
        _projectFile.RemoveProjectReference(projectName, projectFilePath);
        return Task.CompletedTask;
    }

    public Task AddAnalyzerReferenceAsync(string fullPath, CancellationToken cancellationToken)
    {
        _projectFile.AddAnalyzerReference(fullPath);
        return Task.CompletedTask;
    }

    public Task RemoveAnalyzerReferenceAsync(string fullPath, CancellationToken cancellationToken)
    {
        _projectFile.RemoveAnalyzerReference(fullPath);
        return Task.CompletedTask;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        _projectFile.Save();
        return Task.CompletedTask;
    }

}
