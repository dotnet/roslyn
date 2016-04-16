// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that represents all the arguments necessary to create a new project instance.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class ProjectInfo
    {
        /// <summary>
        /// The unique Id of the project.
        /// </summary>
        public ProjectId Id { get; }

        /// <summary>
        /// The version of the project.
        /// </summary>
        public VersionStamp Version { get; }

        /// <summary>
        /// The name of the project. This may differ from the project's filename.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The name of the assembly that this project will create, without file extension.
        /// </summary>,
        public string AssemblyName { get; }

        /// <summary>
        /// The language of the project.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// The path to the project file or null if there is no project file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The path to the output file (module or assembly).
        /// </summary>
        public string OutputFilePath { get; }

        /// <summary>
        /// The initial compilation options for the project, or null if the default options should be used.
        /// </summary>
        public CompilationOptions CompilationOptions { get; }

        /// <summary>
        /// The initial parse options for the source code documents in this project, or null if the default options should be used.
        /// </summary>
        public ParseOptions ParseOptions { get; }

        /// <summary>
        /// The list of source documents initially associated with the project.
        /// </summary>
        public IReadOnlyList<DocumentInfo> Documents { get; }

        /// <summary>
        /// The project references initially defined for the project.
        /// </summary>
        public IReadOnlyList<ProjectReference> ProjectReferences { get; }

        /// <summary>
        /// The metadata references initially defined for the project.
        /// </summary>
        public IReadOnlyList<MetadataReference> MetadataReferences { get; }

        /// <summary>
        /// The analyzers initially associated with this project.
        /// </summary>
        public IReadOnlyList<AnalyzerReference> AnalyzerReferences { get; }

        /// <summary>
        /// The list of non-source documents associated with this project.
        /// </summary>
        public IReadOnlyList<DocumentInfo> AdditionalDocuments { get; }

        /// <summary>
        /// True if this is a submission project for interactive sessions.
        /// </summary>
        public bool IsSubmission { get; }

        /// <summary>
        /// Type of the host object.
        /// </summary>
        public Type HostObjectType { get; }

        /// <summary>
        /// True if project information is complete. In some workspace hosts, it is possible
        /// a project only has partial information. In such cases, a project might not have all
        /// information on its files or references.
        /// </summary>
        internal bool HasAllInformation { get; }

        private ProjectInfo(
            ProjectId id,
            VersionStamp version,
            string name,
            string assemblyName,
            string language,
            string filePath,
            string outputFilePath,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            IEnumerable<DocumentInfo> documents,
            IEnumerable<ProjectReference> projectReferences,
            IEnumerable<MetadataReference> metadataReferences,
            IEnumerable<AnalyzerReference> analyzerReferences,
            IEnumerable<DocumentInfo> additionalDocuments,
            bool isSubmission,
            Type hostObjectType,
            bool hasAllInformation)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            this.Id = id;
            this.Version = version;
            this.Name = name;
            this.AssemblyName = assemblyName;
            this.Language = language;
            this.FilePath = filePath;
            this.OutputFilePath = outputFilePath;
            this.CompilationOptions = compilationOptions;
            this.ParseOptions = parseOptions;
            this.Documents = documents.ToImmutableReadOnlyListOrEmpty();
            this.ProjectReferences = projectReferences.ToImmutableReadOnlyListOrEmpty();
            this.MetadataReferences = metadataReferences.ToImmutableReadOnlyListOrEmpty();
            this.AnalyzerReferences = analyzerReferences.ToImmutableReadOnlyListOrEmpty();
            this.AdditionalDocuments = additionalDocuments.ToImmutableReadOnlyListOrEmpty();
            this.IsSubmission = isSubmission;
            this.HostObjectType = hostObjectType;
            this.HasAllInformation = hasAllInformation;
        }

        /// <summary>
        /// Create a new instance of a ProjectInfo.
        /// </summary>
        internal static ProjectInfo Create(
            ProjectId id,
            VersionStamp version,
            string name,
            string assemblyName,
            string language,
            string filePath,
            string outputFilePath,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            IEnumerable<DocumentInfo> documents,
            IEnumerable<ProjectReference> projectReferences,
            IEnumerable<MetadataReference> metadataReferences,
            IEnumerable<AnalyzerReference> analyzerReferences,
            IEnumerable<DocumentInfo> additionalDocuments,
            bool isSubmission,
            Type hostObjectType,
            bool hasAllInformation)
        {
            return new ProjectInfo(
                id,
                version,
                name,
                assemblyName,
                language,
                filePath,
                outputFilePath,
                compilationOptions,
                parseOptions,
                documents,
                projectReferences,
                metadataReferences,
                analyzerReferences,
                additionalDocuments,
                isSubmission,
                hostObjectType,
                hasAllInformation);
        }

        /// <summary>
        /// Create a new instance of a ProjectInfo.
        /// </summary>
        public static ProjectInfo Create(
            ProjectId id,
            VersionStamp version,
            string name,
            string assemblyName,
            string language,
            string filePath = null,
            string outputFilePath = null,
            CompilationOptions compilationOptions = null,
            ParseOptions parseOptions = null,
            IEnumerable<DocumentInfo> documents = null,
            IEnumerable<ProjectReference> projectReferences = null,
            IEnumerable<MetadataReference> metadataReferences = null,
            IEnumerable<AnalyzerReference> analyzerReferences = null,
            IEnumerable<DocumentInfo> additionalDocuments = null,
            bool isSubmission = false,
            Type hostObjectType = null)
        {
            return Create(
                id, version, name, assemblyName, language,
                filePath, outputFilePath, compilationOptions, parseOptions,
                documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments,
                isSubmission, hostObjectType, hasAllInformation: true);
        }

        private ProjectInfo With(
            ProjectId id = null,
            VersionStamp? version = default(VersionStamp?),
            string name = null,
            string assemblyName = null,
            string language = null,
            Optional<string> filePath = default(Optional<string>),
            Optional<string> outputPath = default(Optional<string>),
            CompilationOptions compilationOptions = null,
            ParseOptions parseOptions = null,
            IEnumerable<DocumentInfo> documents = null,
            IEnumerable<ProjectReference> projectReferences = null,
            IEnumerable<MetadataReference> metadataReferences = null,
            IEnumerable<AnalyzerReference> analyzerReferences = null,
            IEnumerable<DocumentInfo> additionalDocuments = null,
            Optional<bool> isSubmission = default(Optional<bool>),
            Optional<Type> hostObjectType = default(Optional<Type>),
            Optional<bool> hasAllInformation = default(Optional<bool>))
        {
            var newId = id ?? this.Id;
            var newVersion = version.HasValue ? version.Value : this.Version;
            var newName = name ?? this.Name;
            var newAssemblyName = assemblyName ?? this.AssemblyName;
            var newLanguage = language ?? this.Language;
            var newFilepath = filePath.HasValue ? filePath.Value : this.FilePath;
            var newOutputPath = outputPath.HasValue ? outputPath.Value : this.OutputFilePath;
            var newCompilationOptions = compilationOptions ?? this.CompilationOptions;
            var newParseOptions = parseOptions ?? this.ParseOptions;
            var newDocuments = documents ?? this.Documents;
            var newProjectReferences = projectReferences ?? this.ProjectReferences;
            var newMetadataReferences = metadataReferences ?? this.MetadataReferences;
            var newAnalyzerReferences = analyzerReferences ?? this.AnalyzerReferences;
            var newAdditionalDocuments = additionalDocuments ?? this.AdditionalDocuments;
            var newIsSubmission = isSubmission.HasValue ? isSubmission.Value : this.IsSubmission;
            var newHostObjectType = hostObjectType.HasValue ? hostObjectType.Value : this.HostObjectType;
            var newHasAllInformation = hasAllInformation.HasValue ? hasAllInformation.Value : this.HasAllInformation;

            if (newId == this.Id &&
                newVersion == this.Version &&
                newName == this.Name &&
                newAssemblyName == this.AssemblyName &&
                newLanguage == this.Language &&
                newFilepath == this.FilePath &&
                newOutputPath == this.OutputFilePath &&
                newCompilationOptions == this.CompilationOptions &&
                newParseOptions == this.ParseOptions &&
                newDocuments == this.Documents &&
                newProjectReferences == this.ProjectReferences &&
                newMetadataReferences == this.MetadataReferences &&
                newAnalyzerReferences == this.AnalyzerReferences &&
                newAdditionalDocuments == this.AdditionalDocuments &&
                newIsSubmission == this.IsSubmission &&
                newHostObjectType == this.HostObjectType &&
                newHasAllInformation == this.HasAllInformation)
            {
                return this;
            }

            return new ProjectInfo(
                    newId,
                    newVersion,
                    newName,
                    newAssemblyName,
                    newLanguage,
                    newFilepath,
                    newOutputPath,
                    newCompilationOptions,
                    newParseOptions,
                    newDocuments,
                    newProjectReferences,
                    newMetadataReferences,
                    newAnalyzerReferences,
                    newAdditionalDocuments,
                    newIsSubmission,
                    newHostObjectType,
                    newHasAllInformation);
        }

        public ProjectInfo WithDocuments(IEnumerable<DocumentInfo> documents)
        {
            return this.With(documents: documents.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithAdditionalDocuments(IEnumerable<DocumentInfo> additionalDocuments)
        {
            return this.With(additionalDocuments: additionalDocuments.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithVersion(VersionStamp version)
        {
            return this.With(version: version);
        }

        public ProjectInfo WithName(string name)
        {
            return this.With(name: name);
        }

        public ProjectInfo WithFilePath(string filePath)
        {
            return this.With(filePath: filePath);
        }

        public ProjectInfo WithAssemblyName(string assemblyName)
        {
            return this.With(assemblyName: assemblyName);
        }

        public ProjectInfo WithOutputFilePath(string outputFilePath)
        {
            return this.With(outputPath: outputFilePath);
        }

        public ProjectInfo WithCompilationOptions(CompilationOptions compilationOptions)
        {
            return this.With(compilationOptions: compilationOptions);
        }

        public ProjectInfo WithParseOptions(ParseOptions parseOptions)
        {
            return this.With(parseOptions: parseOptions);
        }

        public ProjectInfo WithProjectReferences(IEnumerable<ProjectReference> projectReferences)
        {
            return this.With(projectReferences: projectReferences.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        {
            return this.With(metadataReferences: metadataReferences.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            return this.With(analyzerReferences: analyzerReferences.ToImmutableReadOnlyListOrEmpty());
        }

        internal ProjectInfo WithHasAllInformation(bool hasAllInformation)
        {
            return this.With(hasAllInformation: hasAllInformation);
        }

        internal string GetDebuggerDisplay()
        {
            return nameof(ProjectInfo) + " " + Name + (!string.IsNullOrWhiteSpace(FilePath) ? " " + FilePath : "");
        }
    }
}
