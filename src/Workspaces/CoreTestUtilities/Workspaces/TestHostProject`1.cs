// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public abstract class TestHostProject<TDocument> : AbstractTestHostProject
        where TDocument : TestHostDocument
    {
        private readonly HostLanguageServices _languageServices;

        private readonly ProjectId _id;
        private readonly string _name;
        private readonly IEnumerable<MetadataReference> _metadataReferences;
        private readonly IEnumerable<AnalyzerReference> _analyzerReferences;
        private readonly CompilationOptions _compilationOptions;
        private readonly ParseOptions _parseOptions;
        private readonly bool _isSubmission;
        private readonly string _assemblyName;
        private readonly Type _hostObjectType;
        private readonly VersionStamp _version;
        private readonly string _outputFilePath;
        private readonly string _defaultNamespace;

        public IEnumerable<TDocument> Documents;
        public IEnumerable<TDocument> AdditionalDocuments;
        public IEnumerable<TDocument> AnalyzerConfigDocuments;
        public IEnumerable<ProjectReference> ProjectReferences;
        private string _filePath;

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public IEnumerable<MetadataReference> MetadataReferences
        {
            get
            {
                return _metadataReferences;
            }
        }

        public IEnumerable<AnalyzerReference> AnalyzerReferences
        {
            get
            {
                return _analyzerReferences;
            }
        }

        public CompilationOptions CompilationOptions
        {
            get
            {
                return _compilationOptions;
            }
        }

        public ParseOptions ParseOptions
        {
            get
            {
                return _parseOptions;
            }
        }

        public override ProjectId Id
        {
            get
            {
                return _id;
            }
        }

        public bool IsSubmission
        {
            get
            {
                return _isSubmission;
            }
        }

        public override string AssemblyName
        {
            get
            {
                return _assemblyName;
            }
        }

        public Type HostObjectType
        {
            get
            {
                return _hostObjectType;
            }
        }

        public VersionStamp Version
        {
            get
            {
                return _version;
            }
        }

        public string FilePath
        {
            get
            {
                return _filePath;
            }
        }

        internal void OnProjectFilePathChanged(string filePath)
            => _filePath = filePath;

        public string OutputFilePath
        {
            get { return _outputFilePath; }
        }

        public string DefaultNamespace
        {
            get { return _defaultNamespace; }
        }

        protected TestHostProject(
            HostLanguageServices languageServices,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string assemblyName,
            string projectName,
            IList<MetadataReference> references,
            IList<TDocument> documents,
            IList<TDocument> additionalDocuments = null,
            IList<TDocument> analyzerConfigDocuments = null,
            Type hostObjectType = null,
            bool isSubmission = false,
            string filePath = null,
            IList<AnalyzerReference> analyzerReferences = null,
            string defaultNamespace = null)
        {
            _assemblyName = assemblyName;
            _name = projectName;
            _id = ProjectId.CreateNewId(debugName: this.AssemblyName);
            _languageServices = languageServices;
            _compilationOptions = compilationOptions;
            _parseOptions = parseOptions;
            _metadataReferences = references;
            _analyzerReferences = analyzerReferences ?? SpecializedCollections.EmptyEnumerable<AnalyzerReference>();
            this.Documents = documents;
            this.AdditionalDocuments = additionalDocuments ?? SpecializedCollections.EmptyEnumerable<TDocument>();
            this.AnalyzerConfigDocuments = analyzerConfigDocuments ?? SpecializedCollections.EmptyEnumerable<TDocument>();
            ProjectReferences = SpecializedCollections.EmptyEnumerable<ProjectReference>();
            _isSubmission = isSubmission;
            _hostObjectType = hostObjectType;
            _version = VersionStamp.Create();
            _filePath = filePath;
            _outputFilePath = GetTestOutputFilePath(filePath);
            _defaultNamespace = defaultNamespace;
        }

        protected TestHostProject(
            HostWorkspaceServices hostServices,
            string name = null,
            string language = null,
            CompilationOptions compilationOptions = null,
            ParseOptions parseOptions = null,
            IEnumerable<TDocument> documents = null,
            IEnumerable<TDocument> additionalDocuments = null,
            IEnumerable<TDocument> analyzerConfigDocuments = null,
            IEnumerable<TestHostProject<TDocument>> projectReferences = null,
            IEnumerable<MetadataReference> metadataReferences = null,
            IEnumerable<AnalyzerReference> analyzerReferences = null,
            string assemblyName = null,
            string defaultNamespace = null)
        {
            _name = name ?? "TestProject";

            _id = ProjectId.CreateNewId(debugName: this.Name);

            language = language ?? LanguageNames.CSharp;
            _languageServices = hostServices.GetLanguageServices(language);

            _compilationOptions = compilationOptions ?? this.LanguageServiceProvider.GetService<ICompilationFactoryService>().GetDefaultCompilationOptions();
            _parseOptions = parseOptions ?? this.LanguageServiceProvider.GetService<ISyntaxTreeFactoryService>().GetDefaultParseOptions();
            this.Documents = documents ?? SpecializedCollections.EmptyEnumerable<TDocument>();
            this.AdditionalDocuments = additionalDocuments ?? SpecializedCollections.EmptyEnumerable<TDocument>();
            this.AnalyzerConfigDocuments = analyzerConfigDocuments ?? SpecializedCollections.EmptyEnumerable<TDocument>();
            ProjectReferences = projectReferences != null ? projectReferences.Select(p => new ProjectReference(p.Id)) : SpecializedCollections.EmptyEnumerable<ProjectReference>();
            _metadataReferences = metadataReferences ?? new MetadataReference[] { TestMetadata.Net451.mscorlib };
            _analyzerReferences = analyzerReferences ?? SpecializedCollections.EmptyEnumerable<AnalyzerReference>();
            _assemblyName = assemblyName ?? "TestProject";
            _version = VersionStamp.Create();
            _outputFilePath = GetTestOutputFilePath(_filePath);
            _defaultNamespace = defaultNamespace;

            if (documents != null)
            {
                foreach (var doc in documents)
                {
                    doc.SetProject(this);
                }
            }

            if (additionalDocuments != null)
            {
                foreach (var doc in additionalDocuments)
                {
                    doc.SetProject(this);
                }
            }

            if (analyzerConfigDocuments != null)
            {
                foreach (var doc in analyzerConfigDocuments)
                {
                    doc.SetProject(this);
                }
            }
        }

        internal void SetSolution()
        {
            // set up back pointer to this project.
            if (this.Documents != null)
            {
                foreach (var doc in this.Documents)
                {
                    doc.SetProject(this);
                }

                foreach (var doc in this.AdditionalDocuments)
                {
                    doc.SetProject(this);
                }

                foreach (var doc in this.AnalyzerConfigDocuments)
                {
                    doc.SetProject(this);
                }
            }
        }

        internal void AddDocument(TDocument document)
        {
            this.Documents = this.Documents.Concat(new TDocument[] { document });
            document.SetProject(this);
        }

        internal void RemoveDocument(TDocument document)
            => this.Documents = this.Documents.Where(d => d != document);

        internal void AddAdditionalDocument(TDocument document)
        {
            this.AdditionalDocuments = this.AdditionalDocuments.Concat(new TDocument[] { document });
            document.SetProject(this);
        }

        internal void RemoveAdditionalDocument(TDocument document)
            => this.AdditionalDocuments = this.AdditionalDocuments.Where(d => d != document);

        internal void AddAnalyzerConfigDocument(TDocument document)
        {
            this.AnalyzerConfigDocuments = this.AnalyzerConfigDocuments.Concat(new TDocument[] { document });
            document.SetProject(this);
        }

        internal void RemoveAnalyzerConfigDocument(TDocument document)
            => this.AnalyzerConfigDocuments = this.AnalyzerConfigDocuments.Where(d => d != document);

        public override string Language
        {
            get
            {
                return _languageServices.Language;
            }
        }

        public override HostLanguageServices LanguageServiceProvider
        {
            get
            {
                return _languageServices;
            }
        }

        public ProjectInfo ToProjectInfo()
        {
            return ProjectInfo.Create(
                new ProjectInfo.ProjectAttributes(
                    Id,
                    Version,
                    name: Name,
                    assemblyName: AssemblyName,
                    language: Language,
                    compilationOutputFilePaths: default,
                    checksumAlgorithm: Text.SourceHashAlgorithms.Default,
                    defaultNamespace: DefaultNamespace,
                    filePath: FilePath,
                    outputFilePath: OutputFilePath,
                    isSubmission: IsSubmission),
                CompilationOptions,
                ParseOptions,
                documents: Documents.Where(d => !d.IsSourceGenerated).Select(d => d.ToDocumentInfo()),
                ProjectReferences,
                MetadataReferences,
                AnalyzerReferences,
                additionalDocuments: AdditionalDocuments.Select(d => d.ToDocumentInfo()),
                analyzerConfigDocuments: AnalyzerConfigDocuments.Select(d => d.ToDocumentInfo()),
                HostObjectType);
        }

        // It is identical with the internal extension method 'GetDefaultExtension' defined in OutputKind.cs.
        // However, we could not apply for InternalVisibleToTest due to other parts of this assembly
        // complaining about CS0507: "cannot change access modifiers when overriding 'access' inherited member".
        private static string GetDefaultExtension(OutputKind kind)
        {
            switch (kind)
            {
                case OutputKind.ConsoleApplication:
                case OutputKind.WindowsApplication:
                case OutputKind.WindowsRuntimeApplication:
                    return ".exe";

                case OutputKind.DynamicallyLinkedLibrary:
                    return ".dll";

                case OutputKind.NetModule:
                    return ".netmodule";

                case OutputKind.WindowsRuntimeMetadata:
                    return ".winmdobj";

                default:
                    return ".dll";
            }
        }

        private string GetTestOutputFilePath(string filepath)
        {
            var outputFilePath = @"Z:\";

            try
            {
                outputFilePath = Path.GetDirectoryName(filepath);
            }
            catch (ArgumentException)
            {
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                outputFilePath = @"Z:\";
            }

            return this.CompilationOptions == null ? "" : Path.Combine(outputFilePath, this.AssemblyName + GetDefaultExtension(this.CompilationOptions.OutputKind));
        }
    }
}
