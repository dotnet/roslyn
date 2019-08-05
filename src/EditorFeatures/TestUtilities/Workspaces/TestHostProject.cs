// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class TestHostProject
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
        private readonly string _filePath;
        private readonly string _outputFilePath;
        private readonly string _defaultNamespace;

        public IEnumerable<TestHostDocument> Documents;
        public IEnumerable<TestHostDocument> AdditionalDocuments;
        public IEnumerable<TestHostDocument> AnalyzerConfigDocuments;
        public IEnumerable<ProjectReference> ProjectReferences;

        public string Name
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

        public ProjectId Id
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

        public string AssemblyName
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

        public string OutputFilePath
        {
            get { return _outputFilePath; }
        }

        public string DefaultNamespace
        {
            get { return _defaultNamespace; }
        }

        internal TestHostProject(
            HostLanguageServices languageServices,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            params MetadataReference[] references)
            : this(languageServices, compilationOptions, parseOptions, "Test", references)
        {
        }
        internal TestHostProject(
            HostLanguageServices languageServices,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string assemblyName,
            params MetadataReference[] references)
            : this(languageServices,
                   compilationOptions,
                   parseOptions,
                   assemblyName: assemblyName,
                   projectName: assemblyName,
                   references: references,
                   documents: Array.Empty<TestHostDocument>())
        {
        }

        internal TestHostProject(
            HostLanguageServices languageServices,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string assemblyName,
            string projectName,
            IList<MetadataReference> references,
            IList<TestHostDocument> documents,
            IList<TestHostDocument> additionalDocuments = null,
            IList<TestHostDocument> analyzerConfigDocuments = null,
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
            this.AdditionalDocuments = additionalDocuments ?? SpecializedCollections.EmptyEnumerable<TestHostDocument>();
            this.AnalyzerConfigDocuments = analyzerConfigDocuments ?? SpecializedCollections.EmptyEnumerable<TestHostDocument>();
            ProjectReferences = SpecializedCollections.EmptyEnumerable<ProjectReference>();
            _isSubmission = isSubmission;
            _hostObjectType = hostObjectType;
            _version = VersionStamp.Create();
            _filePath = filePath;
            _outputFilePath = GetTestOutputFilePath(filePath);
            _defaultNamespace = defaultNamespace;
        }

        public TestHostProject(
            TestWorkspace workspace,
            TestHostDocument document,
            string name = null,
            string language = null,
            CompilationOptions compilationOptions = null,
            ParseOptions parseOptions = null,
            IEnumerable<TestHostProject> projectReferences = null,
            IEnumerable<MetadataReference> metadataReferences = null,
            IEnumerable<AnalyzerReference> analyzerReferences = null,
            string assemblyName = null,
            string defaultNamespace = null)
            : this(workspace, name, language, compilationOptions, parseOptions, SpecializedCollections.SingletonEnumerable(document), SpecializedCollections.EmptyEnumerable<TestHostDocument>(), SpecializedCollections.EmptyEnumerable<TestHostDocument>(), projectReferences, metadataReferences, analyzerReferences, assemblyName, defaultNamespace)
        {
        }

        public TestHostProject(
            TestWorkspace workspace,
            string name = null,
            string language = null,
            CompilationOptions compilationOptions = null,
            ParseOptions parseOptions = null,
            IEnumerable<TestHostDocument> documents = null,
            IEnumerable<TestHostDocument> additionalDocuments = null,
            IEnumerable<TestHostDocument> analyzerConfigDocuments = null,
            IEnumerable<TestHostProject> projectReferences = null,
            IEnumerable<MetadataReference> metadataReferences = null,
            IEnumerable<AnalyzerReference> analyzerReferences = null,
            string assemblyName = null,
            string defaultNamespace = null)
        {
            _name = name ?? "TestProject";

            _id = ProjectId.CreateNewId(debugName: this.Name);

            language = language ?? LanguageNames.CSharp;
            _languageServices = workspace.Services.GetLanguageServices(language);

            _compilationOptions = compilationOptions ?? this.LanguageServiceProvider.GetService<ICompilationFactoryService>().GetDefaultCompilationOptions();
            _parseOptions = parseOptions ?? this.LanguageServiceProvider.GetService<ISyntaxTreeFactoryService>().GetDefaultParseOptions();
            this.Documents = documents ?? SpecializedCollections.EmptyEnumerable<TestHostDocument>();
            this.AdditionalDocuments = additionalDocuments ?? SpecializedCollections.EmptyEnumerable<TestHostDocument>();
            this.AnalyzerConfigDocuments = analyzerConfigDocuments ?? SpecializedCollections.EmptyEnumerable<TestHostDocument>();
            ProjectReferences = projectReferences != null ? projectReferences.Select(p => new ProjectReference(p.Id)) : SpecializedCollections.EmptyEnumerable<ProjectReference>();
            _metadataReferences = metadataReferences ?? new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib };
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

        internal void SetSolution(TestHostSolution solution)
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

        internal void AddDocument(TestHostDocument document)
        {
            this.Documents = this.Documents.Concat(new TestHostDocument[] { document });
            document.SetProject(this);
        }

        internal void RemoveDocument(TestHostDocument document)
        {
            this.Documents = this.Documents.Where(d => d != document);
        }

        internal void AddAdditionalDocument(TestHostDocument document)
        {
            this.AdditionalDocuments = this.AdditionalDocuments.Concat(new TestHostDocument[] { document });
            document.SetProject(this);
        }

        internal void RemoveAdditionalDocument(TestHostDocument document)
        {
            this.AdditionalDocuments = this.AdditionalDocuments.Where(d => d != document);
        }

        internal void AddAnalyzerConfigDocument(TestHostDocument document)
        {
            this.AnalyzerConfigDocuments = this.AnalyzerConfigDocuments.Concat(new TestHostDocument[] { document });
            document.SetProject(this);
        }

        internal void RemoveAnalyzerConfigDocument(TestHostDocument document)
        {
            this.AnalyzerConfigDocuments = this.AnalyzerConfigDocuments.Where(d => d != document);
        }

        public string Language
        {
            get
            {
                return _languageServices.Language;
            }
        }

        internal HostLanguageServices LanguageServiceProvider
        {
            get
            {
                return _languageServices;
            }
        }

        public ProjectInfo ToProjectInfo()
        {
            return ProjectInfo.Create(
                this.Id,
                this.Version,
                this.Name,
                this.AssemblyName,
                this.Language,
                this.FilePath,
                this.OutputFilePath,
                outputRefFilePath: null,
                defaultNamespace: null,
                this.CompilationOptions,
                this.ParseOptions,
                this.Documents.Select(d => d.ToDocumentInfo()),
                this.ProjectReferences,
                this.MetadataReferences,
                this.AnalyzerReferences,
                this.AdditionalDocuments.Select(d => d.ToDocumentInfo()),
                this.AnalyzerConfigDocuments.Select(d => d.ToDocumentInfo()),
                this.IsSubmission,
                this.HostObjectType,
                hasAllInformation: true)
                .WithDefaultNamespace(this.DefaultNamespace);
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
