// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild.Logging;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a project file loaded from disk.
    /// </summary>
    internal interface IProjectFile
    {
        /// <summary>
        /// The path to the project file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// The error message produced when a failure occurred attempting to access the project file. 
        /// If a failure occurred the project file info will be inaccessible.
        /// </summary>
        DiagnosticLog Log { get; }

        /// <summary>
        /// Gets project file information asynchronously. Note that this can produce multiple
        /// instances of <see cref="ProjectFileInfo"/> if the project is multi-targeted: one for
        /// each target framework.
        /// </summary>
        Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the corresponding extension for a source file of a given kind.
        /// </summary>
        string GetDocumentExtension(SourceCodeKind kind);

        /// <summary>
        /// Add a source document to a project file.
        /// </summary>
        void AddDocument(string filePath, string logicalPath = null);

        /// <summary>
        /// Remove a source document from a project file.
        /// </summary>
        void RemoveDocument(string filePath);

        /// <summary>
        ///  Add a metadata reference to a project file.
        /// </summary>
        void AddMetadataReference(MetadataReference reference, AssemblyIdentity identity);

        /// <summary>
        /// Remove a metadata reference from a project file.
        /// </summary>
        void RemoveMetadataReference(MetadataReference reference, AssemblyIdentity identity);

        /// <summary>
        /// Add a reference to another project to a project file.
        /// </summary>
        void AddProjectReference(string projectName, ProjectFileReference reference);

        /// <summary>
        /// Remove a reference to another project from a project file.
        /// </summary>
        void RemoveProjectReference(string projectName, string projectFilePath);

        /// <summary>
        /// Add an analyzer reference to the project file.
        /// </summary>
        void AddAnalyzerReference(AnalyzerReference reference);

        /// <summary>
        /// Remove an analyzer reference from the project file.
        /// </summary>
        void RemoveAnalyzerReference(AnalyzerReference reference);

        /// <summary>
        /// Save the current state of the project file to disk.
        /// </summary>
        void Save();
    }
}
