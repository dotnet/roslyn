// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// RPC methods.
/// </summary>
internal interface IProjectFile : IDisposable
{
    /// <summary>
    /// Gets the diagnostics reported while evaluating the project file, which apply to every target. Diagnostics specific to a
    /// target are on that target's <see cref="ProjectFileInfo.Diagnostics"/>.
    /// </summary>
    DiagnosticLogItem[] GetEvaluationDiagnosticLogItems();
    Task<ProjectFileInfo[]> GetProjectFileInfosAsync(CancellationToken cancellationToken);
    void AddDocument(string filePath, string? logicalPath);
    void RemoveDocument(string filePath);
    void AddMetadataReference(string metadataReferenceIdentity, string[] aliases, string? hintPath);
    void RemoveMetadataReference(string shortAssemblyName, string fullAssemblyName, string filePath);
    void AddProjectReference(string projectName, ProjectFileReference reference);
    void RemoveProjectReference(string projectName, string projectFilePath);
    void AddAnalyzerReference(string fullPath);
    void RemoveAnalyzerReference(string fullPath);
    void Save();
}
