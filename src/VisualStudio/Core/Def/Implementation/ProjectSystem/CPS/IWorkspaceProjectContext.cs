// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Project context to initialize properties and items of a Workspace project created with <see
    /// cref="IWorkspaceProjectContextFactory.CreateProjectContextAsync"/>. 
    /// </summary>
    /// <remarks>
    /// <see cref="IDisposable.Dispose"/> is safe to call on instances of this type on any thread.
    /// </remarks>
    internal interface IWorkspaceProjectContext : IDisposable
    {
        // Project properties.
        string DisplayName { get; set; }
        string ProjectFilePath { get; set; }
        Guid Guid { get; set; }
        bool LastDesignTimeBuildSucceeded { get; set; }
        string BinOutputPath { get; set; }

        /// <summary>
        /// When this project is one of a multi-targeting group of projects, this value indicates whether or not this
        /// particular project is the primary one.  The primary project is responsible for certain things when reporting
        /// data from Roslyn's individual projects back to the project system itself.  For example, designer attributes
        /// are only associated with the primary project, and should be skipped for other projects.
        /// </summary>
        bool IsPrimary { get; set; }

        ProjectId Id { get; }

        // Options.

        [Obsolete("To avoid contributing to the large object heap, use SetOptions(ImmutableArray<string>). This API will be removed in the future.")]
        void SetOptions(string commandLineForOptions);
        void SetOptions(ImmutableArray<string> arguments);

        // Other project properties.
        void SetProperty(string name, string value);

        // References.
        void AddMetadataReference(string referencePath, MetadataReferenceProperties properties);
        void RemoveMetadataReference(string referencePath);
        void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties);
        void RemoveProjectReference(IWorkspaceProjectContext project);
        void AddAnalyzerReference(string referencePath);
        void RemoveAnalyzerReference(string referencePath);

        // Files.
        void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular);
        void RemoveSourceFile(string filePath);
        void AddAdditionalFile(string filePath, bool isInCurrentContext = true);
        void RemoveAdditionalFile(string filePath);
        void AddDynamicFile(string filePath, IEnumerable<string> folderNames = null);
        void RemoveDynamicFile(string filePath);

        /// <summary>
        /// Adds a file (like a .editorconfig) used to configure analyzers.
        /// </summary>
        void AddAnalyzerConfigFile(string filePath);

        /// <summary>
        /// Removes a file (like a .editorconfig) used to configure analyzers.
        /// </summary>
        void RemoveAnalyzerConfigFile(string filePath);

        void SetRuleSetFile(string filePath);

        void StartBatch();
        [Obsolete($"Use {nameof(EndBatchAsync)}.")]
        void EndBatch();
        ValueTask EndBatchAsync();

        void ReorderSourceFiles(IEnumerable<string> filePaths);
    }
}
