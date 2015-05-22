// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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
        /// Gets the project file info asynchronously.
        /// </summary>
        IRemoteTask<ProjectFileInfo> GetProjectFileInfoAsync();

        /// <summary>
        /// Get the kind of source found in the specified file. 
        /// This is usually determined by the file name extension.
        /// </summary>
        SourceCodeKind GetSourceCodeKind(string documentFileName);

        /// <summary>
        /// Gets the corresponding extension for a source file of a given kind.
        /// </summary>
        string GetDocumentExtension(SourceCodeKind kind);

        /// <summary>
        /// Gets a specific project file property.
        /// </summary>
        string GetPropertyValue(string name);

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
        void AddMetadataReference(ProjectMetadataReference metadata, string assemblyDisplayName);

        /// <summary>
        /// Remove a metadata reference from a project file.
        /// </summary>
        void RemoveMetadataReference(ProjectMetadataReference metadata, string assemblyFullName);

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
        void AddAnalyzerReference(ProjectAnalyzerReference reference);

        /// <summary>
        /// Remove an analyzer reference from the project file.
        /// </summary>
        void RemoveAnalyzerReference(ProjectAnalyzerReference reference);

        /// <summary>
        /// Save the current state of the project file to disk.
        /// </summary>
        void Save();
    }
}
