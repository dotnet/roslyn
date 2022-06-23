// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.Editor.CSharp.DecompiledSource;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
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
                => string.Format("<member name='{0}'><summary>{0}</summary></member>", documentationMemberID);

            public override bool Equals(object obj)
                => ReferenceEquals(this, obj);

            public override int GetHashCode()
                => RuntimeHelpers.GetHashCode(this);
        }

        public static TestWorkspace Create(string xmlDefinition, bool openDocuments = false, ExportProvider exportProvider = null, TestComposition composition = null)
            => Create(XElement.Parse(xmlDefinition), openDocuments, exportProvider, composition);

        public static TestWorkspace CreateWorkspace(
            XElement workspaceElement,
            bool openDocuments = true,
            ExportProvider exportProvider = null,
            TestComposition composition = null,
            string workspaceKind = null)
        {
            return Create(workspaceElement, openDocuments, exportProvider, composition, workspaceKind);
        }

        internal static TestWorkspace Create(
            XElement workspaceElement,
            bool openDocuments = true,
            ExportProvider exportProvider = null,
            TestComposition composition = null,
            string workspaceKind = null,
            IDocumentServiceProvider documentServiceProvider = null,
            bool ignoreUnchangeableDocumentsWhenApplyingChanges = true)
        {
            var workspace = new TestWorkspace(exportProvider, composition, workspaceKind, ignoreUnchangeableDocumentsWhenApplyingChanges: ignoreUnchangeableDocumentsWhenApplyingChanges);
            workspace.InitializeDocuments(workspaceElement, openDocuments, documentServiceProvider);
            return workspace;
        }

        internal void InitializeDocuments(
            string language,
            CompilationOptions compilationOptions = null,
            ParseOptions parseOptions = null,
            string[] files = null,
            string[] metadataReferences = null,
            string extension = null,
            bool commonReferences = true,
            bool openDocuments = true,
            IDocumentServiceProvider documentServiceProvider = null)
        {
            var workspaceElement = CreateWorkspaceElement(
                language,
                compilationOptions,
                parseOptions,
                files,
                sourceGeneratedFiles: Array.Empty<string>(),
                metadataReferences,
                extension,
                commonReferences);

            InitializeDocuments(workspaceElement, openDocuments, documentServiceProvider);
        }

        internal void InitializeDocuments(
            XElement workspaceElement,
            bool openDocuments = true,
            IDocumentServiceProvider documentServiceProvider = null)
        {
            if (workspaceElement.Name != WorkspaceElementName)
            {
                throw new ArgumentException();
            }

            var projectNameToTestHostProject = new Dictionary<string, TestHostProject>();
            var projectElementToProjectName = new Dictionary<XElement, string>();
            var projectIdentifier = 0;
            var documentIdentifier = 0;

            foreach (var projectElement in workspaceElement.Elements(ProjectElementName))
            {
                var project = CreateProject(
                    workspaceElement,
                    projectElement,
                    ExportProvider,
                    this,
                    documentServiceProvider,
                    ref projectIdentifier,
                    ref documentIdentifier);

                Assert.False(projectNameToTestHostProject.ContainsKey(project.Name), $"The workspace XML already contains a project with name {project.Name}");
                projectNameToTestHostProject.Add(project.Name, project);
                projectElementToProjectName.Add(projectElement, project.Name);
                Projects.Add(project);
            }

            var documentFilePaths = new HashSet<string>();
            foreach (var project in projectNameToTestHostProject.Values)
            {
                foreach (var document in project.Documents)
                {
                    Assert.True(document.IsLinkFile || documentFilePaths.Add(document.FilePath));

                    Documents.Add(document);
                }
            }

            var submissions = CreateSubmissions(workspaceElement.Elements(SubmissionElementName), ExportProvider);

            foreach (var submission in submissions)
            {
                projectNameToTestHostProject.Add(submission.Name, submission);
                Documents.Add(submission.Documents.Single());
            }

            var solution = new TestHostSolution(projectNameToTestHostProject.Values.ToArray());
            AddTestSolution(solution);

            foreach (var projectElement in workspaceElement.Elements(ProjectElementName))
            {
                foreach (var projectReference in projectElement.Elements(ProjectReferenceElementName))
                {
                    var fromName = projectElementToProjectName[projectElement];
                    var toName = projectReference.Value;

                    var fromProject = projectNameToTestHostProject[fromName];
                    var toProject = projectNameToTestHostProject[toName];

                    var aliases = projectReference.Attributes(AliasAttributeName).Select(a => a.Value).ToImmutableArray();

                    OnProjectReferenceAdded(fromProject.Id, new ProjectReference(toProject.Id, aliases.Any() ? aliases : default));
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
                        OnProjectReferenceAdded(submissions[i].Id, new ProjectReference(submissions[j].Id));
                        break;
                    }
                }
            }

            foreach (var project in projectNameToTestHostProject.Values)
            {
                foreach (var document in project.Documents)
                {
                    if (openDocuments && !document.IsSourceGenerated)
                    {
                        // This implicitly opens the document in the workspace by fetching the container.
                        document.GetOpenTextContainer();
                    }
                }
            }
        }

        private IList<TestHostProject> CreateSubmissions(
            IEnumerable<XElement> submissionElements,
            ExportProvider exportProvider)
        {
            var submissions = new List<TestHostProject>();
            var submissionIndex = 0;

            foreach (var submissionElement in submissionElements)
            {
                var submissionName = "Submission" + (submissionIndex++);

                var languageName = GetLanguage(this, submissionElement);

                // The document
                var markupCode = submissionElement.NormalizedValue();
                MarkupTestFile.GetPositionAndSpans(markupCode,
                    out var code, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> spans);

                var languageServices = Services.GetLanguageServices(languageName);

                // The project

                var document = new TestHostDocument(exportProvider, languageServices, code, submissionName, submissionName, cursorPosition, spans, SourceCodeKind.Script);
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

                var metadataService = Services.GetService<IMetadataService>();
                var metadataResolver = RuntimeMetadataReferenceResolver.CreateCurrentPlatformResolver(fileReferenceProvider: metadataService.GetReference);
                var syntaxFactory = languageServices.GetService<ISyntaxTreeFactoryService>();
                var compilationFactory = languageServices.GetService<ICompilationFactoryService>();
                var compilationOptions = compilationFactory.GetDefaultCompilationOptions()
                    .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
                    .WithMetadataReferenceResolver(metadataResolver);

                var parseOptions = syntaxFactory.GetDefaultParseOptions().WithKind(SourceCodeKind.Script);

                var references = CreateCommonReferences(this, submissionElement);

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
            IDocumentServiceProvider documentServiceProvider,
            ref int projectId,
            ref int documentId)
        {
            AssertNoChildText(projectElement);

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

            var languageServices = workspace.Services.GetLanguageServices(language);

            var parseOptions = GetParseOptions(projectElement, language, languageServices);
            var compilationOptions = CreateCompilationOptions(workspace, projectElement, language, parseOptions);
            var rootNamespace = GetRootNamespace(workspace, compilationOptions, projectElement);

            var references = CreateReferenceList(workspace, projectElement);
            var analyzers = CreateAnalyzerList(projectElement);

            var documents = new List<TestHostDocument>();
            var documentElements = projectElement.Elements(DocumentElementName).ToList();
            foreach (var documentElement in documentElements)
            {
                var document = CreateDocument(
                    workspace,
                    workspaceElement,
                    documentElement,
                    exportProvider,
                    languageServices,
                    documentServiceProvider,
                    ref documentId);

                documents.Add(document);
            }

            SingleFileTestGenerator testGenerator = null;
            foreach (var sourceGeneratedDocumentElement in projectElement.Elements(DocumentFromSourceGeneratorElementName))
            {
                if (testGenerator is null)
                {
                    testGenerator = new SingleFileTestGenerator();
                    analyzers.Add(new TestGeneratorReference(testGenerator));
                }

                var name = GetFileName(workspace, sourceGeneratedDocumentElement, ref documentId);

                var markupCode = sourceGeneratedDocumentElement.NormalizedValue();
                MarkupTestFile.GetPositionAndSpans(markupCode,
                    out var code, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> spans);

                var documentFilePath = typeof(SingleFileTestGenerator).Assembly.GetName().Name + '\\' + typeof(SingleFileTestGenerator).FullName + '\\' + name;
                var document = new TestHostDocument(exportProvider, languageServices, code, name, documentFilePath, cursorPosition, spans, generator: testGenerator);
                documents.Add(document);

                testGenerator.AddSource(code, name);
            }

            var additionalDocuments = new List<TestHostDocument>();
            var additionalDocumentElements = projectElement.Elements(AdditionalDocumentElementName).ToList();
            foreach (var additionalDocumentElement in additionalDocumentElements)
            {
                var document = CreateDocument(
                    workspace,
                    workspaceElement,
                    additionalDocumentElement,
                    exportProvider,
                    languageServices,
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
                    exportProvider,
                    languageServices,
                    documentServiceProvider,
                    ref documentId);

                analyzerConfigDocuments.Add(document);
            }

            return new TestHostProject(languageServices, compilationOptions, parseOptions, assemblyName, projectName, references, documents, additionalDocuments, analyzerConfigDocuments, filePath: filePath, analyzerReferences: analyzers, defaultNamespace: rootNamespace);
        }

        private static ParseOptions GetParseOptions(XElement projectElement, string language, HostLanguageServices languageServices)
        {
            return language is LanguageNames.CSharp or LanguageNames.VisualBasic
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
                if (CodeAnalysis.CSharp.LanguageVersionFacts.TryParse(languageVersionAttribute.Value, out var languageVersion))
                {
                    return ((CSharpParseOptions)parseOptions).WithLanguageVersion(languageVersion);
                }
            }
            else if (language == LanguageNames.VisualBasic)
            {
                var languageVersion = CodeAnalysis.VisualBasic.LanguageVersion.Default;
                if (CodeAnalysis.VisualBasic.LanguageVersionFacts.TryParse(languageVersionAttribute.Value, ref languageVersion))
                {
                    return ((VisualBasicParseOptions)parseOptions).WithLanguageVersion(languageVersion);
                }
            }

            throw new Exception($"LanguageVersion attribute on {languageVersionAttribute.Parent} was not recognized.");
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
            var languageAttribute = projectElement.Attribute(LanguageAttributeName);
            if (languageAttribute == null)
            {
                throw new ArgumentException($"{projectElement} is missing a {LanguageAttributeName} attribute.");
            }

            var languageName = languageAttribute.Value;

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
            return language is LanguageNames.CSharp or LanguageNames.VisualBasic
                ? CreateCompilationOptions(workspace, language, compilationOptionsElement, parseOptions)
                : null;
        }

        private static CompilationOptions CreateCompilationOptions(TestWorkspace workspace, string language, XElement compilationOptionsElement, ParseOptions parseOptions)
        {
            var rootNamespace = new VisualBasicCompilationOptions(OutputKind.ConsoleApplication).RootNamespace;
            var globalImports = new List<GlobalImport>();
            var reportDiagnostic = ReportDiagnostic.Default;
            var cryptoKeyFile = (string)null;
            var strongNameProvider = (StrongNameProvider)null;
            var delaySign = (bool?)null;
            var checkOverflow = false;
            var allowUnsafe = false;
            var outputKind = OutputKind.DynamicallyLinkedLibrary;
            var nullable = NullableContextOptions.Disable;

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
                    outputKind = (OutputKind)Enum.Parse(typeof(OutputKind), outputKindAttribute.Value);
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

                var nullableAttribute = compilationOptionsElement.Attribute(NullableAttributeName);
                if (nullableAttribute != null)
                {
                    nullable = (NullableContextOptions)Enum.Parse(typeof(NullableContextOptions), nullableAttribute.Value);
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
                       ? new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata, allowUnsafe: allowUnsafe)
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
                compilationOptions = ((CSharpCompilationOptions)compilationOptions).WithAllowUnsafe(allowUnsafe).WithNullableContextOptions(nullable);
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
            ExportProvider exportProvider,
            HostLanguageServices languageServiceProvider,
            IDocumentServiceProvider documentServiceProvider,
            ref int documentId)
        {
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

            var markupCode = documentElement.NormalizedValue();
            var fileName = GetFileName(workspace, documentElement, ref documentId);

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

            var markupAttribute = documentElement.Attribute(MarkupAttributeName);
            var isMarkup = markupAttribute == null || (string)markupAttribute == "true" || (string)markupAttribute == "SpansOnly";

            string code;
            int? cursorPosition;
            ImmutableDictionary<string, ImmutableArray<TextSpan>> spans;

            if (isMarkup)
            {
                // if the caller doesn't want us caring about positions, then replace any $'s with a character unlikely
                // to ever show up in the doc naturally.  Then, after we convert things, change that character back. We
                // do this as a single character so that all the positions of the spans do not change.
                if ((string)markupAttribute == "SpansOnly")
                    markupCode = markupCode.Replace("$", "\uD7FF");

                TestFileMarkupParser.GetPositionAndSpans(markupCode, out code, out cursorPosition, out spans);

                // if we were told SpansOnly then that means that $$ isn't actually a caret (but is something like a raw
                // interpolated string delimiter.  In that case, if we did see a $$ add it back it at the location we
                // found it, and set the cursor back to null as the test will be specifying that location manually
                // itself.
                if ((string)markupAttribute == "SpansOnly")
                {
                    Contract.ThrowIfTrue(cursorPosition != null);
                    code = code.Replace("\uD7FF", "$");
                }
            }
            else
            {
                code = markupCode;
                cursorPosition = null;
                spans = ImmutableDictionary<string, ImmutableArray<TextSpan>>.Empty;
            }

            var testDocumentServiceProvider = GetDocumentServiceProvider(documentElement);

            if (documentServiceProvider == null)
            {
                documentServiceProvider = testDocumentServiceProvider;
            }
            else if (testDocumentServiceProvider != null)
            {
                AssertEx.Fail($"The document attributes on file {fileName} conflicted");
            }

            return new TestHostDocument(
                exportProvider, languageServiceProvider, code, fileName, fileName, cursorPosition, spans, codeKind, folders, isLinkFile, documentServiceProvider);
        }

        internal static TestHostDocument CreateDocument(
            XElement documentElement,
            ExportProvider exportProvider,
            HostLanguageServices languageServiceProvider,
            ImmutableArray<string> roles)
        {
            var markupCode = documentElement.NormalizedValue();

            var folders = GetFolders(documentElement);
            var optionsElement = documentElement.Element(ParseOptionsElementName);

            var codeKind = SourceCodeKind.Regular;
            if (optionsElement != null)
            {
                var attr = optionsElement.Attribute(KindAttributeName);
                codeKind = attr == null
                    ? SourceCodeKind.Regular
                    : (SourceCodeKind)Enum.Parse(typeof(SourceCodeKind), attr.Value);
            }

            MarkupTestFile.GetPositionAndSpans(markupCode,
                out var code, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            var documentServiceProvider = GetDocumentServiceProvider(documentElement);

            return new TestHostDocument(
                exportProvider, languageServiceProvider, code, name: string.Empty, filePath: string.Empty, cursorPosition, spans, codeKind, folders, isLinkFile: false, documentServiceProvider, roles: roles);
        }

#nullable enable

        private static TestDocumentServiceProvider? GetDocumentServiceProvider(XElement documentElement)
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

#nullable disable

        private static string GetFileName(
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
        private static MetadataReference CreateMetadataReferenceFromSource(TestWorkspace workspace, XElement projectElement, XElement referencedSource)
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

            var referencesOnDisk = projectElement.Attribute(ReferencesOnDiskAttributeName) is { } onDiskAttribute
                && ((bool?)onDiskAttribute).GetValueOrDefault();

            var image = compilation.EmitToArray();
            var metadataReference = MetadataReference.CreateFromImage(image, new MetadataReferenceProperties(aliases: aliases), includeXmlDocComments ? new DeferredDocumentationProvider(compilation) : null);
            if (referencesOnDisk)
            {
                AssemblyResolver.TestAccessor.AddInMemoryImage(metadataReference, "unknown", image);
            }

            return metadataReference;
        }

        private static Compilation CreateCompilation(TestWorkspace workspace, XElement referencedSource)
        {
            AssertNoChildText(referencedSource);

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
                // Read the image to an ImmutableArray<byte>, since the GC does a better job of tracking these than
                // Marshal.AllocHGlobal and thus knowing when it's necessary to run finalizers to clean up old Metadata
                // objects that are no longer in use. There are no public APIs available to directly dispose of these
                // images, so we are relying on GC running finalizers to avoid OutOfMemoryException during tests.
                var content = File.ReadAllBytes(reference.Value);
                references.Add(MetadataReference.CreateFromImage(content, filePath: reference.Value));
            }

            foreach (var metadataReferenceFromSource in element.Elements(MetadataReferenceFromSourceElementName))
            {
                references.Add(CreateMetadataReferenceFromSource(workspace, element, metadataReferenceFromSource));
            }

            return references;
        }

        private static IList<AnalyzerReference> CreateAnalyzerList(XElement projectElement)
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
                references = new List<MetadataReference> { TestBase.MscorlibRef_v4_0_30316_17626, TestBase.SystemRef_v4_0_30319_17929, TestBase.SystemCoreRef_v4_0_30319_17929, TestBase.SystemRuntimeSerializationRef_v4_0_30319_17929 };
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
                references = new List<MetadataReference> { TestBase.MscorlibRef_v46, TestBase.SystemRef_v46, TestBase.SystemCoreRef_v46, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef };
                if (GetLanguage(workspace, element) == LanguageNames.VisualBasic)
                {
                    references.Add(TestBase.MsvbRef_v4_0_30319_17929);
                    references.Add(TestBase.SystemXmlRef);
                    references.Add(TestBase.SystemXmlLinqRef);
                }
            }

            var commonReferencesWithoutValueTupleAttribute = element.Attribute(CommonReferencesWithoutValueTupleAttributeName);
            if (commonReferencesWithoutValueTupleAttribute != null &&
                ((bool?)commonReferencesWithoutValueTupleAttribute).HasValue &&
                ((bool?)commonReferencesWithoutValueTupleAttribute).Value)
            {
                references = new List<MetadataReference> { TestBase.MscorlibRef_v46, TestBase.SystemRef_v46, TestBase.SystemCoreRef_v46 };
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

            var netcore30 = element.Attribute(CommonReferencesNetCoreAppName);
            if (netcore30 != null &&
                ((bool?)netcore30).HasValue &&
                ((bool?)netcore30).Value)
            {
                references = NetCoreApp.StandardReferences.ToList();
            }

            var netstandard20 = element.Attribute(CommonReferencesNetStandard20Name);
            if (netstandard20 != null &&
                ((bool?)netstandard20).HasValue &&
                ((bool?)netstandard20).Value)
            {
                references = TargetFrameworkUtil.NetStandard20References.ToList();
            }

            var net6 = element.Attribute(CommonReferencesNet6Name);
            if (net6 != null &&
                ((bool?)net6).HasValue &&
                ((bool?)net6).Value)
            {
                references = TargetFrameworkUtil.GetReferences(TargetFramework.Net60).ToList();
            }

            return references;
        }

        public static bool IsWorkspaceElement(string text)
            => text.TrimStart('\r', '\n', ' ').StartsWith("<Workspace>", StringComparison.Ordinal);

        private static void AssertNoChildText(XElement element)
        {
            foreach (var node in element.Nodes())
            {
                if (node is XText text && !string.IsNullOrWhiteSpace(text.Value))
                {
                    throw new Exception($"Element {element} has child text that isn't recognized. The XML syntax is invalid.");
                }
            }
        }
    }
}
