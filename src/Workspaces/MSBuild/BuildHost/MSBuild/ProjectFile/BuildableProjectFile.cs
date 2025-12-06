// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildableProjectFile(ProjectFile project, ProjectBuildManager buildManager, DiagnosticLog log) : IProjectFile
{
    public string FilePath
        => project.FilePath;

    public string Language
        => project.Language;

    public ImmutableArray<DiagnosticLogItem> GetDiagnosticLogItems()
        => [.. log];

    public Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken)
        => project.GetProjectFileInfosAsync(buildManager, log, cancellationToken);

    public void AddDocument(string filePath, string? logicalPath = null)
        => project.AddDocument(filePath, logicalPath);

    public void RemoveDocument(string filePath)
        => project.RemoveDocument(filePath);

    public void AddMetadataReference(string metadataReferenceIdentity, ImmutableArray<string> aliases, string? hintPath)
        => project.AddMetadataReference(metadataReferenceIdentity, aliases, hintPath);

    public void RemoveMetadataReference(string shortAssemblyName, string fullAssemblyName, string filePath)
        => project.RemoveMetadataReference(shortAssemblyName, fullAssemblyName, filePath);

    public void AddProjectReference(string projectName, ProjectFileReference reference)
        => project.AddProjectReference(projectName, reference);

    public void RemoveProjectReference(string projectName, string projectFilePath)
        => project.RemoveProjectReference(projectName, projectFilePath);

    public void AddAnalyzerReference(string fullPath)
        => project.AddAnalyzerReference(fullPath);

    public void RemoveAnalyzerReference(string fullPath)
        => project.RemoveAnalyzerReference(fullPath);

    public void Save()
        => project.Save();
}
