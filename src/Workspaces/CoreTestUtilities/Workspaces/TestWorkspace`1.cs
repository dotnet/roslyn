// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias SCRIPTING;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using RuntimeMetadataReferenceResolver = SCRIPTING::Microsoft.CodeAnalysis.Scripting.Hosting.RuntimeMetadataReferenceResolver;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public abstract partial class TestWorkspace<TDocument, TProject, TSolution> : Workspace, ILspWorkspace
        where TDocument : TestHostDocument
        where TProject : TestHostProject<TDocument>
        where TSolution : TestHostSolution<TDocument>
    {
        public ExportProvider ExportProvider { get; }
        public TestComposition? Composition { get; }

        public bool CanApplyChangeDocument { get; set; }

        internal override bool CanChangeActiveContextDocument { get { return true; } }

        public IList<TProject> Projects { get; }
        public IList<TDocument> Documents { get; }
        public IList<TDocument> AdditionalDocuments { get; }
        public IList<TDocument> AnalyzerConfigDocuments { get; }
        public IList<TDocument> ProjectionDocuments { get; }

        /// <summary>
        /// Should only be used in LSP layer and above.
        /// </summary>
        [Experimental("LAYERING_IGlobalOptionService")]
        internal IGlobalOptionService GlobalOptions => GetService<IGlobalOptionService>();

        internal override bool IgnoreUnchangeableDocumentsWhenApplyingChanges { get; }

        private readonly string _workspaceKind;
        private readonly bool _supportsLspMutation;

        internal TestWorkspace(
            TestComposition? composition = null,
            string? workspaceKind = WorkspaceKind.Host,
            Guid solutionTelemetryId = default,
            bool disablePartialSolutions = true,
            bool ignoreUnchangeableDocumentsWhenApplyingChanges = true,
            WorkspaceConfigurationOptions? configurationOptions = null,
            bool supportsLspMutation = false)
            : base(GetHostServices(ref composition, configurationOptions != null), workspaceKind ?? WorkspaceKind.Host)
        {
            this.Composition = composition;
            this.ExportProvider = composition.ExportProviderFactory.CreateExportProvider();

            var partialSolutionsTestHook = Services.GetRequiredService<IWorkspacePartialSolutionsTestHook>();
            partialSolutionsTestHook.IsPartialSolutionDisabled = disablePartialSolutions;

            // configure workspace before creating any solutions:
            if (configurationOptions != null)
            {
                var workspaceConfigurationService = GetService<TestWorkspaceConfigurationService>();
                workspaceConfigurationService.Options = configurationOptions.Value;
            }

            SetCurrentSolutionEx(CreateSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()).WithTelemetryId(solutionTelemetryId)));

            _workspaceKind = workspaceKind ?? WorkspaceKind.Host;
            this.Projects = [];
            this.Documents = [];
            this.AdditionalDocuments = [];
            this.AnalyzerConfigDocuments = [];
            this.ProjectionDocuments = [];

            this.CanApplyChangeDocument = true;
            this.IgnoreUnchangeableDocumentsWhenApplyingChanges = ignoreUnchangeableDocumentsWhenApplyingChanges;
            _supportsLspMutation = supportsLspMutation;

            if (Services.GetService<INotificationService>() is INotificationServiceCallback callback)
            {
                // Avoid showing dialogs in tests by default
                callback.NotificationCallback = (message, title, severity) =>
                {
                    var severityText = severity switch
                    {
                        NotificationSeverity.Information => "💡",
                        NotificationSeverity.Warning => "⚠",
                        _ => "❌"
                    };

                    var fullMessage = string.IsNullOrEmpty(title)
                        ? message
                        : $"{title}:{Environment.NewLine}{Environment.NewLine}{message}";

                    throw new InvalidOperationException($"{severityText} {fullMessage}");
                };
            }
        }

        private static HostServices GetHostServices([NotNull] ref TestComposition? composition, bool hasWorkspaceConfigurationOptions)
        {
            composition ??= FeaturesTestCompositions.Features;

            if (hasWorkspaceConfigurationOptions)
            {
                composition = composition.AddParts(typeof(TestWorkspaceConfigurationService));
            }

            return composition.GetHostServices();
        }

        private protected abstract TDocument CreateDocument(
            string text = "",
            string displayName = "",
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            DocumentId? id = null,
            string? filePath = null,
            IReadOnlyList<string>? folders = null,
            ExportProvider? exportProvider = null,
            IDocumentServiceProvider? documentServiceProvider = null);

        private protected abstract TDocument CreateDocument(
            ExportProvider exportProvider,
            HostLanguageServices? languageServiceProvider,
            string code,
            string name,
            string filePath,
            int? cursorPosition,
            IDictionary<string, ImmutableArray<TextSpan>> spans,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            IReadOnlyList<string>? folders = null,
            bool isLinkFile = false,
            IDocumentServiceProvider? documentServiceProvider = null,
            ISourceGenerator? generator = null);

        private protected abstract TProject CreateProject(
            HostLanguageServices languageServices,
            CompilationOptions? compilationOptions,
            ParseOptions? parseOptions,
            string assemblyName,
            string projectName,
            IList<MetadataReference>? references,
            IList<TDocument> documents,
            IList<TDocument>? additionalDocuments = null,
            IList<TDocument>? analyzerConfigDocuments = null,
            Type? hostObjectType = null,
            bool isSubmission = false,
            string? filePath = null,
            IList<AnalyzerReference>? analyzerReferences = null,
            string? defaultNamespace = null);

        private protected abstract TSolution CreateSolution(TProject[] projects);

        public static string GetDefaultTestSourceDocumentName(int index, string extension)
           => "test" + (index + 1) + extension;

        public static string GetSourceFileExtension(string language, ParseOptions parseOptions)
        {
            if (language == LanguageNames.CSharp)
            {
                return parseOptions.Kind == SourceCodeKind.Regular
                    ? CSharpExtension
                    : CSharpScriptExtension;
            }
            else if (language == LanguageNames.VisualBasic)
            {
                return parseOptions.Kind == SourceCodeKind.Regular
                    ? VisualBasicExtension
                    : VisualBasicScriptExtension;
            }

            throw ExceptionUtilities.UnexpectedValue(language);
        }

        protected internal override bool PartialSemanticsEnabled => true;

        public TDocument DocumentWithCursor
            => Documents.Single(d => d.CursorPosition.HasValue && !d.IsLinkFile);

        public new void RegisterText(SourceTextContainer text)
            => base.RegisterText(text);

        internal void AddTestSolution(TSolution solution)
            => this.OnSolutionAdded(SolutionInfo.Create(solution.Id, solution.Version, solution.FilePath, projects: solution.Projects.Select(p => p.ToProjectInfo())));

        public void AddTestProject(TProject project)
        {
            if (!this.Projects.Contains(project))
            {
                this.Projects.Add(project);

                foreach (var doc in project.Documents)
                {
                    this.Documents.Add(doc);
                }

                foreach (var doc in project.AdditionalDocuments)
                {
                    this.AdditionalDocuments.Add(doc);
                }

                foreach (var doc in project.AnalyzerConfigDocuments)
                {
                    this.AnalyzerConfigDocuments.Add(doc);
                }
            }

            this.OnProjectAdded(project.ToProjectInfo());
        }

        public new void OnProjectRemoved(ProjectId projectId)
            => base.OnProjectRemoved(projectId);

        public new void OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
            => base.OnProjectReferenceAdded(projectId, projectReference);

        public new void OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
            => base.OnProjectReferenceRemoved(projectId, projectReference);

        public new void OnDocumentOpened(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext = true)
            => base.OnDocumentOpened(documentId, textContainer, isCurrentContext);

        public new void OnParseOptionsChanged(ProjectId projectId, ParseOptions parseOptions)
            => base.OnParseOptionsChanged(projectId, parseOptions);

        public new void OnAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
            => base.OnAnalyzerReferenceAdded(projectId, analyzerReference);

        public void OnDocumentRemoved(DocumentId documentId, bool closeDocument = false)
        {
            if (closeDocument && this.IsDocumentOpen(documentId))
            {
                this.CloseDocument(documentId);
            }

            base.OnDocumentRemoved(documentId);
        }

        public new void OnDocumentSourceCodeKindChanged(DocumentId documentId, SourceCodeKind sourceCodeKind)
            => base.OnDocumentSourceCodeKindChanged(documentId, sourceCodeKind);

        public DocumentId? GetDocumentId(TDocument hostDocument)
        {
            if (!Documents.Contains(hostDocument) &&
                !AdditionalDocuments.Contains(hostDocument) &&
                !AnalyzerConfigDocuments.Contains(hostDocument))
            {
                return null;
            }

            return hostDocument.Id;
        }

        public TDocument? GetTestDocument(DocumentId documentId)
            => this.Documents.FirstOrDefault(d => d.Id == documentId);

        public TDocument? GetTestAdditionalDocument(DocumentId documentId)
            => this.AdditionalDocuments.FirstOrDefault(d => d.Id == documentId);

        public TDocument? GetTestAnalyzerConfigDocument(DocumentId documentId)
            => this.AnalyzerConfigDocuments.FirstOrDefault(d => d.Id == documentId);

        public TestHostProject<TDocument>? GetTestProject(DocumentId documentId)
            => GetTestProject(documentId.ProjectId);

        public TestHostProject<TDocument>? GetTestProject(ProjectId projectId)
            => this.Projects.FirstOrDefault(p => p.Id == projectId);

        public TServiceInterface GetService<TServiceInterface>()
            => ExportProvider.GetExportedValue<TServiceInterface>();

        public TServiceInterface GetService<TServiceInterface>(string contentType)
        {
            var values = ExportProvider.GetExports<TServiceInterface, ContentTypeMetadata>();
            return values.Single(value => value.Metadata.ContentTypes.Contains(contentType)).Value;
        }

        public TServiceInterface GetService<TServiceInterface>(string contentType, string name)
        {
            var values = ExportProvider.GetExports<TServiceInterface, OrderableContentTypeMetadata>();
            return values.Single(value => value.Metadata.Name == name && value.Metadata.ContentTypes.Contains(contentType)).Value;
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.AddDocument:
                case ApplyChangesKind.RemoveDocument:
                    return KindSupportsAddRemoveDocument();

                case ApplyChangesKind.AddAdditionalDocument:
                case ApplyChangesKind.RemoveAdditionalDocument:
                case ApplyChangesKind.AddAnalyzerConfigDocument:
                case ApplyChangesKind.RemoveAnalyzerConfigDocument:
                case ApplyChangesKind.AddAnalyzerReference:
                case ApplyChangesKind.RemoveAnalyzerReference:
                case ApplyChangesKind.AddSolutionAnalyzerReference:
                case ApplyChangesKind.RemoveSolutionAnalyzerReference:
                    return true;

                case ApplyChangesKind.ChangeDocument:
                case ApplyChangesKind.ChangeAdditionalDocument:
                case ApplyChangesKind.ChangeAnalyzerConfigDocument:
                case ApplyChangesKind.ChangeDocumentInfo:
                    return this.CanApplyChangeDocument;

                case ApplyChangesKind.AddProjectReference:
                case ApplyChangesKind.AddMetadataReference:
                    return true;

                default:
                    return false;
            }
        }

        private bool KindSupportsAddRemoveDocument()
            => _workspaceKind switch
            {
                WorkspaceKind.MiscellaneousFiles => false,
                WorkspaceKind.Interactive => false,
                WorkspaceKind.SemanticSearch => false,
                _ => true
            };

        protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
        {
            var hostProject = this.GetTestProject(info.Id.ProjectId);
            Contract.ThrowIfNull(hostProject);

            var hostDocument = CreateDocument(
                text.ToString(), info.Name, info.SourceCodeKind,
                info.Id, info.FilePath, info.Folders, ExportProvider,
                info.DocumentServiceProvider);

            hostProject.AddDocument(hostDocument);
            this.OnDocumentAdded(hostDocument.ToDocumentInfo());
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
        {
            var hostProject = this.GetTestProject(documentId.ProjectId);
            Contract.ThrowIfNull(hostProject);

            var hostDocument = this.GetTestDocument(documentId);
            Contract.ThrowIfNull(hostDocument);

            hostProject.RemoveDocument(hostDocument);
            this.OnDocumentRemoved(documentId, closeDocument: true);
        }

        protected override void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text)
        {
            var hostProject = this.GetTestProject(info.Id.ProjectId);
            Contract.ThrowIfNull(hostProject);

            var hostDocument = CreateDocument(text.ToString(), info.Name, id: info.Id, exportProvider: ExportProvider);
            hostProject.AddAdditionalDocument(hostDocument);
            this.OnAdditionalDocumentAdded(hostDocument.ToDocumentInfo());
        }

        protected override void ApplyAdditionalDocumentRemoved(DocumentId documentId)
        {
            var hostProject = this.GetTestProject(documentId.ProjectId);
            Contract.ThrowIfNull(hostProject);

            var hostDocument = this.GetTestAdditionalDocument(documentId);
            if (hostDocument != null)
            {
                hostProject.RemoveAdditionalDocument(hostDocument);
            }

            this.OnAdditionalDocumentRemoved(documentId);
        }

        protected override void ApplyAnalyzerConfigDocumentAdded(DocumentInfo info, SourceText text)
        {
            var hostProject = this.GetTestProject(info.Id.ProjectId);
            Contract.ThrowIfNull(hostProject);

            var hostDocument = CreateDocument(text.ToString(), info.Name, id: info.Id, filePath: info.FilePath, folders: info.Folders, exportProvider: ExportProvider);
            hostProject.AddAnalyzerConfigDocument(hostDocument);
            this.OnAnalyzerConfigDocumentAdded(hostDocument.ToDocumentInfo());
        }

        protected override void ApplyAnalyzerConfigDocumentRemoved(DocumentId documentId)
        {
            var hostProject = this.GetTestProject(documentId.ProjectId);
            Contract.ThrowIfNull(hostProject);

            var hostDocument = this.GetTestAnalyzerConfigDocument(documentId);
            if (hostDocument != null)
            {
                hostProject.RemoveAnalyzerConfigDocument(hostDocument);
            }

            this.OnAnalyzerConfigDocumentRemoved(documentId);
        }

        protected override void ApplyProjectChanges(ProjectChanges projectChanges)
        {
            if (projectChanges.OldProject.FilePath != projectChanges.NewProject.FilePath)
            {
                var hostProject = this.GetTestProject(projectChanges.NewProject.Id);
                Contract.ThrowIfNull(hostProject);

                hostProject.OnProjectFilePathChanged(projectChanges.NewProject.FilePath);
                base.OnProjectNameChanged(projectChanges.NewProject.Id, projectChanges.NewProject.Name, projectChanges.NewProject.FilePath);
            }

            base.ApplyProjectChanges(projectChanges);
        }

        internal override void SetDocumentContext(DocumentId documentId)
            => OnDocumentContextUpdated(documentId);

        /// <summary>
        /// Overriding base impl so that when we close a document it goes back to the initial state when the test
        /// workspace was loaded, throwing away any changes made to the open version.
        /// </summary>
        internal override ValueTask TryOnDocumentClosedAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(this._supportsLspMutation);

            var testDocument = this.GetTestDocument(documentId);
            Contract.ThrowIfNull(testDocument);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);

            this.OnDocumentClosedEx(documentId, testDocument.Loader, requireDocumentPresentAndOpen: false);
            return ValueTaskFactory.CompletedTask;
        }

        public override void CloseDocument(DocumentId documentId)
        {
            var testDocument = this.GetTestDocument(documentId);
            Contract.ThrowIfNull(testDocument);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);
            Contract.ThrowIfFalse(IsDocumentOpen(documentId));

            this.OnDocumentClosed(documentId, testDocument.Loader);
        }

        public override void CloseAdditionalDocument(DocumentId documentId)
        {
            var testDocument = this.GetTestAdditionalDocument(documentId);
            Contract.ThrowIfNull(testDocument);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);
            Contract.ThrowIfFalse(IsDocumentOpen(documentId));

            this.OnAdditionalDocumentClosed(documentId, testDocument.Loader);
        }

        public override void CloseAnalyzerConfigDocument(DocumentId documentId)
        {
            var testDocument = this.GetTestAnalyzerConfigDocument(documentId);
            Contract.ThrowIfNull(testDocument);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);
            Contract.ThrowIfFalse(IsDocumentOpen(documentId));

            this.OnAnalyzerConfigDocumentClosed(documentId, testDocument.Loader);
        }

        public async Task CloseSourceGeneratedDocumentAsync(DocumentId documentId)
        {
            var testDocument = this.GetTestDocument(documentId);
            Contract.ThrowIfNull(testDocument);
            Contract.ThrowIfFalse(testDocument.IsSourceGenerated);
            Contract.ThrowIfFalse(IsDocumentOpen(documentId));

            var document = await CurrentSolution.GetSourceGeneratedDocumentAsync(documentId, CancellationToken.None);
            Contract.ThrowIfNull(document);
            OnSourceGeneratedDocumentClosed(document);
        }

        public Task ChangeDocumentAsync(DocumentId documentId, SourceText text)
        {
            return ChangeDocumentAsync(documentId, this.CurrentSolution.WithDocumentText(documentId, text));
        }

        public Task ChangeDocumentAsync(DocumentId documentId, Solution solution)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(solution);

            return this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId.ProjectId, documentId);
        }

        public Task AddDocumentAsync(DocumentInfo documentInfo)
        {
            var documentId = documentInfo.Id;

            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(this.CurrentSolution.AddDocument(documentInfo));

            return this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentAdded, oldSolution, newSolution, documentId: documentId);
        }

        public void ChangeAdditionalDocument(DocumentId documentId, SourceText text)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(this.CurrentSolution.WithAdditionalDocumentText(documentId, text));

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AdditionalDocumentChanged, oldSolution, newSolution, documentId.ProjectId, documentId);
        }

        public void ChangeAnalyzerConfigDocument(DocumentId documentId, SourceText text)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(this.CurrentSolution.WithAnalyzerConfigDocumentText(documentId, text));

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AnalyzerConfigDocumentChanged, oldSolution, newSolution, documentId.ProjectId, documentId);
        }

        public Task ChangeProjectAsync(ProjectId projectId, Solution solution)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(solution);

            return this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, oldSolution, newSolution, projectId);
        }

        public new void ClearSolution()
            => base.ClearSolution();

        public Task ChangeSolutionAsync(Solution solution)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(solution);

            return this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
        }

        public override bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project)
            => true;

        internal override bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject)
        {
            // VisualStudioWorkspace asserts the main thread for this call, so do the same thing here to catch tests
            // that fail to account for this possibility.
            var threadingContext = ExportProvider.GetExportedValue<IThreadingContext>();
            Contract.ThrowIfFalse(threadingContext.HasMainThread && threadingContext.JoinableTaskContext.IsOnMainThread);
            return true;
        }

        internal void InitializeDocuments(
            string language,
            CompilationOptions? compilationOptions = null,
            ParseOptions? parseOptions = null,
            string[]? files = null,
            string[]? metadataReferences = null,
            string? extension = null,
            bool commonReferences = true,
            bool openDocuments = true,
            IDocumentServiceProvider? documentServiceProvider = null)
        {
            var workspaceElement = CreateWorkspaceElement(
                language,
                compilationOptions,
                parseOptions,
                files,
                sourceGeneratedFiles: [],
                metadataReferences,
                extension,
                commonReferences);

            InitializeDocuments(workspaceElement, openDocuments, documentServiceProvider);
        }

        internal void InitializeDocuments(
            XElement workspaceElement,
            bool openDocuments = true,
            IDocumentServiceProvider? documentServiceProvider = null)
        {
            if (workspaceElement.Name != WorkspaceElementName)
            {
                throw new ArgumentException();
            }

            var projectNameToTestHostProject = new Dictionary<string, TProject>();
            var projectElementToProjectName = new Dictionary<XElement, string>();
            var projectIdentifier = 0;
            var documentIdentifier = 0;

            foreach (var projectElement in workspaceElement.Elements(ProjectElementName))
            {
                var project = CreateProject(
                    workspaceElement,
                    projectElement,
                    ExportProvider,
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
                    Assert.True(document.IsLinkFile || documentFilePaths.Add(document.FilePath!));

                    Documents.Add(document);
                }
            }

            var submissions = CreateSubmissions(workspaceElement.Elements(SubmissionElementName), ExportProvider);

            foreach (var submission in submissions)
            {
                projectNameToTestHostProject.Add(submission.Name, submission);
                Documents.Add(submission.Documents.Single());
            }

            var solution = CreateSolution([.. projectNameToTestHostProject.Values]);
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

            if (openDocuments)
            {
                foreach (var project in projectNameToTestHostProject.Values)
                {
                    foreach (var document in project.Documents)
                    {
                        if (!document.IsSourceGenerated)
                        {
                            // This implicitly opens the document in the workspace by fetching the container.
                            document.Open();
                        }
                    }
                }
            }
        }

        private IList<TProject> CreateSubmissions(
            IEnumerable<XElement> submissionElements,
            ExportProvider exportProvider)
        {
            var submissions = new List<TProject>();
            var submissionIndex = 0;

            foreach (var submissionElement in submissionElements)
            {
                var submissionName = "Submission" + (submissionIndex++);

                var languageName = GetLanguage(submissionElement);

                // The document
                var markupCode = submissionElement.NormalizedValue();
                MarkupTestFile.GetPositionAndSpans(markupCode,
                    out var code, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> spans);

                var languageServices = Services.GetLanguageServices(languageName);

                // The project

                var document = CreateDocument(exportProvider, languageServices, code, submissionName, submissionName, cursorPosition, spans, SourceCodeKind.Script);
                var documents = new List<TDocument> { document };

                if (languageName == NoCompilationConstants.LanguageName)
                {
                    submissions.Add(
                        CreateProject(
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

                var metadataService = Services.GetRequiredService<IMetadataService>();
                var metadataResolver = RuntimeMetadataReferenceResolver.CreateCurrentPlatformResolver(fileReferenceProvider: metadataService.GetReference);
                var syntaxFactory = languageServices.GetRequiredService<ISyntaxTreeFactoryService>();
                var compilationFactory = languageServices.GetRequiredService<ICompilationFactoryService>();
                var compilationOptions = compilationFactory.GetDefaultCompilationOptions()
                    .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
                    .WithMetadataReferenceResolver(metadataResolver);

                var parseOptions = syntaxFactory.GetDefaultParseOptions().WithKind(SourceCodeKind.Script);

                var references = CreateCommonReferences(submissionElement);

                var project = CreateProject(
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

        public override bool TryApplyChanges(Solution newSolution)
        {
            var result = base.TryApplyChanges(newSolution);

            // Ensure that any in-memory analyzer references in this test workspace are known by the serializer service
            // so that we can validate OOP scenarios involving analyzers.
            foreach (var analyzer in this.CurrentSolution.AnalyzerReferences)
            {
                if (analyzer is AnalyzerImageReference analyzerImageReference)
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerImageReference);
#pragma warning restore CA1416 // Validate platform compatibility
                }
            }

            return result;
        }
    }
}
