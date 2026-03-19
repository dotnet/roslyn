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
internal interface IProjectFile
{
    ImmutableArray<DiagnosticLogItem> GetDiagnosticLogItems();
    Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken);
    void AddDocument(string filePath, string? logicalPath);
    void RemoveDocument(string filePath);
    void AddMetadataReference(string metadataReferenceIdentity, ImmutableArray<string> aliases, string? hintPath);
    void RemoveMetadataReference(string shortAssemblyName, string fullAssemblyName, string filePath);
    void AddProjectReference(string projectName, ProjectFileReference reference);
    void RemoveProjectReference(string projectName, string projectFilePath);
    void AddAnalyzerReference(string fullPath);
    void RemoveAnalyzerReference(string fullPath);
    void Save();
}
