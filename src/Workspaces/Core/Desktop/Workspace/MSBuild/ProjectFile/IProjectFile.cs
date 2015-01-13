// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
        /// The unique GUID associated with the project.
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// Gets the project file info asynchronously.
        /// </summary>
        Task<ProjectFileInfo> GetProjectFileInfoAsync(CancellationToken cancellationToken);

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
        /// Save the current state of the project file to disk.
        /// </summary>
        void Save();
    }
}
