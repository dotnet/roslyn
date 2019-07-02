// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that represents all the arguments necessary to create a new project instance.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class ProjectInfo
    {
        internal ProjectAttributes Attributes { get; }

        /// <summary>
        /// The unique Id of the project.
        /// </summary>
        public ProjectId Id => Attributes.Id;

        /// <summary>
        /// The version of the project.
        /// </summary>
        public VersionStamp Version => Attributes.Version;

        /// <summary>
        /// The name of the project. This may differ from the project's filename.
        /// </summary>
        public string Name => Attributes.Name;

        /// <summary>
        /// The name of the assembly that this project will create, without file extension.
        /// </summary>,
        public string AssemblyName => Attributes.AssemblyName;

        /// <summary>
        /// The language of the project.
        /// </summary>
        public string Language => Attributes.Language;

        /// <summary>
        /// The path to the project file or null if there is no project file.
        /// </summary>
        public string FilePath => Attributes.FilePath;

        /// <summary>
        /// The path to the output file (module or assembly).
        /// </summary>
        public string OutputFilePath => Attributes.OutputFilePath;

        /// <summary>
        /// The path to the reference assembly output file.
        /// </summary>
        public string OutputRefFilePath => Attributes.OutputRefFilePath;

        /// <summary>
        /// The default namespace of the project ("" if not defined, which means global namespace),
        /// or null if it is unknown or not applicable. 
        /// </summary>
        /// <remarks>
        /// Right now VB doesn't have the concept of "default namespace", but we conjure one in workspace 
        /// by assigning the value of the project's root namespace to it. So various features can choose to 
        /// use it for their own purpose.
        /// In the future, we might consider officially exposing "default namespace" for VB project 
        /// (e.g. through a "defaultnamespace" msbuild property)
        /// </remarks>
        internal string DefaultNamespace => Attributes.DefaultNamespace;

        /// <summary>
        /// True if this is a submission project for interactive sessions.
        /// </summary>
        public bool IsSubmission => Attributes.IsSubmission;

        /// <summary>
        /// True if project information is complete. In some workspace hosts, it is possible
        /// a project only has partial information. In such cases, a project might not have all
        /// information on its files or references.
        /// </summary>
        internal bool HasAllInformation => Attributes.HasAllInformation;

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
        /// The list of analyzerconfig documents associated with this project.
        /// </summary>
        public IReadOnlyList<DocumentInfo> AnalyzerConfigDocuments { get; }

        /// <summary>
        /// Type of the host object.
        /// </summary>
        public Type HostObjectType { get; }

        private ProjectInfo(
            ProjectAttributes attributes,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            IEnumerable<DocumentInfo> documents,
            IEnumerable<ProjectReference> projectReferences,
            IEnumerable<MetadataReference> metadataReferences,
            IEnumerable<AnalyzerReference> analyzerReferences,
            IEnumerable<DocumentInfo> additionalDocuments,
            IEnumerable<DocumentInfo> analyzerConfigDocuments,
            Type hostObjectType)
        {
            Attributes = attributes;
            CompilationOptions = compilationOptions;
            ParseOptions = parseOptions;
            Documents = documents.ToImmutableReadOnlyListOrEmpty();
            ProjectReferences = projectReferences.ToImmutableReadOnlyListOrEmpty();
            MetadataReferences = metadataReferences.ToImmutableReadOnlyListOrEmpty();
            AnalyzerReferences = analyzerReferences.ToImmutableReadOnlyListOrEmpty();
            AdditionalDocuments = additionalDocuments.ToImmutableReadOnlyListOrEmpty();
            AnalyzerConfigDocuments = analyzerConfigDocuments.ToImmutableReadOnlyListOrEmpty();
            HostObjectType = hostObjectType;
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
            string outputRefFilePath,
            string defaultNamespace,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            IEnumerable<DocumentInfo> documents,
            IEnumerable<ProjectReference> projectReferences,
            IEnumerable<MetadataReference> metadataReferences,
            IEnumerable<AnalyzerReference> analyzerReferences,
            IEnumerable<DocumentInfo> additionalDocuments,
            IEnumerable<DocumentInfo> analyzerConfigDocuments,
            bool isSubmission,
            Type hostObjectType,
            bool hasAllInformation)
        {
            return new ProjectInfo(
                new ProjectAttributes(
                    id,
                    version,
                    name,
                    assemblyName,
                    language,
                    filePath,
                    outputFilePath,
                    outputRefFilePath,
                    defaultNamespace,
                    isSubmission,
                    hasAllInformation),
                compilationOptions,
                parseOptions,
                documents,
                projectReferences,
                metadataReferences,
                analyzerReferences,
                additionalDocuments,
                analyzerConfigDocuments,
                hostObjectType);
        }

        // 2.7.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        /// <summary>
        /// Create a new instance of a ProjectInfo.
        /// </summary>
        public static ProjectInfo Create(
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
            Type hostObjectType)
        {
            return Create(
                id, version, name, assemblyName, language,
                filePath, outputFilePath, outputRefFilePath: null, defaultNamespace: null, compilationOptions, parseOptions,
                documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments, analyzerConfigDocuments: null,
                isSubmission, hostObjectType, hasAllInformation: true);
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
            Type hostObjectType = null,
            string outputRefFilePath = null)
        {
            return Create(
                id, version, name, assemblyName, language,
                filePath, outputFilePath, outputRefFilePath, defaultNamespace: null, compilationOptions, parseOptions,
                documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments, analyzerConfigDocuments: null,
                isSubmission, hostObjectType, hasAllInformation: true);
        }

        private ProjectInfo With(
            ProjectAttributes attributes = null,
            CompilationOptions compilationOptions = null,
            ParseOptions parseOptions = null,
            IEnumerable<DocumentInfo> documents = null,
            IEnumerable<ProjectReference> projectReferences = null,
            IEnumerable<MetadataReference> metadataReferences = null,
            IEnumerable<AnalyzerReference> analyzerReferences = null,
            IEnumerable<DocumentInfo> additionalDocuments = null,
            IEnumerable<DocumentInfo> analyzerConfigDocuments = null,
            Optional<Type> hostObjectType = default)
        {
            var newAttributes = attributes ?? Attributes;
            var newCompilationOptions = compilationOptions ?? CompilationOptions;
            var newParseOptions = parseOptions ?? ParseOptions;
            var newDocuments = documents ?? Documents;
            var newProjectReferences = projectReferences ?? ProjectReferences;
            var newMetadataReferences = metadataReferences ?? MetadataReferences;
            var newAnalyzerReferences = analyzerReferences ?? AnalyzerReferences;
            var newAdditionalDocuments = additionalDocuments ?? AdditionalDocuments;
            var newAnalyzerConfigDocuments = analyzerConfigDocuments ?? AnalyzerConfigDocuments;
            var newHostObjectType = hostObjectType.HasValue ? hostObjectType.Value : HostObjectType;

            if (newAttributes == Attributes &&
                newCompilationOptions == CompilationOptions &&
                newParseOptions == ParseOptions &&
                newDocuments == Documents &&
                newProjectReferences == ProjectReferences &&
                newMetadataReferences == MetadataReferences &&
                newAnalyzerReferences == AnalyzerReferences &&
                newAdditionalDocuments == AdditionalDocuments &&
                newAnalyzerConfigDocuments == AnalyzerConfigDocuments &&
                newHostObjectType == HostObjectType)
            {
                return this;
            }

            return new ProjectInfo(
                newAttributes,
                newCompilationOptions,
                newParseOptions,
                newDocuments,
                newProjectReferences,
                newMetadataReferences,
                newAnalyzerReferences,
                newAdditionalDocuments,
                newAnalyzerConfigDocuments,
                newHostObjectType);
        }

        public ProjectInfo WithDocuments(IEnumerable<DocumentInfo> documents)
        {
            return With(documents: documents.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithAdditionalDocuments(IEnumerable<DocumentInfo> additionalDocuments)
        {
            return With(additionalDocuments: additionalDocuments.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithAnalyzerConfigDocuments(IEnumerable<DocumentInfo> analyzerConfigDocuments)
        {
            return With(analyzerConfigDocuments: analyzerConfigDocuments.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithVersion(VersionStamp version)
        {
            return With(attributes: Attributes.With(version: version));
        }

        public ProjectInfo WithName(string name)
        {
            return With(attributes: Attributes.With(name: name));
        }

        public ProjectInfo WithFilePath(string filePath)
        {
            return With(attributes: Attributes.With(filePath: filePath));
        }

        public ProjectInfo WithAssemblyName(string assemblyName)
        {
            return With(attributes: Attributes.With(assemblyName: assemblyName));
        }

        public ProjectInfo WithOutputFilePath(string outputFilePath)
        {
            return With(attributes: Attributes.With(outputPath: outputFilePath));
        }

        public ProjectInfo WithOutputRefFilePath(string outputRefFilePath)
        {
            return With(attributes: Attributes.With(outputRefPath: outputRefFilePath));
        }

        public ProjectInfo WithDefaultNamespace(string defaultNamespace)
        {
            return With(attributes: Attributes.With(defaultNamespace: defaultNamespace));
        }

        public ProjectInfo WithCompilationOptions(CompilationOptions compilationOptions)
        {
            return With(compilationOptions: compilationOptions);
        }

        public ProjectInfo WithParseOptions(ParseOptions parseOptions)
        {
            return With(parseOptions: parseOptions);
        }

        public ProjectInfo WithProjectReferences(IEnumerable<ProjectReference> projectReferences)
        {
            return With(projectReferences: projectReferences.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        {
            return With(metadataReferences: metadataReferences.ToImmutableReadOnlyListOrEmpty());
        }

        public ProjectInfo WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            return With(analyzerReferences: analyzerReferences.ToImmutableReadOnlyListOrEmpty());
        }

        internal ProjectInfo WithHasAllInformation(bool hasAllInformation)
        {
            return With(attributes: Attributes.With(hasAllInformation: hasAllInformation));
        }

        internal string GetDebuggerDisplay()
        {
            return nameof(ProjectInfo) + " " + Name + (!string.IsNullOrWhiteSpace(FilePath) ? " " + FilePath : "");
        }

        /// <summary>
        /// type that contains information regarding this project itself but
        /// no tree information such as document info
        /// </summary>
        internal class ProjectAttributes : IChecksummedObject, IObjectWritable
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
            /// The path to the reference assembly output file.
            /// </summary>
            public string OutputRefFilePath { get; }

            /// <summary>
            /// The default namespace of the project.
            /// </summary>
            public string DefaultNamespace { get; }

            /// <summary>
            /// True if this is a submission project for interactive sessions.
            /// </summary>
            public bool IsSubmission { get; }

            /// <summary>
            /// True if project information is complete. In some workspace hosts, it is possible
            /// a project only has partial information. In such cases, a project might not have all
            /// information on its files or references.
            /// </summary>
            public bool HasAllInformation { get; }

            public ProjectAttributes(
                ProjectId id,
                VersionStamp version,
                string name,
                string assemblyName,
                string language,
                string filePath,
                string outputFilePath,
                string outputRefFilePath,
                string defaultNamespace,
                bool isSubmission,
                bool hasAllInformation)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Language = language ?? throw new ArgumentNullException(nameof(language));
                AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));

                Version = version;
                FilePath = filePath;
                OutputFilePath = outputFilePath;
                OutputRefFilePath = outputRefFilePath;
                DefaultNamespace = defaultNamespace;
                IsSubmission = isSubmission;
                HasAllInformation = hasAllInformation;
            }

            public ProjectAttributes With(
                VersionStamp? version = default,
                string name = null,
                string assemblyName = null,
                string language = null,
                Optional<string> filePath = default,
                Optional<string> outputPath = default,
                Optional<string> outputRefPath = default,
                Optional<string> defaultNamespace = default,
                Optional<bool> isSubmission = default,
                Optional<bool> hasAllInformation = default)
            {
                var newVersion = version.HasValue ? version.Value : Version;
                var newName = name ?? Name;
                var newAssemblyName = assemblyName ?? AssemblyName;
                var newLanguage = language ?? Language;
                var newFilepath = filePath.HasValue ? filePath.Value : FilePath;
                var newOutputPath = outputPath.HasValue ? outputPath.Value : OutputFilePath;
                var newOutputRefPath = outputRefPath.HasValue ? outputRefPath.Value : OutputRefFilePath;
                var newDefaultNamespace = defaultNamespace.HasValue ? defaultNamespace.Value : DefaultNamespace;
                var newIsSubmission = isSubmission.HasValue ? isSubmission.Value : IsSubmission;
                var newHasAllInformation = hasAllInformation.HasValue ? hasAllInformation.Value : HasAllInformation;

                if (newVersion == Version &&
                    newName == Name &&
                    newAssemblyName == AssemblyName &&
                    newLanguage == Language &&
                    newFilepath == FilePath &&
                    newOutputPath == OutputFilePath &&
                    newOutputRefPath == OutputRefFilePath &&
                    newDefaultNamespace == DefaultNamespace &&
                    newIsSubmission == IsSubmission &&
                    newHasAllInformation == HasAllInformation)
                {
                    return this;
                }

                return new ProjectAttributes(
                    Id,
                    newVersion,
                    newName,
                    newAssemblyName,
                    newLanguage,
                    newFilepath,
                    newOutputPath,
                    newOutputRefPath,
                    newDefaultNamespace,
                    newIsSubmission,
                    newHasAllInformation);
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            public void WriteTo(ObjectWriter writer)
            {
                Id.WriteTo(writer);

                // TODO: figure out a way to send version info over as well
                // info.Version.WriteTo(writer);

                writer.WriteString(Name);
                writer.WriteString(AssemblyName);
                writer.WriteString(Language);
                writer.WriteString(FilePath);
                writer.WriteString(OutputFilePath);
                writer.WriteString(OutputRefFilePath);
                writer.WriteString(DefaultNamespace);
                writer.WriteBoolean(IsSubmission);
                writer.WriteBoolean(HasAllInformation);

                // TODO: once CompilationOptions, ParseOptions, ProjectReference, MetadataReference, AnalyzerReference supports
                //       serialization, we should include those here as well.
            }

            public static ProjectAttributes ReadFrom(ObjectReader reader)
            {
                var projectId = ProjectId.ReadFrom(reader);

                // var version = VersionStamp.ReadFrom(reader);
                var name = reader.ReadString();
                var assemblyName = reader.ReadString();
                var language = reader.ReadString();
                var filePath = reader.ReadString();
                var outputFilePath = reader.ReadString();
                var outputRefFilePath = reader.ReadString();
                var defaultNamespace = reader.ReadString();
                var isSubmission = reader.ReadBoolean();
                var hasAllInformation = reader.ReadBoolean();

                return new ProjectAttributes(projectId, VersionStamp.Create(), name, assemblyName, language, filePath, outputFilePath, outputRefFilePath, defaultNamespace, isSubmission, hasAllInformation);
            }

            private Checksum _lazyChecksum;
            Checksum IChecksummedObject.Checksum
            {
                get
                {
                    if (_lazyChecksum == null)
                    {
                        _lazyChecksum = Checksum.Create(WellKnownSynchronizationKind.ProjectAttributes, this);
                    }

                    return _lazyChecksum;
                }
            }
        }
    }
}
