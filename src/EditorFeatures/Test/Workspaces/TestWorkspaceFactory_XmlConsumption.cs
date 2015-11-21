// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias WORKSPACES;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    using System.Threading.Tasks;
    using RelativePathResolver = WORKSPACES::Microsoft.CodeAnalysis.RelativePathResolver;

    public partial class TestWorkspaceFactory
    {
        /// <summary>
        /// This place-holder value is used to set a project's file path to be null.  It was explicitly chosen to be
        /// convoluted to avoid any accidental usage (e.g., what if I really wanted FilePath to be the string "null"?),
        /// obvious to anybody debugging that it is a special value, and invalid as an actual file path.
        /// </summary>
        public const string NullFilePath = "NullFilePath::{AFA13775-BB7D-4020-9E58-C68CF43D8A68}";

        private class TestDocumentationProvider : DocumentationProvider
        {
            protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
            {
                return string.Format("<member name='{0}'><summary>{0}</summary></member>", documentationMemberID);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj);
            }

            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(this);
            }
        }

        public static Task<TestWorkspace> CreateWorkspaceAsync(string xmlDefinition, bool completed = true, bool openDocuments = true, ExportProvider exportProvider = null)
        {
            return CreateWorkspaceAsync(XElement.Parse(xmlDefinition), completed, openDocuments, exportProvider);
        }

        public static TestWorkspace CreateWorkspace(
            XElement workspaceElement,
            bool completed = true,
            bool openDocuments = true,
            ExportProvider exportProvider = null,
            string workspaceKind = null)
        {
            return CreateWorkspaceAsync(workspaceElement, completed, openDocuments, exportProvider, workspaceKind).WaitAndGetResult(CancellationToken.None);
        }

        public static async Task<TestWorkspace> CreateWorkspaceAsync(
            XElement workspaceElement,
            bool completed = true,
            bool openDocuments = true,
            ExportProvider exportProvider = null,
            string workspaceKind = null)
        {
            if (workspaceElement.Name != WorkspaceElementName)
            {
                throw new ArgumentException();
            }

            exportProvider = exportProvider ?? TestExportProvider.ExportProviderWithCSharpAndVisualBasic;

            var workspace = new TestWorkspace(exportProvider, workspaceKind);

            var projectMap = new Dictionary<string, TestHostProject>();
            var documentElementToFilePath = new Dictionary<XElement, string>();
            var projectElementToAssemblyName = new Dictionary<XElement, string>();
            var filePathToTextBufferMap = new Dictionary<string, ITextBuffer>();
            int projectIdentifier = 0;
            int documentIdentifier = 0;

            foreach (var projectElement in workspaceElement.Elements(ProjectElementName))
            {
                var project = CreateProject(
                    workspaceElement,
                    projectElement,
                    exportProvider,
                    workspace,
                    projectElementToAssemblyName,
                    documentElementToFilePath,
                    filePathToTextBufferMap,
                    ref projectIdentifier,
                    ref documentIdentifier);
                Assert.False(projectMap.ContainsKey(project.AssemblyName));
                projectMap.Add(project.AssemblyName, project);
                workspace.Projects.Add(project);
            }

            var documentFilePaths = new HashSet<string>();
            foreach (var project in projectMap.Values)
            {
                foreach (var document in project.Documents)
                {
                    Assert.True(document.IsLinkFile || documentFilePaths.Add(document.FilePath));
                }
            }

            var submissions = CreateSubmissions(workspace, workspaceElement.Elements(SubmissionElementName), exportProvider);

            foreach (var submission in submissions)
            {
                projectMap.Add(submission.AssemblyName, submission);
            }

            var solution = new TestHostSolution(projectMap.Values.ToArray());
            workspace.AddTestSolution(solution);

            foreach (var projectElement in workspaceElement.Elements(ProjectElementName))
            {
                foreach (var projectReference in projectElement.Elements(ProjectReferenceElementName))
                {
                    var fromName = projectElementToAssemblyName[projectElement];
                    var toName = projectReference.Value;

                    var fromProject = projectMap[fromName];
                    var toProject = projectMap[toName];

                    var aliases = projectReference.Attributes(AliasAttributeName).Select(a => a.Value).ToImmutableArray();

                    workspace.OnProjectReferenceAdded(fromProject.Id, new ProjectReference(toProject.Id, aliases.Any() ? aliases : default(ImmutableArray<string>)));
                }
            }

            for (int i = 1; i < submissions.Count; i++)
            {
                if (submissions[i].CompilationOptions == null)
                {
                    continue;
                }

                for (int j = i - 1; j >= 0; j--)
                {
                    if (submissions[j].CompilationOptions != null)
                    {
                        workspace.OnProjectReferenceAdded(submissions[i].Id, new ProjectReference(submissions[j].Id));
                        break;
                    }
                }
            }

            foreach (var project in projectMap.Values)
            {
                foreach (var document in project.Documents)
                {
                    if (openDocuments)
                    {
                        await workspace.OnDocumentOpenedAsync(document.Id, document.GetOpenTextContainer(), isCurrentContext: !document.IsLinkFile);
                    }

                    workspace.Documents.Add(document);
                }
            }

            return workspace;
        }

        private static IList<TestHostProject> CreateSubmissions(
            TestWorkspace workspace,
            IEnumerable<XElement> submissionElements,
            ExportProvider exportProvider)
        {
            var submissions = new List<TestHostProject>();
            var submissionIndex = 0;

            foreach (var submissionElement in submissionElements)
            {
                var submissionName = "Submission" + (submissionIndex++);

                var languageName = GetLanguage(workspace, submissionElement);

                // The document
                var markupCode = submissionElement.NormalizedValue();
                string code;
                int? cursorPosition;
                IDictionary<string, IList<TextSpan>> spans;
                MarkupTestFile.GetPositionAndSpans(markupCode, out code, out cursorPosition, out spans);

                var languageServices = workspace.Services.GetLanguageServices(languageName);
                var contentTypeLanguageService = languageServices.GetService<IContentTypeLanguageService>();

                var contentType = contentTypeLanguageService.GetDefaultContentType();
                var textBuffer = EditorFactory.CreateBuffer(contentType.TypeName, exportProvider, code);

                // The project

                var document = new TestHostDocument(exportProvider, languageServices, textBuffer, submissionName, cursorPosition, spans, SourceCodeKind.Script);
                var documents = new List<TestHostDocument> { document };

                if (languageName == NoCompilationConstants.LanguageName)
                {
                    submissions.Add(
                        new TestHostProject(
                            languageServices, 
                            compilationOptions: null, 
                            parseOptions: null, 
                            assemblyName: submissionName, 
                            references: null, 
                            documents: documents, 
                            isSubmission: true));
                    continue;
                }

                var syntaxFactory = languageServices.GetService<ISyntaxTreeFactoryService>();
                var compilationFactory = languageServices.GetService<ICompilationFactoryService>();
                var compilationOptions = compilationFactory.GetDefaultCompilationOptions().WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

                var parseOptions = syntaxFactory.GetDefaultParseOptions().WithKind(SourceCodeKind.Script);

                var references = CreateCommonReferences(workspace, submissionElement);

                var project = new TestHostProject(
                    languageServices,
                    compilationOptions,
                    parseOptions,
                    submissionName,
                    references,
                    documents, 
                    isSubmission: true);

                submissions.Add(project);
            }

            return submissions;
        }

        private static TestHostProject CreateProject(
            XElement workspaceElement,
            XElement projectElement,
            ExportProvider exportProvider,
            TestWorkspace workspace,
            Dictionary<XElement, string> projectElementToAssemblyName,
            Dictionary<XElement, string> documentElementToFilePath,
            Dictionary<string, ITextBuffer> filePathToTextBufferMap,
            ref int projectId,
            ref int documentId)
        {
            var language = GetLanguage(workspace, projectElement);

            var assemblyName = GetAssemblyName(workspace, projectElement, ref projectId);
            projectElementToAssemblyName.Add(projectElement, assemblyName);

            string filePath;

            if (projectElement.Attribute(FilePathAttributeName) != null)
            {
                filePath = projectElement.Attribute(FilePathAttributeName).Value;
                if (string.Compare(filePath, NullFilePath, StringComparison.Ordinal) == 0)
                {
                    // allow explicit null file path
                    filePath = null;
                }
            }
            else
            {
                filePath = assemblyName +
                    (language == LanguageNames.CSharp ? ".csproj" :
                     language == LanguageNames.VisualBasic ? ".vbproj" : ("." + language));
            }

            var contentTypeRegistryService = exportProvider.GetExportedValue<IContentTypeRegistryService>();
            var languageServices = workspace.Services.GetLanguageServices(language);

            var compilationOptions = CreateCompilationOptions(workspace, projectElement, language);
            var parseOptions = GetParseOptions(projectElement, language, languageServices);

            var references = CreateReferenceList(workspace, projectElement);
            var analyzers = CreateAnalyzerList(workspace, projectElement);

            var documents = new List<TestHostDocument>();
            var documentElements = projectElement.Elements(DocumentElementName).ToList();
            foreach (var documentElement in documentElements)
            {
                var document = CreateDocument(
                    workspace,
                    workspaceElement,
                    documentElement,
                    language,
                    exportProvider,
                    languageServices,
                    filePathToTextBufferMap,
                    ref documentId);

                documents.Add(document);
                documentElementToFilePath.Add(documentElement, document.FilePath);
            }

            return new TestHostProject(languageServices, compilationOptions, parseOptions, assemblyName, references, documents, filePath: filePath, analyzerReferences: analyzers);
        }

        private static ParseOptions GetParseOptions(XElement projectElement, string language, HostLanguageServices languageServices)
        {
            return language == LanguageNames.CSharp || language == LanguageNames.VisualBasic
                ? GetParseOptionsWorker(projectElement, language, languageServices)
                : null;
        }

        private static ParseOptions GetParseOptionsWorker(XElement projectElement, string language, HostLanguageServices languageServices)
        {
            ParseOptions parseOptions;
            var preprocessorSymbolsAttribute = projectElement.Attribute(PreprocessorSymbolsAttributeName);
            if (preprocessorSymbolsAttribute != null)
            {
                parseOptions = GetPreProcessorParseOptions(language, preprocessorSymbolsAttribute);
            }
            else
            {
                parseOptions = languageServices.GetService<ISyntaxTreeFactoryService>().GetDefaultParseOptions();
            }

            var languageVersionAttribute = projectElement.Attribute(LanguageVersionAttributeName);
            if (languageVersionAttribute != null)
            {
                parseOptions = GetParseOptionsWithLanguageVersion(language, parseOptions, languageVersionAttribute);
            }

            var documentationMode = GetDocumentationMode(projectElement);
            if (documentationMode != null)
            {
                parseOptions = parseOptions.WithDocumentationMode(documentationMode.Value);
            }

            return parseOptions;
        }

        private static ParseOptions GetPreProcessorParseOptions(string language, XAttribute preprocessorSymbolsAttribute)
        {
            if (language == LanguageNames.CSharp)
            {
                return new CSharpParseOptions(preprocessorSymbols: preprocessorSymbolsAttribute.Value.Split(','));
            }
            else if (language == LanguageNames.VisualBasic)
            {
                return new VisualBasicParseOptions(preprocessorSymbols: preprocessorSymbolsAttribute.Value
                    .Split(',').Select(v => KeyValuePair.Create(v.Split('=').ElementAt(0), (object)v.Split('=').ElementAt(1))).ToImmutableArray());
            }
            else
            {
                throw new ArgumentException("Unexpected language '{0}' for generating custom parse options.", language);
            }
        }

        private static ParseOptions GetParseOptionsWithLanguageVersion(string language, ParseOptions parseOptions, XAttribute languageVersionAttribute)
        {
            if (language == LanguageNames.CSharp)
            {
                var languageVersion = (CodeAnalysis.CSharp.LanguageVersion)Enum.Parse(typeof(CodeAnalysis.CSharp.LanguageVersion), languageVersionAttribute.Value);
                parseOptions = ((CSharpParseOptions)parseOptions).WithLanguageVersion(languageVersion);
            }
            else if (language == LanguageNames.VisualBasic)
            {
                var languageVersion = (CodeAnalysis.VisualBasic.LanguageVersion)Enum.Parse(typeof(CodeAnalysis.VisualBasic.LanguageVersion), languageVersionAttribute.Value);
                parseOptions = ((VisualBasicParseOptions)parseOptions).WithLanguageVersion(languageVersion);
            }

            return parseOptions;
        }

        private static DocumentationMode? GetDocumentationMode(XElement projectElement)
        {
            var documentationModeAttribute = projectElement.Attribute(DocumentationModeAttributeName);
            if (documentationModeAttribute != null)
            {
                return (DocumentationMode)Enum.Parse(typeof(DocumentationMode), documentationModeAttribute.Value);
            }
            else
            {
                return null;
            }
        }

        private static string GetAssemblyName(TestWorkspace workspace, XElement projectElement, ref int projectId)
        {
            var assemblyNameAttribute = projectElement.Attribute(AssemblyNameAttributeName);
            if (assemblyNameAttribute != null)
            {
                return assemblyNameAttribute.Value;
            }

            var language = GetLanguage(workspace, projectElement);

            projectId++;
            return language == LanguageNames.CSharp ? "CSharpAssembly" + projectId :
                   language == LanguageNames.VisualBasic ? "VisualBasicAssembly" + projectId :
                                                            language + "Assembly" + projectId;
        }

        private static string GetLanguage(TestWorkspace workspace, XElement projectElement)
        {
            string languageName = projectElement.Attribute(LanguageAttributeName).Value;

            if (!workspace.Services.SupportedLanguages.Contains(languageName))
            {
                throw new ArgumentException(string.Format("Language should be one of '{0}' and it is {1}",
                    string.Join(", ", workspace.Services.SupportedLanguages),
                    languageName));
            }

            return languageName;
        }

        private static CompilationOptions CreateCompilationOptions(
            TestWorkspace workspace,
            XElement projectElement,
            string language)
        {
            var compilationOptionsElement = projectElement.Element(CompilationOptionsElementName);
            return language == LanguageNames.CSharp || language == LanguageNames.VisualBasic
                ? CreateCompilationOptions(workspace, language, compilationOptionsElement)
                : null;
        }

        private static CompilationOptions CreateCompilationOptions(TestWorkspace workspace, string language, XElement compilationOptionsElement)
        {
            var rootNamespace = new VisualBasicCompilationOptions(OutputKind.ConsoleApplication).RootNamespace;
            var globalImports = new List<GlobalImport>();
            var reportDiagnostic = ReportDiagnostic.Default;

            if (compilationOptionsElement != null)
            {
                globalImports = compilationOptionsElement.Elements(GlobalImportElementName)
                                                         .Select(x => GlobalImport.Parse(x.Value)).ToList();
                var rootNamespaceAttribute = compilationOptionsElement.Attribute(RootNamespaceAttributeName);
                if (rootNamespaceAttribute != null)
                {
                    rootNamespace = rootNamespaceAttribute.Value;
                }

                var reportDiagnosticAttribute = compilationOptionsElement.Attribute(ReportDiagnosticAttributeName);
                if (reportDiagnosticAttribute != null)
                {
                    reportDiagnostic = (ReportDiagnostic)Enum.Parse(typeof(ReportDiagnostic), (string)reportDiagnosticAttribute);
                }

                var outputTypeAttribute = compilationOptionsElement.Attribute(OutputTypeAttributeName);
                if (outputTypeAttribute != null
                    && outputTypeAttribute.Value == "WindowsRuntimeMetadata")
                {
                    if (rootNamespaceAttribute == null)
                    {
                        rootNamespace = new VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata).RootNamespace;
                    }

                    return language == LanguageNames.CSharp
                       ? (CompilationOptions)new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata)
                       : new VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata).WithGlobalImports(globalImports).WithRootNamespace(rootNamespace);
                }
            }
            else
            {
                // Add some common global imports by default for VB
                globalImports.Add(GlobalImport.Parse("System"));
                globalImports.Add(GlobalImport.Parse("System.Collections.Generic"));
                globalImports.Add(GlobalImport.Parse("System.Linq"));
            }

            // TODO: Allow these to be specified.
            var languageServices = workspace.Services.GetLanguageServices(language);
            var metadataService = workspace.Services.GetService<IMetadataService>();
            var compilationOptions = languageServices.GetService<ICompilationFactoryService>().GetDefaultCompilationOptions();
            compilationOptions = compilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
                                                   .WithGeneralDiagnosticOption(reportDiagnostic)
                                                   .WithSourceReferenceResolver(SourceFileResolver.Default)
                                                   .WithXmlReferenceResolver(XmlFileResolver.Default)
                                                   .WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver(ImmutableArray<string>.Empty, null)))
                                                   .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);

            if (language == LanguageNames.VisualBasic)
            {
                compilationOptions = ((VisualBasicCompilationOptions)compilationOptions).WithRootNamespace(rootNamespace)
                                                                                        .WithGlobalImports(globalImports);
            }

            return compilationOptions;
        }

        private static TestHostDocument CreateDocument(
            TestWorkspace workspace,
            XElement workspaceElement,
            XElement documentElement,
            string language,
            ExportProvider exportProvider,
            HostLanguageServices languageServiceProvider,
            Dictionary<string, ITextBuffer> filePathToTextBufferMap,
            ref int documentId)
        {
            string markupCode;
            string filePath;

            var isLinkFileAttribute = documentElement.Attribute(IsLinkFileAttributeName);
            bool isLinkFile = isLinkFileAttribute != null && ((bool?)isLinkFileAttribute).HasValue && ((bool?)isLinkFileAttribute).Value;
            if (isLinkFile)
            {
                // This is a linked file. Use the filePath and markup from the referenced document.

                var originalProjectName = documentElement.Attribute(LinkAssemblyNameAttributeName);
                var originalDocumentPath = documentElement.Attribute(LinkFilePathAttributeName);

                if (originalProjectName == null || originalDocumentPath == null)
                {
                    throw new ArgumentException("Linked file specified without LinkAssemblyName or LinkFilePath.");
                }

                var originalProjectNameStr = originalProjectName.Value;
                var originalDocumentPathStr = originalDocumentPath.Value;

                var originalProject = workspaceElement.Elements(ProjectElementName).First(p =>
                {
                    var assemblyName = p.Attribute(AssemblyNameAttributeName);
                    return assemblyName != null && assemblyName.Value == originalProjectNameStr;
                });

                if (originalProject == null)
                {
                    throw new ArgumentException("Linked file's LinkAssemblyName '{0}' project not found.", originalProjectNameStr);
                }

                var originalDocument = originalProject.Elements(DocumentElementName).First(d =>
                {
                    var documentPath = d.Attribute(FilePathAttributeName);
                    return documentPath != null && documentPath.Value == originalDocumentPathStr;
                });

                if (originalDocument == null)
                {
                    throw new ArgumentException("Linked file's LinkFilePath '{0}' file not found.", originalDocumentPathStr);
                }

                markupCode = originalDocument.NormalizedValue();
                filePath = GetFilePath(workspace, originalDocument, ref documentId);
            }
            else
            {
                markupCode = documentElement.NormalizedValue();
                filePath = GetFilePath(workspace, documentElement, ref documentId);
            }

            var folders = GetFolders(documentElement);
            var optionsElement = documentElement.Element(ParseOptionsElementName);

            // TODO: Allow these to be specified.
            var codeKind = SourceCodeKind.Regular;
            if (optionsElement != null)
            {
                var attr = optionsElement.Attribute(KindAttributeName);
                codeKind = attr == null
                    ? SourceCodeKind.Regular
                    : (SourceCodeKind)Enum.Parse(typeof(SourceCodeKind), attr.Value);
            }

            var contentTypeLanguageService = languageServiceProvider.GetService<IContentTypeLanguageService>();
            var contentType = contentTypeLanguageService.GetDefaultContentType();

            string code;
            int? cursorPosition;
            IDictionary<string, IList<TextSpan>> spans;
            MarkupTestFile.GetPositionAndSpans(markupCode, out code, out cursorPosition, out spans);

            // For linked files, use the same ITextBuffer for all linked documents
            ITextBuffer textBuffer;
            if (!filePathToTextBufferMap.TryGetValue(filePath, out textBuffer))
            {
                textBuffer = EditorFactory.CreateBuffer(contentType.TypeName, exportProvider, code);
                filePathToTextBufferMap.Add(filePath, textBuffer);
            }

            return new TestHostDocument(exportProvider, languageServiceProvider, textBuffer, filePath, cursorPosition, spans, codeKind, folders, isLinkFile);
        }

        private static string GetFilePath(
            TestWorkspace workspace,
            XElement documentElement,
            ref int documentId)
        {
            var filePathAttribute = documentElement.Attribute(FilePathAttributeName);
            if (filePathAttribute != null)
            {
                return filePathAttribute.Value;
            }

            var language = GetLanguage(workspace, documentElement.Ancestors(ProjectElementName).Single());
            documentId++;
            var name = "Test" + documentId;
            return language == LanguageNames.CSharp ? name + ".cs" : name + ".vb";
        }

        private static IReadOnlyList<string> GetFolders(XElement documentElement)
        {
            var folderAttribute = documentElement.Attribute(FoldersAttributeName);
            if (folderAttribute == null)
            {
                return null;
            }

            var folderContainers = folderAttribute.Value.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return new ReadOnlyCollection<string>(folderContainers.ToList());
        }

        /// <summary>
        /// Takes completely valid code, compiles it, and emits it to a MetadataReference without using 
        /// the file system
        /// </summary>
        private static MetadataReference CreateMetadataReferenceFromSource(TestWorkspace workspace, XElement referencedSource)
        {
            var compilation = CreateCompilation(workspace, referencedSource);

            var aliasElement = referencedSource.Attribute("Aliases") != null ? referencedSource.Attribute("Aliases").Value : null;
            var aliases = aliasElement != null ? aliasElement.Split(',').Select(s => s.Trim()).ToImmutableArray() : default(ImmutableArray<string>);

            bool includeXmlDocComments = false;
            var includeXmlDocCommentsAttribute = referencedSource.Attribute(IncludeXmlDocCommentsAttributeName);
            if (includeXmlDocCommentsAttribute != null &&
                ((bool?)includeXmlDocCommentsAttribute).HasValue &&
                ((bool?)includeXmlDocCommentsAttribute).Value)
            {
                includeXmlDocComments = true;
            }

            return MetadataReference.CreateFromImage(compilation.EmitToArray(), new MetadataReferenceProperties(aliases: aliases), includeXmlDocComments ? new DeferredDocumentationProvider(compilation) : null);
        }

        private static Compilation CreateCompilation(TestWorkspace workspace, XElement referencedSource)
        {
            string languageName = GetLanguage(workspace, referencedSource);

            string assemblyName = "ReferencedAssembly";
            var assemblyNameAttribute = referencedSource.Attribute(AssemblyNameAttributeName);
            if (assemblyNameAttribute != null)
            {
                assemblyName = assemblyNameAttribute.Value;
            }

            var languageServices = workspace.Services.GetLanguageServices(languageName);
            var compilationFactory = languageServices.GetService<ICompilationFactoryService>();
            var options = compilationFactory.GetDefaultCompilationOptions().WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

            var compilation = compilationFactory.CreateCompilation(assemblyName, options);

            var documentElements = referencedSource.Elements(DocumentElementName).ToList();
            foreach (var documentElement in documentElements)
            {
                compilation = compilation.AddSyntaxTrees(CreateSyntaxTree(languageName, documentElement.Value));
            }

            foreach (var reference in CreateReferenceList(workspace, referencedSource))
            {
                compilation = compilation.AddReferences(reference);
            }

            return compilation;
        }

        private static SyntaxTree CreateSyntaxTree(string languageName, string referencedCode)
        {
            if (LanguageNames.CSharp == languageName)
            {
                return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(referencedCode);
            }
            else
            {
                return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(referencedCode);
            }
        }

        private static IList<MetadataReference> CreateReferenceList(TestWorkspace workspace, XElement element)
        {
            var references = CreateCommonReferences(workspace, element);
            foreach (var reference in element.Elements(MetadataReferenceElementName))
            {
                references.Add(MetadataReference.CreateFromFile(reference.Value));
            }

            foreach (var metadataReferenceFromSource in element.Elements(MetadataReferenceFromSourceElementName))
            {
                references.Add(CreateMetadataReferenceFromSource(workspace, metadataReferenceFromSource));
            }

            return references;
        }

        private static IList<AnalyzerReference> CreateAnalyzerList(TestWorkspace workspace, XElement projectElement)
        {
            var analyzers = new List<AnalyzerReference>();
            foreach (var analyzer in projectElement.Elements(AnalyzerElementName))
            {
                analyzers.Add(
                    new AnalyzerImageReference(
                        ImmutableArray<DiagnosticAnalyzer>.Empty,
                        display: (string)analyzer.Attribute(AnalyzerDisplayAttributeName),
                        fullPath: (string)analyzer.Attribute(AnalyzerFullPathAttributeName)));
            }

            return analyzers;
        }

        private static IList<MetadataReference> CreateCommonReferences(TestWorkspace workspace, XElement element)
        {
            var references = new List<MetadataReference>();

            var net45 = element.Attribute(CommonReferencesNet45AttributeName);
            if (net45 != null &&
                ((bool?)net45).HasValue &&
                ((bool?)net45).Value)
            {
                references = new List<MetadataReference> { TestBase.MscorlibRef_v4_0_30316_17626, TestBase.SystemRef_v4_0_30319_17929, TestBase.SystemCoreRef_v4_0_30319_17929 };
                if (GetLanguage(workspace, element) == LanguageNames.VisualBasic)
                {
                    references.Add(TestBase.MsvbRef);
                    references.Add(TestBase.SystemXmlRef);
                    references.Add(TestBase.SystemXmlLinqRef);
                }
            }

            var commonReferencesAttribute = element.Attribute(CommonReferencesAttributeName);
            if (commonReferencesAttribute != null &&
                ((bool?)commonReferencesAttribute).HasValue &&
                ((bool?)commonReferencesAttribute).Value)
            {
                references = new List<MetadataReference> { TestBase.MscorlibRef_v4_0_30316_17626, TestBase.SystemRef_v4_0_30319_17929, TestBase.SystemCoreRef_v4_0_30319_17929 };
                if (GetLanguage(workspace, element) == LanguageNames.VisualBasic)
                {
                    references.Add(TestBase.MsvbRef_v4_0_30319_17929);
                    references.Add(TestBase.SystemXmlRef);
                    references.Add(TestBase.SystemXmlLinqRef);
                }
            }

            var winRT = element.Attribute(CommonReferencesWinRTAttributeName);
            if (winRT != null &&
                ((bool?)winRT).HasValue &&
                ((bool?)winRT).Value)
            {
                references = new List<MetadataReference>(TestBase.WinRtRefs.Length);
                references.AddRange(TestBase.WinRtRefs);
                if (GetLanguage(workspace, element) == LanguageNames.VisualBasic)
                {
                    references.Add(TestBase.MsvbRef_v4_0_30319_17929);
                    references.Add(TestBase.SystemXmlRef);
                    references.Add(TestBase.SystemXmlLinqRef);
                }
            }

            var portable = element.Attribute(CommonReferencesPortableAttributeName);
            if (portable != null &&
                ((bool?)portable).HasValue &&
                ((bool?)portable).Value)
            {
                references = new List<MetadataReference>(TestBase.PortableRefsMinimal.Length);
                references.AddRange(TestBase.PortableRefsMinimal);
            }

            var systemRuntimeFacade = element.Attribute(CommonReferenceFacadeSystemRuntimeAttributeName);
            if (systemRuntimeFacade != null &&
                ((bool?)systemRuntimeFacade).HasValue &&
                ((bool?)systemRuntimeFacade).Value)
            {
                references.Add(TestBase.SystemRuntimeFacadeRef);
            }

            return references;
        }
    }
}
