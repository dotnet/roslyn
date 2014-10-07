using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class FrozenProjectInfo : ProjectInfo
    {
        private readonly ProjectId id;
        private readonly VersionStamp version;
        private readonly string name;
        private readonly string assemblyName;
        private readonly string language;
        private readonly string filePath;
        private readonly string outputFilePath;
        private readonly CommonCompilationOptions compilationOptions;
        private readonly CommonParseOptions parseOptions;
        private readonly ImmutableList<FrozenDocumentInfo> documents;
        private readonly ImmutableList<ProjectReference> projectReferences;
        private readonly ImmutableList<MetadataReference> metadataReferences;
        private readonly FileResolver fileResolver;
        private readonly bool isSubmission;
        private readonly Type hostObjectType;

        public FrozenProjectInfo(
            ProjectId id,
            VersionStamp version,
            string name,
            string assemblyName,
            string language,
            string filePath,
            string outputFilePath,
            CommonCompilationOptions compilationOptions,
            CommonParseOptions parseOptions,
            ImmutableList<FrozenDocumentInfo> documents,
            ImmutableList<ProjectReference> projectReferences,
            ImmutableList<MetadataReference> metadataReferences,
            FileResolver fileResolver,
            bool isSubmission,
            Type hostObjectType)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            if (name == null)
            {
                throw new ArgumentNullException("displayName");
            }

            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }

            if (language == null)
            {
                throw new ArgumentNullException("language");
            }

            if (projectReferences == null)
            {
                throw new ArgumentNullException("projectReferences");
            }

            if (projectReferences.Any(p => p == null))
            {
                throw new ArgumentNullException("a project reference in projectReference can't be null.");
            }

            this.id = id;
            this.version = version;
            this.name = name;
            this.assemblyName = assemblyName;
            this.language = language;
            this.filePath = filePath;
            this.outputFilePath = outputFilePath;
            this.compilationOptions = compilationOptions;
            this.parseOptions = parseOptions;
            this.documents = documents;
            this.projectReferences = projectReferences;
            this.metadataReferences = metadataReferences;
            this.fileResolver = fileResolver;
            this.isSubmission = isSubmission;
            this.hostObjectType = hostObjectType;
        }

        private FrozenProjectInfo With(
            ProjectId id = null,
            VersionStamp version = null,
            string name = null,
            string assemblyName = null,
            string language = null,
            Optional<string> filePath = default(Optional<string>),
            Optional<string> outputPath = default(Optional<string>),
            CommonCompilationOptions compilationOptions = null,
            CommonParseOptions parseOptions = null,
            ImmutableList<FrozenDocumentInfo> documents = null,
            ImmutableList<ProjectReference> projectReferences = null,
            ImmutableList<MetadataReference> metadataReferences = null,
            Optional<FileResolver> fileResolver = default(Optional<FileResolver>),
            Optional<bool> isSubmission = default(Optional<bool>),
            Optional<Type> hostObjectType = default(Optional<Type>))
        {
            var newId = id ?? this.Id;
            var newVersion = version ?? this.Version;
            var newName = name ?? this.Name;
            var newAssemblyName = assemblyName ?? this.AssemblyName;
            var newLanguage = language ?? this.Language;
            var newFilepath = filePath.HasValue ? filePath.Value : this.FilePath;
            var newOutputPath = outputPath.HasValue ? outputPath.Value : this.OutputFilePath;
            var newCompilationOptions = compilationOptions ?? this.CompilationOptions;
            var newParseOptions = parseOptions ?? this.ParseOptions;
            var newDocuments = documents ?? this.documents;
            var newProjectReferences = projectReferences ?? this.projectReferences;
            var newMetadataReferences = metadataReferences ?? this.metadataReferences;
            var newFileResolver = fileResolver.HasValue ? fileResolver.Value : this.FileResolver;
            var newIsSubmission = isSubmission.HasValue ? isSubmission.Value : this.IsSubmission;
            var newHostObjectType = hostObjectType.HasValue ? hostObjectType.Value : this.HostObjectType;

            if (newId == this.Id &&
                newVersion == this.Version &&
                newName == this.Name &&
                newAssemblyName == this.AssemblyName &&
                newLanguage == this.Language &&
                newFilepath == this.FilePath &&
                newOutputPath == this.OutputFilePath &&
                newCompilationOptions == this.CompilationOptions &&
                newParseOptions == this.ParseOptions &&
                newDocuments == this.documents &&
                newProjectReferences == this.ProjectReferences &&
                newMetadataReferences == this.MetadataReferences &&
                newFileResolver == this.FileResolver &&
                newIsSubmission == this.IsSubmission &&
                newHostObjectType == this.HostObjectType)
            {
                return this;
            }

            return new FrozenProjectInfo(
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
                    newFileResolver,
                    newIsSubmission,
                    newHostObjectType);
        }

        public override string AssemblyName
        {
            get
            {
                return this.assemblyName;
            }
        }

        public override CommonCompilationOptions CompilationOptions
        {
            get
            {
                return this.compilationOptions;
            }
        }

        public override IEnumerable<DocumentInfo> Documents
        {
            get
            {
                return this.documents;
            }
        }

        public override string FilePath
        {
            get
            {
                return this.filePath;
            }
        }

        public override string OutputFilePath
        {
            get
            {
                return this.outputFilePath;
            }
        }

        public override FileResolver FileResolver
        {
            get
            {
                return this.fileResolver;
            }
        }

        public override Type HostObjectType
        {
            get
            {
                return this.hostObjectType;
            }
        }

        public override ProjectId Id
        {
            get
            {
                return this.id;
            }
        }

        public override bool IsSubmission
        {
            get
            {
                return this.isSubmission;
            }
        }

        public override string Language
        {
            get
            {
                return this.language;
            }
        }

        public override IEnumerable<MetadataReference> MetadataReferences
        {
            get
            {
                return this.metadataReferences;
            }
        }

        public override string Name
        {
            get
            {
                return this.name;
            }
        }

        public override CommonParseOptions ParseOptions
        {
            get
            {
                return this.parseOptions;
            }
        }

        public override IEnumerable<ProjectReference> ProjectReferences
        {
            get
            {
                return this.projectReferences;
            }
        }

        public override VersionStamp Version
        {
            get
            {
                return this.version;
            }
        }

        internal FrozenProjectInfo WithDocuments(ImmutableList<FrozenDocumentInfo> documents)
        {
            return this.With(documents: documents);
        }

        internal FrozenProjectInfo WithVersion(VersionStamp version)
        {
            return this.With(version: version);
        }

        internal FrozenProjectInfo WithAssemblyName(string assemblyName)
        {
            return this.With(assemblyName: assemblyName);
        }

        internal FrozenProjectInfo WithCompilationOptions(CommonCompilationOptions compilationOptions)
        {
            return this.With(compilationOptions: compilationOptions);
        }

        internal FrozenProjectInfo WithParseOptions(CommonParseOptions parseOptions)
        {
            return this.With(parseOptions: parseOptions);
        }

        internal FrozenProjectInfo WithProjectReferences(ImmutableList<ProjectReference> projectReferences)
        {
            return this.With(projectReferences: projectReferences);
        }

        internal FrozenProjectInfo WithMetadataReferences(ImmutableList<MetadataReference> metadataReferences)
        {
            return this.With(metadataReferences: metadataReferences);
        }

        internal FrozenProjectInfo WithFileResolver(FileResolver fileResolver)
        {
            return this.With(fileResolver: fileResolver);
        }
    }
}