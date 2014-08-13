// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a project file loaded from disk.
    /// </summary>
    internal sealed class ProjectFileInfo
    {
        /// <summary>
        /// The project's individual GUID
        /// </summary>
        public Guid Guid { get; private set; }

        /// <summary>
        /// The path to the output file this project generates.
        /// </summary>
        public string OutputFilePath { get; private set; }

        /// <summary>
        /// The assembly name of the output.
        /// </summary>
        public string AssemblyName { get; private set; }

        /// <summary>
        /// The compilation options for this project.
        /// </summary>
        public CompilationOptions CompilationOptions { get; private set; }

        /// <summary>
        /// The parse options for this project.
        /// </summary>
        public ParseOptions ParseOptions { get; private set; }

        /// <summary>
        /// The source documents.
        /// </summary>
        public IReadOnlyList<DocumentFileInfo> Documents { get; private set; }

        /// <summary>
        /// The additional documents.
        /// </summary>
        public IReadOnlyList<DocumentFileInfo> AdditionalDocuments { get; private set; }

        /// <summary>
        /// References to other projects.
        /// </summary>
        public IReadOnlyList<ProjectFileReference> ProjectReferences { get; private set; }

        /// <summary>
        /// References to other metadata files; libraries and executables.
        /// </summary>
        public IReadOnlyList<MetadataReference> MetadataReferences { get; private set; }

        /// <summary>
        /// References to analyzer assembly files; contains diagnostic analyzers.
        /// </summary>
        public IReadOnlyList<AnalyzerReference> AnalyzerReferences { get; private set; }

        public ProjectFileInfo(
            Guid guid,
            string outputPath,
            string assemblyName,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            IEnumerable<DocumentFileInfo> documents,
            IEnumerable<DocumentFileInfo> additionalDocuments,
            IEnumerable<ProjectFileReference> projectReferences,
            IEnumerable<MetadataReference> metadataReferences,
            IEnumerable<AnalyzerReference> analyzerReferences)
        {
            this.Guid = guid;
            this.OutputFilePath = outputPath;
            this.AssemblyName = assemblyName;
            this.CompilationOptions = compilationOptions;
            this.ParseOptions = parseOptions;
            this.Documents = documents.ToImmutableReadOnlyListOrEmpty();
            this.AdditionalDocuments = additionalDocuments.ToImmutableReadOnlyListOrEmpty();
            this.ProjectReferences = projectReferences.ToImmutableReadOnlyListOrEmpty();
            this.MetadataReferences = metadataReferences.ToImmutableReadOnlyListOrEmpty();
            this.AnalyzerReferences = analyzerReferences.ToImmutableReadOnlyListOrEmpty();
        }
    }
}