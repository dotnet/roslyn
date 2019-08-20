// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias WORKSPACES;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Host;
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
    using RelativePathResolver = WORKSPACES::Microsoft.CodeAnalysis.RelativePathResolver;

    public partial class TestWorkspace
    {
        /// <summary>
        /// This place-holder value is used to set a project's file path to be null.  It was explicitly chosen to be
        /// convoluted to avoid any accidental usage (e.g., what if I really wanted FilePath to be the string "null"?),
        /// obvious to anybody debugging that it is a special value, and invalid as an actual file path.
        /// </summary>
        public const string NullFilePath = "NullFilePath::{AFA13775-BB7D-4020-9E58-C68CF43D8A68}";

        private class TestDocumentationProvider : DocumentationProvider
        {
            protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
            {
                return string.Format("<member name='{0}'><summary>{0}</summary></member>", documentationMemberID);
            }

            public override bool Equals(object obj)
            {
                return (object)this == obj;
            }

            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(this);
            }
        }

        public static TestWorkspace Create(string xmlDefinition, bool completed = true, bool openDocuments = true, ExportProvider exportProvider = null)
        {
            return Create(XElement.Parse(xmlDefinition), completed, openDocuments, exportProvider);
        }

        public static TestWorkspace CreateWorkspace(
            XElement workspaceElement,
            bool completed = true,
            bool openDocuments = true,
            ExportProvider exportProvider = null,
            string workspaceKind = null)
        {
            return Create(workspaceElement, completed, openDocuments, exportProvider, workspaceKind);
        }

        internal static TestWorkspace Create(
            XElement workspaceElement,
            bool completed = true,
            bool openDocuments = true,
            ExportProvider exportProvider = null,
            string workspaceKind = null,
            IDocumentServiceProvider documentServiceProvider = null)
        {
            if (workspaceElement.Name != WorkspaceElementName)
            {
                throw new ArgumentException();
            }

            exportProvider ??= TestExportProvider.ExportProviderWithCSharpAndVisualBasic;

            var workspace = new TestWorkspace(exportProvider, workspaceKind);

            var projectNameToTestHostProject = new Dictionary<string, TestHostProject>();
            var projectElementToProjectName = new Dictionary<XElement, string>();
            var filePathToTextBufferMap = new Dictionary<string, ITextBuffer>();
            var projectIdentifier = 0;
            var documentIdentifier = 0;

            foreach (var projectElement in workspaceElement.Elements(ProjectElementName))
            {
                var project = CreateProject(
                    workspaceElement,
                    projectElement,
                    exportProvider,
                    workspace,
                    filePathToTextBufferMap,
                    documentServiceProvider,
                    ref projectIdentifier,
                    ref documentIdentifier);
                Assert.False(projectNameToTestHostProject.ContainsKey(project.Name), $"The workspace XML already contains a project with name {project.Name}");
                projectNameToTestHostProject.Add(project.Name, project);
                projectElementToProjectName.Add(projectElement, project.Name);
                workspace.Projects.Add(project);
            }

            var documentFilePaths = new HashSet<string>();
            foreach (var project in projectNameToTestHostProject.Values)
            {
                foreach (var document in project.Documents)
                {
                    Assert.True(document.IsLinkFile || documentFilePaths.Add(document.FilePath));
                }
            }

            var submissions = CreateSubmissions(workspace, workspaceElement.Elements(SubmissionElementName), exportProvider);

            foreach (var submission in submissions)
            {
                projectNameToTestHostProject.Add(submission.Name, submission);
            }

            var solution = new TestHostSolution(projectNameToTestHostProject.Values.ToArray());
            workspace.AddTestSolution(solution);

            foreach (var projectElement in workspaceElement.Elements(ProjectElementName))
            {
                foreach (var projectReference in projectElement.Elements(ProjectReferenceElementName))
                {
                    var fromName = projectElementToProjectName[projectElement];
                    var toName = projectReference.Value;

                    var fromProject = projectNameToTestHostProject[fromName];
                    var toProject = projectNameToTestHostProject[toName];

                    var aliases = projectReference.Attributes(AliasAttributeName).Select(a => a.Value).ToImmutableArray();

                    workspace.OnProjectReferenceAdded(fromProject.Id, new ProjectReference(toProject.Id, aliases.Any() ? aliases : default));
                }
            }

            for (var i = 1; i < submissions.Count; i++)
            {
                if (submissions[i].CompilationOptions == null)
                {
                    continue;
                }

                for (var j = i - 1; j >= 0; j--)
                {
                    if (submissions[j].CompilationOptions != null)
                    {
                        workspace.OnProjectReferenceAdded(submissions[i].Id, new ProjectReference(submissions[j].Id));
                        break;
                    }
                }
            }

            foreach (var project in projectNameToTestHostProject.Values)
            {
                foreach (var document in project.Documents)
                {
                    if (openDocuments)
                    {
                        workspace.OnDocumentOpened(document.Id, document.GetOpenTextContainer(), isCurrentContext: !document.IsLinkFile);
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
                MarkupTestFile.GetPositionAndSpans(markupCode,
                    out var code, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> spans);

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
                            projectName: submissionName,
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
            Dictionary<string, ITextBuffer> filePathToTextBufferMap,
            IDocumentServiceProvider documentServiceProvider,
            ref int projectId,
            ref int documentId)
        {
            var language = GetLanguage(workspace, projectElement);

            var assemblyName = GetAssemblyName(workspace, projectElement, ref projectId);

            string filePath;

            var projectName = projectElement.Attribute(ProjectNameAttribute)?.Value ?? assemblyName;

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
                filePath = projectName +
                    (language == LanguageNames.CSharp ? ".csproj" :
                     language == LanguageNames.VisualBasic ? ".vbproj" : ("." + language));
            }

            var contentTypeRegistryService = exportProvider.GetExportedValue<IContentTypeRegistryService>();
            var languageServices = workspace.Services.GetLanguageServices(language);

            var parseOptions = GetParseOptions(projectElement, language, languageServices);
            var compilationOptions = CreateCompilationOptions(workspace, projectElement, language, parseOptions);
            var rootNamespace = GetRootNamespace(workspace, compilationOptions, projectElement);

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
                    documentServiceProvider,
                    ref documentId);

                documents.Add(document);
            }

            var additionalDocuments = new List<TestHostDocument>();
            var additionalDocumentElements = projectElement.Elements(AdditionalDocumentElementName).ToList();
            foreach (var additionalDocumentElement in additionalDocumentElements)
            {
                var document = CreateDocument(
                    workspace,
                    workspaceElement,
                    additionalDocumentElement,
                    language,
                    exportProvider,
                    languageServices,
                    filePathToTextBufferMap,
                    documentServiceProvider,
                    ref documentId);

                additionalDocuments.Add(document);
            }

            var analyzerConfigDocuments = new List<TestHostDocument>();
            var analyzerConfigElements = projectElement.Elements(AnalyzerConfigDocumentElementName).ToList();
            foreach (var analyzerConfigElement in analyzerConfigElements)
            {
                var document = CreateDocument(
                    workspace,
                    workspaceElement,
                    analyzerConfigElement,
                    language,
                    exportProvider,
                    languageServices,
                    filePathToTextBufferMap,
                    documentServiceProvider,
                    ref documentId);

                analyzerConfigDocuments.Add(document);
            }

            return new TestHostProject(languageServices, compilationOptions, parseOptions, assemblyName, projectName, references, documents, additionalDocuments, analyzerConfigDocuments, filePath: filePath, analyzerReferences: analyzers, defaultNamespace: rootNamespace);
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

            var featuresAttribute = projectElement.Attribute(FeaturesAttributeName);
            if (featuresAttribute != null)
            {
                parseOptions = GetParseOptionsWithFeatures(parseOptions, featuresAttribute);
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
                    .Split(',').Select(v => KeyValuePairUtil.Create(v.Split('=').ElementAt(0), (object)v.Split('=').ElementAt(1))).ToImmutableArray());
            }
            else
            {
                throw new ArgumentException("Unexpected language '{0}' for generating custom parse options.", language);
            }
        }

        private static ParseOptions GetParseOptionsWithFeatures(ParseOptions parseOptions, XAttribute featuresAttribute)
        {
            var entries = featuresAttribute.Value.Split(';');
            var features = entries.Select(x =>
            {
                var split = x.Split('=');

                var key = split[0];
                var value = split.Length == 2 ? split[1] : "true";

                return new KeyValuePair<string, string>(key, value);
            });

            return parseOptions.WithFeatures(features);
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
            var languageName = projectElement.Attribute(LanguageAttributeName).Value;

            if (!workspace.Services.SupportedLanguages.Contains(languageName))
            {
                throw new ArgumentException(string.Format("Language should be one of '{0}' and it is {1}",
                    string.Join(", ", workspace.Services.SupportedLanguages),
                    languageName));
            }

            return languageName;
        }

        private static string GetRootNamespace(TestWorkspace workspace, CompilationOptions compilationOptions, XElement projectElement)
        {
            var language = GetLanguage(workspace, projectElement);
            var rootNamespaceAttribute = projectElement.Attribute(RootNamespaceAttributeName);

            if (GetLanguage(workspace, projectElement) == LanguageNames.VisualBasic)
            {
                // For VB tests, root namespace value must be defined in compilation options element, 
                // it can't use the property in project element to avoid confusion.
                Assert.Null(rootNamespaceAttribute);

                var vbCompilationOptions = (VisualBasicCompilationOptions)compilationOptions;
                return vbCompilationOptions.RootNamespace;
            }

            // If it's not defined, default to "" (global namespace)
            return rootNamespaceAttribute?.Value ?? string.Empty;
        }

        private static CompilationOptions CreateCompilationOptions(
            TestWorkspace workspace,
            XElement projectElement,
            string language,
            ParseOptions parseOptions)
        {
            var compilationOptionsElement = projectElement.Element(CompilationOptionsElementName);
            return language == LanguageNames.CSharp || language == LanguageNames.VisualBasic
                ? CreateCompilationOptions(workspace, language, compilationOptionsElement, parseOptions)
                : null;
        }

        private static CompilationOptions CreateCompilationOptions(TestWorkspace workspace, string language, XElement compilationOptionsElement, ParseOptions parseOptions)
        {
            var rootNamespace = new VisualBasicCompilationOptions(OutputKind.ConsoleApplication).RootNamespace;
            var globalImports = new List<GlobalImport>();
            var reportDiagnostic = ReportDiagnostic.Default;
            var cryptoKeyFile = default(string);
            var strongNameProvider = default(StrongNameProvider);
            var delaySign = default(bool?);
            var checkOverflow = false;
            var allowUnsafe = false;
            var outputKind = OutputKind.DynamicallyLinkedLibrary;

            if (compilationOptionsElement != null)
            {
                globalImports = compilationOptionsElement.Elements(GlobalImportElementName)
                                                         .Select(x => GlobalImport.Parse(x.Value)).ToList();
                var rootNamespaceAttribute = compilationOptionsElement.Attribute(RootNamespaceAttributeName);
                if (rootNamespaceAttribute != null)
                {
                    rootNamespace = rootNamespaceAttribute.Value;
                }

                var outputKindAttribute = compilationOptionsElement.Attribute(OutputKindName);
                if (outputKindAttribute != null)
                {
                    outputKind = (OutputKind)Enum.Parse(typeof(OutputKind), (string)outputKindAttribute.Value);
                }

                var checkOverflowAttribute = compilationOptionsElement.Attribute(CheckOverflowAttributeName);
                if (checkOverflowAttribute != null)
                {
                    checkOverflow = (bool)checkOverflowAttribute;
                }

                var allowUnsafeAttribute = compilationOptionsElement.Attribute(AllowUnsafeAttributeName);
                if (allowUnsafeAttribute != null)
                {
                    allowUnsafe = (bool)allowUnsafeAttribute;
                }

                var reportDiagnosticAttribute = compilationOptionsElement.Attribute(ReportDiagnosticAttributeName);
                if (reportDiagnosticAttribute != null)
                {
                    reportDiagnostic = (ReportDiagnostic)Enum.Parse(typeof(ReportDiagnostic), (string)reportDiagnosticAttribute);
                }

                var cryptoKeyFileAttribute = compilationOptionsElement.Attribute(CryptoKeyFileAttributeName);
                if (cryptoKeyFileAttribute != null)
                {
                    cryptoKeyFile = (string)cryptoKeyFileAttribute;
                }

                var strongNameProviderAttribute = compilationOptionsElement.Attribute(StrongNameProviderAttributeName);
                if (strongNameProviderAttribute != null)
                {
                    var type = Type.GetType((string)strongNameProviderAttribute);
                    // DesktopStrongNameProvider and SigningTestHelpers.VirtualizedStrongNameProvider do
                    // not have a default constructor but constructors with optional parameters.
                    // Activator.CreateInstance does not work with this.
                    if (type == typeof(DesktopStrongNameProvider))
                    {
                        strongNameProvider = SigningTestHelpers.DefaultDesktopStrongNameProvider;
                    }
                    else
                    {
                        strongNameProvider = (StrongNameProvider)Activator.CreateInstance(type);
                    }
                }

                var delaySignAttribute = compilationOptionsElement.Attribute(DelaySignAttributeName);
                if (delaySignAttribute != null)
                {
                    delaySign = (bool)delaySignAttribute;
                }

                var outputTypeAttribute = compilationOptionsElement.Attribute(OutputTypeAttributeName);
                if (outputTypeAttribute != null
                    && outputTypeAttribute.Value == "WindowsRuntimeMetadata")
                {
                    if (rootNamespaceAttribute == null)
                    {
                        rootNamespace = new VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata).RootNamespace;
                    }

                    // VB needs Compilation.ParseOptions set (we do the same at the VS layer)
                    return language == LanguageNames.CSharp
                       ? (CompilationOptions)new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata, allowUnsafe: allowUnsafe)
                       : new VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata).WithGlobalImports(globalImports).WithRootNamespace(rootNamespace)
                            .WithParseOptions((VisualBasicParseOptions)parseOptions ?? VisualBasicParseOptions.Default);
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
            compilationOptions = compilationOptions.WithOutputKind(outputKind)
                                                   .WithGeneralDiagnosticOption(reportDiagnostic)
                                                   .WithSourceReferenceResolver(SourceFileResolver.Default)
                                                   .WithXmlReferenceResolver(XmlFileResolver.Default)
                                                   .WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver(ImmutableArray<string>.Empty, null)))
                                                   .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                                                   .WithCryptoKeyFile(cryptoKeyFile)
                                                   .WithStrongNameProvider(strongNameProvider)
                                                   .WithDelaySign(delaySign)
                                                   .WithOverflowChecks(checkOverflow);

            if (language == LanguageNames.CSharp)
            {
                compilationOptions = ((CSharpCompilationOptions)compilationOptions).WithAllowUnsafe(allowUnsafe);
            }

            if (language == LanguageNames.VisualBasic)
            {
                // VB needs Compilation.ParseOptions set (we do the same at the VS layer)
                compilationOptions = ((VisualBasicCompilationOptions)compilationOptions).WithRootNamespace(rootNamespace)
                                                                                        .WithGlobalImports(globalImports)
                                                                                        .WithParseOptions((VisualBasicParseOptions)parseOptions ??
                                                                                            VisualBasicParseOptions.Default);
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
            IDocumentServiceProvider documentServiceProvider,
            ref int documentId)
        {
            string markupCode;
            string filePath;

            var isLinkFileAttribute = documentElement.Attribute(IsLinkFileAttributeName);
            var isLinkFile = isLinkFileAttribute != null && ((bool?)isLinkFileAttribute).HasValue && ((bool?)isLinkFileAttribute).Value;
            if (isLinkFile)
            {
                // This is a linked file. Use the filePath and markup from the referenced document.

                var originalAssemblyName = documentElement.Attribute(LinkAssemblyNameAttributeName)?.Value;
                var originalProjectName = documentElement.Attribute(LinkProjectNameAttributeName)?.Value;

                if (originalAssemblyName == null && originalProjectName == null)
                {
                    throw new ArgumentException($"Linked files must specify either a {LinkAssemblyNameAttributeName} or {LinkProjectNameAttributeName}");
                }

                var originalProject = workspaceElement.Elements(ProjectElementName).FirstOrDefault(p =>
                {
                    if (originalAssemblyName != null)
                    {
                        return p.Attribute(AssemblyNameAttributeName)?.Value == originalAssemblyName;
                    }
                    else
                    {
                        return p.Attribute(ProjectNameAttribute)?.Value == originalProjectName;
                    }
                });

                if (originalProject == null)
                {
                    if (originalProjectName != null)
                    {
                        throw new ArgumentException($"Linked file's {LinkProjectNameAttributeName} '{originalProjectName}' project not found.");
                    }
                    else
                    {
                        throw new ArgumentException($"Linked file's {LinkAssemblyNameAttributeName} '{originalAssemblyName}' project not found.");
                    }
                }

                var originalDocumentPath = documentElement.Attribute(LinkFilePathAttributeName)?.Value;

                if (originalDocumentPath == null)
                {
                    throw new ArgumentException($"Linked files must specify a {LinkFilePathAttributeName}");
                }

                documentElement = originalProject.Elements(DocumentElementName).FirstOrDefault(d =>
                {
                    return d.Attribute(FilePathAttributeName)?.Value == originalDocumentPath;
                });

                if (documentElement == null)
                {
                    throw new ArgumentException($"Linked file's LinkFilePath '{originalDocumentPath}' file not found.");
                }
            }

            markupCode = documentElement.NormalizedValue();
            filePath = GetFilePath(workspace, documentElement, ref documentId);

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

            MarkupTestFile.GetPositionAndSpans(markupCode,
                out var code, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            // For linked files, use the same ITextBuffer for all linked documents
            if (!filePathToTextBufferMap.TryGetValue(filePath, out var textBuffer))
            {
                textBuffer = EditorFactory.CreateBuffer(contentType.TypeName, exportProvider, code);
                filePathToTextBufferMap.Add(filePath, textBuffer);
            }

            var testDocumentServiceProvider = GetDocumentServiceProvider(documentElement);

            if (documentServiceProvider == null)
            {
                documentServiceProvider = testDocumentServiceProvider;
            }
            else if (testDocumentServiceProvider != null)
            {
                AssertEx.Fail("A non-null documentServiceProvider is provided when creating TestWorkspace from XML, which might conflict with the valued provided in document attributes.");
            }

            return new TestHostDocument(
                exportProvider, languageServiceProvider, textBuffer, filePath, cursorPosition, spans, codeKind, folders, isLinkFile, documentServiceProvider);
        }

        private static TestDocumentServiceProvider GetDocumentServiceProvider(XElement documentElement)
        {
            var canApplyChange = (bool?)documentElement.Attribute("CanApplyChange");
            var supportDiagnostics = (bool?)documentElement.Attribute("SupportDiagnostics");

            if (canApplyChange == null && supportDiagnostics == null)
            {
                return null;
            }

            return new TestDocumentServiceProvider(
                canApplyChange ?? true,
                supportDiagnostics ?? true);
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

            var folderContainers = folderAttribute.Value.Split(new[] { PathUtilities.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            return new ReadOnlyCollection<string>(folderContainers.ToList());
        }

        /// <summary>
        /// Takes completely valid code, compiles it, and emits it to a MetadataReference without using 
        /// the file system
        /// </summary>
        private static MetadataReference CreateMetadataReferenceFromSource(TestWorkspace workspace, XElement referencedSource)
        {
            var compilation = CreateCompilation(workspace, referencedSource);

            var aliasElement = referencedSource.Attribute("Aliases")?.Value;
            var aliases = aliasElement != null ? aliasElement.Split(',').Select(s => s.Trim()).ToImmutableArray() : default;

            var includeXmlDocComments = false;
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
            var languageName = GetLanguage(workspace, referencedSource);

            var assemblyName = "ReferencedAssembly";
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
            var parseOptions = GetParseOptions(referencedSource, languageName, languageServices);

            foreach (var documentElement in documentElements)
            {
                compilation = compilation.AddSyntaxTrees(CreateSyntaxTree(parseOptions, documentElement.Value));
            }

            foreach (var reference in CreateReferenceList(workspace, referencedSource))
            {
                compilation = compilation.AddReferences(reference);
            }

            return compilation;
        }

        private static SyntaxTree CreateSyntaxTree(ParseOptions options, string referencedCode)
        {
            if (LanguageNames.CSharp == options.Language)
            {
                return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(referencedCode, options);
            }
            else
            {
                return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(referencedCode, options);
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
                references = new List<MetadataReference> { TestBase.MscorlibRef_v46, TestBase.SystemRef_v46, TestBase.SystemCoreRef_v46 };
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

        public static bool IsWorkspaceElement(string text)
        {
            return text.TrimStart('\r', '\n', ' ').StartsWith("<Workspace>", StringComparison.Ordinal);
        }
    }
}
