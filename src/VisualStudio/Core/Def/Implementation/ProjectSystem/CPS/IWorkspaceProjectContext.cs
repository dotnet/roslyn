// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Project context to initialize properties and items of a Workspace project created with <see cref="IWorkspaceProjectContextFactory.CreateProjectContext(string, string, string, Guid, object, string)"/>. 
    /// </summary>
    internal interface IWorkspaceProjectContext : IDisposable
    {
        // Project properties.
        string DisplayName { get; set; }
        string ProjectFilePath { get; set; }
        Guid Guid { get; set; }
        bool LastDesignTimeBuildSucceeded { get; set; }
        string BinOutputPath { get; set; }

        ProjectId Id { get; }

        // Options.
        void SetOptions(string commandLineForOptions);

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
        void EndBatch();

        void ReorderSourceFiles(IEnumerable<string> filePaths);

        /// <summary>
        /// Applies changes to symbols for documents after their file path has changed. The same policy
        /// is applied across all documents based on the <see cref="FilePathChangeSymbolChange"/>
        /// </summary>
        /// <param name="documentIdToOriginalPathMapping">Mapping of document ids to the original file path. Original path may be used to determine if symbol changes are applicable</param>
        /// <remarks>
        /// Note that calls to this may do expensive work to fix up symbol renaming. Symbol changes express a desire to change the symbol, but may not result in a symbol change if 
        /// deemed unnecessary. For example, if no types matched the file name before then a named type change will not be performed. 
        /// </remarks>
        void ApplyDocumentSymbolChanges(ImmutableDictionary<DocumentId, string> documentIdToOriginalPathMapping, FilePathChangeSymbolChange filePathChangeSymbolChange, CancellationToken cancellationToken);
    }

    [Flags]
    internal enum FilePathChangeSymbolChange
    {
        None = 0,
        Namespace = 1 << 0,
        NamedType = 1 << 1,
        All = Namespace | NamedType
    }

}
