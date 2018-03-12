// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a project file loaded from disk.
    /// </summary>
    internal sealed class ProjectFileInfo
    {
        public bool IsEmpty { get; }

        /// <summary>
        /// The language of this project.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// The path to the project file for this project.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The path to the output file this project generates.
        /// </summary>
        public string OutputFilePath { get; }

        /// <summary>
        /// The path to the reference assembly output file this project generates.
        /// </summary>
        public string OutputRefFilePath { get; }

        /// <summary>
        /// The target framework of this project.
        /// </summary>
        public string TargetFramework { get; }

        /// <summary>
        /// The command line args used to compile the project.
        /// </summary>
        public IReadOnlyList<string> CommandLineArgs { get; }

        /// <summary>
        /// The source documents.
        /// </summary>
        public IReadOnlyList<DocumentFileInfo> Documents { get; }

        /// <summary>
        /// The additional documents.
        /// </summary>
        public IReadOnlyList<DocumentFileInfo> AdditionalDocuments { get; }

        /// <summary>
        /// References to other projects.
        /// </summary>
        public IReadOnlyList<ProjectFileReference> ProjectReferences { get; }

        /// <summary>
        /// The error message produced when a failure occurred attempting to get the info. 
        /// If a failure occurred some or all of the information may be inaccurate or incomplete.
        /// </summary>
        public DiagnosticLog Log { get; }

        private ProjectFileInfo(
            bool isEmpty,
            string language,
            string filePath,
            string outputFilePath,
            string outputRefFilePath,
            string targetFramework,
            IEnumerable<string> commandLineArgs,
            IEnumerable<DocumentFileInfo> documents,
            IEnumerable<DocumentFileInfo> additionalDocuments,
            IEnumerable<ProjectFileReference> projectReferences,
            DiagnosticLog log)
        {
            Debug.Assert(filePath != null);

            this.IsEmpty = isEmpty;
            this.Language = language;
            this.FilePath = filePath;
            this.OutputFilePath = outputFilePath;
            this.OutputRefFilePath = outputRefFilePath;
            this.TargetFramework = targetFramework;
            this.CommandLineArgs = commandLineArgs.ToImmutableArrayOrEmpty();
            this.Documents = documents.ToImmutableReadOnlyListOrEmpty();
            this.AdditionalDocuments = additionalDocuments.ToImmutableArrayOrEmpty();
            this.ProjectReferences = projectReferences.ToImmutableReadOnlyListOrEmpty();
            this.Log = log;
        }

        public static ProjectFileInfo Create(
            string language,
            string filePath,
            string outputFilePath,
            string outputRefFilePath,
            string targetFramework,
            IEnumerable<string> commandLineArgs,
            IEnumerable<DocumentFileInfo> documents,
            IEnumerable<DocumentFileInfo> additionalDocuments,
            IEnumerable<ProjectFileReference> projectReferences,
            DiagnosticLog log)
            => new ProjectFileInfo(
                isEmpty: false,
                language,
                filePath,
                outputFilePath,
                outputRefFilePath,
                targetFramework,
                commandLineArgs,
                documents,
                additionalDocuments,
                projectReferences,
                log);

        public static ProjectFileInfo CreateEmpty(string language, string filePath, DiagnosticLog log)
            => new ProjectFileInfo(
                isEmpty: true,
                language,
                filePath,
                outputFilePath: null,
                outputRefFilePath: null,
                targetFramework: null,
                commandLineArgs: SpecializedCollections.EmptyEnumerable<string>(),
                documents: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                additionalDocuments: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                projectReferences: SpecializedCollections.EmptyEnumerable<ProjectFileReference>(),
                log);
    }
}
