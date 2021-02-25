﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.MetadataReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Roslyn.Utilities;
using VSLangProj;
using VSLangProj140;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// The Workspace for running inside Visual Studio.
    /// </summary>
    internal abstract partial class VisualStudioWorkspaceImpl : VisualStudioWorkspace
    {
        private static readonly IntPtr s_docDataExisting_Unknown = new(-1);
        private const string AppCodeFolderName = "App_Code";

        private readonly IThreadingContext _threadingContext;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        private readonly Lazy<VisualStudioProjectFactory> _projectFactory;

        private readonly ITextBufferCloneService _textBufferCloneService;

        private readonly object _gate = new();

        /// <summary>
        /// A <see cref="ForegroundThreadAffinitizedObject"/> to make assertions that stuff is on the right thread.
        /// </summary>
        private readonly ForegroundThreadAffinitizedObject _foregroundObject;

        private ImmutableDictionary<ProjectId, IVsHierarchy?> _projectToHierarchyMap = ImmutableDictionary<ProjectId, IVsHierarchy?>.Empty;
        private ImmutableDictionary<ProjectId, Guid> _projectToGuidMap = ImmutableDictionary<ProjectId, Guid>.Empty;
        private readonly Dictionary<ProjectId, string?> _projectToMaxSupportedLangVersionMap = new();
        private readonly Dictionary<ProjectId, string> _projectToDependencyNodeTargetIdentifier = new();

        /// <summary>
        /// A map to fetch the path to a rule set file for a project. This right now is only used to implement
        /// <see cref="TryGetRuleSetPathForProject(ProjectId)"/> and any other use is extremely suspicious, since direct use of this is out of
        /// sync with the Workspace if there is active batching happening.
        /// </summary>
        private readonly Dictionary<ProjectId, Func<string?>> _projectToRuleSetFilePath = new();

        private readonly Dictionary<string, List<VisualStudioProject>> _projectSystemNameToProjectsMap = new();

        private readonly Dictionary<string, UIContext?> _languageToProjectExistsUIContext = new();

        /// <summary>
        /// A set of documents that were added by <see cref="VisualStudioProject.AddSourceTextContainer"/>, and aren't otherwise
        /// tracked for opening/closing.
        /// </summary>
        private ImmutableHashSet<DocumentId> _documentsNotFromFiles = ImmutableHashSet<DocumentId>.Empty;

        /// <summary>
        /// Indicates whether the current solution is closing.
        /// </summary>
        private bool _solutionClosing;

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal VisualStudioProjectTracker? _projectTracker;

        private OpenFileTracker? _openFileTracker;
        internal FileChangeWatcher FileChangeWatcher { get; }
        internal FileWatchedPortableExecutableReferenceFactory FileWatchedReferenceFactory { get; }

        private readonly Lazy<IProjectCodeModelFactory> _projectCodeModelFactory;
        private readonly IEnumerable<Lazy<IDocumentOptionsProviderFactory, OrderableMetadata>> _documentOptionsProviderFactories;
        private bool _documentOptionsProvidersInitialized = false;

        private readonly Lazy<ExternalErrorDiagnosticUpdateSource> _lazyExternalErrorDiagnosticUpdateSource;
        private bool _isExternalErrorDiagnosticUpdateSourceSubscribedToSolutionBuildEvents;

        public VisualStudioWorkspaceImpl(ExportProvider exportProvider, IAsyncServiceProvider asyncServiceProvider)
            : base(VisualStudioMefHostServices.Create(exportProvider))
        {
            _threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
            _textBufferCloneService = exportProvider.GetExportedValue<ITextBufferCloneService>();
            _textBufferFactoryService = exportProvider.GetExportedValue<ITextBufferFactoryService>();
            _projectionBufferFactoryService = exportProvider.GetExportedValue<IProjectionBufferFactoryService>();
            _projectCodeModelFactory = exportProvider.GetExport<IProjectCodeModelFactory>();
            _documentOptionsProviderFactories = exportProvider.GetExports<IDocumentOptionsProviderFactory, OrderableMetadata>();

            // We fetch this lazily because VisualStudioProjectFactory depends on VisualStudioWorkspaceImpl -- we have a circularity. Since this
            // exists right now as a compat shim, we'll just do this.
#pragma warning disable CS0618 // Type or member is obsolete
            _projectFactory = exportProvider.GetExport<VisualStudioProjectFactory>();
#pragma warning restore CS0618 // Type or member is obsolete

            _foregroundObject = new ForegroundThreadAffinitizedObject(_threadingContext);

            _textBufferFactoryService.TextBufferCreated += AddTextBufferCloneServiceToBuffer;
            _projectionBufferFactoryService.ProjectionBufferCreated += AddTextBufferCloneServiceToBuffer;

            _ = Task.Run(() => InitializeUIAffinitizedServicesAsync(asyncServiceProvider));

            FileChangeWatcher = exportProvider.GetExportedValue<FileChangeWatcherProvider>().Watcher;
            FileWatchedReferenceFactory = exportProvider.GetExportedValue<FileWatchedPortableExecutableReferenceFactory>();

            FileWatchedReferenceFactory.ReferenceChanged += this.RefreshMetadataReferencesForFile;

            _lazyExternalErrorDiagnosticUpdateSource = new Lazy<ExternalErrorDiagnosticUpdateSource>(() =>
                new ExternalErrorDiagnosticUpdateSource(
                    this,
                    exportProvider.GetExportedValue<IDiagnosticAnalyzerService>(),
                    exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>(),
                    exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>(),
                    _threadingContext), isThreadSafe: true);
        }

        internal ExternalErrorDiagnosticUpdateSource ExternalErrorDiagnosticUpdateSource => _lazyExternalErrorDiagnosticUpdateSource.Value;

        internal void SubscribeExternalErrorDiagnosticUpdateSourceToSolutionBuildEvents()
        {
            _foregroundObject.AssertIsForeground();

            if (_isExternalErrorDiagnosticUpdateSourceSubscribedToSolutionBuildEvents)
            {
                return;
            }

            // TODO: https://github.com/dotnet/roslyn/issues/36065
            // UIContextImpl requires IVsMonitorSelection service:
            if (ServiceProvider.GlobalProvider.GetService(typeof(IVsMonitorSelection)) == null)
            {
                return;
            }

            // This pattern ensures that we are called whenever the build starts/completes even if it is already in progress.
            KnownUIContexts.SolutionBuildingContext.WhenActivated(() =>
            {
                KnownUIContexts.SolutionBuildingContext.UIContextChanged += (object _, UIContextChangedEventArgs e) =>
                {
                    if (e.Activated)
                    {
                        ExternalErrorDiagnosticUpdateSource.OnSolutionBuildStarted();
                    }
                    else
                    {
                        ExternalErrorDiagnosticUpdateSource.OnSolutionBuildCompleted();
                    }
                };

                ExternalErrorDiagnosticUpdateSource.OnSolutionBuildStarted();
            });

            _isExternalErrorDiagnosticUpdateSourceSubscribedToSolutionBuildEvents = true;
        }

        public async Task InitializeUIAffinitizedServicesAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            // Create services that are bound to the UI thread
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);

            var solutionClosingContext = UIContext.FromUIContextGuid(VSConstants.UICONTEXT.SolutionClosing_guid);
            solutionClosingContext.UIContextChanged += (_, e) => _solutionClosing = e.Activated;

            var openFileTracker = await OpenFileTracker.CreateAsync(this, asyncServiceProvider).ConfigureAwait(true);

            // Update our fields first, so any asynchronous work that needs to use these is able to see the service.
            lock (_gate)
            {
                _openFileTracker = openFileTracker;
            }

            openFileTracker.ProcessQueuedWorkOnUIThread();
        }

        public void QueueCheckForFilesBeingOpen(ImmutableArray<string> newFileNames)
            => _openFileTracker?.QueueCheckForFilesBeingOpen(newFileNames);

        public void ProcessQueuedWorkOnUIThread()
            => _openFileTracker?.ProcessQueuedWorkOnUIThread();

        internal void AddProjectToInternalMaps(VisualStudioProject project, IVsHierarchy? hierarchy, Guid guid, string projectSystemName)
        {
            lock (_gate)
            {
                _projectToHierarchyMap = _projectToHierarchyMap.Add(project.Id, hierarchy);
                _projectToGuidMap = _projectToGuidMap.Add(project.Id, guid);
                _projectSystemNameToProjectsMap.MultiAdd(projectSystemName, project);
            }
        }

        internal void AddProjectRuleSetFileToInternalMaps(VisualStudioProject project, Func<string?> ruleSetFilePathFunc)
        {
            lock (_gate)
            {
                _projectToRuleSetFilePath.Add(project.Id, ruleSetFilePathFunc);
            }
        }

        internal void AddDocumentToDocumentsNotFromFiles(DocumentId documentId)
        {
            lock (_gate)
            {
                _documentsNotFromFiles = _documentsNotFromFiles.Add(documentId);
            }
        }

        internal void RemoveDocumentToDocumentsNotFromFiles(DocumentId documentId)
        {
            lock (_gate)
            {
                _documentsNotFromFiles = _documentsNotFromFiles.Remove(documentId);
            }
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal VisualStudioProjectTracker GetProjectTrackerAndInitializeIfNecessary()
        {
            if (_projectTracker == null)
            {
                _projectTracker = new VisualStudioProjectTracker(this, _projectFactory.Value, _threadingContext);
            }

            return _projectTracker;
        }

        [Obsolete("This is a compatibility shim for TypeScript and F#; please do not use it.")]
        internal VisualStudioProjectTracker ProjectTracker
        {
            get
            {
                return GetProjectTrackerAndInitializeIfNecessary();
            }
        }

        internal ContainedDocument? TryGetContainedDocument(DocumentId documentId)
        {
            // TODO: move everybody off of this instance method and replace them with calls to
            // ContainedDocument.TryGetContainedDocument
            return ContainedDocument.TryGetContainedDocument(documentId);
        }

        internal VisualStudioProject? GetProjectWithHierarchyAndName(IVsHierarchy hierarchy, string projectName)
        {
            lock (_gate)
            {
                if (_projectSystemNameToProjectsMap.TryGetValue(projectName, out var projects))
                {
                    foreach (var project in projects)
                    {
                        if (_projectToHierarchyMap.TryGetValue(project.Id, out var projectHierarchy))
                        {
                            if (projectHierarchy == hierarchy)
                            {
                                return project;
                            }
                        }
                    }
                }
            }

            return null;
        }

        // TODO: consider whether this should be going to the project system directly to get this path. This is only called on interactions from the
        // Solution Explorer in the SolutionExplorerShim, where if we just could more directly get to the rule set file it'd simplify this.
        internal override string? TryGetRuleSetPathForProject(ProjectId projectId)
        {
            lock (_gate)
            {
                if (_projectToRuleSetFilePath.TryGetValue(projectId, out var ruleSetPathFunc))
                {
                    return ruleSetPathFunc();
                }
                else
                {
                    return null;
                }
            }
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal IVisualStudioHostDocument? GetHostDocument(DocumentId documentId)
        {
            // TypeScript only calls this to immediately check if the document is a ContainedDocument. Because of that we can just check for
            // ContainedDocuments
            return ContainedDocument.TryGetContainedDocument(documentId);
        }

        public override EnvDTE.FileCodeModel GetFileCodeModel(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            var documentFilePath = GetFilePath(documentId);
            if (documentFilePath == null)
            {
                throw new ArgumentException(ServicesVSResources.The_given_DocumentId_did_not_come_from_the_Visual_Studio_workspace, nameof(documentId));
            }

            return _projectCodeModelFactory.Value.GetOrCreateFileCodeModel(documentId.ProjectId, documentFilePath);
        }

        internal override bool TryApplyChanges(
            Microsoft.CodeAnalysis.Solution newSolution,
            IProgressTracker progressTracker)
        {
            if (!_foregroundObject.IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.VisualStudioWorkspace_TryApplyChanges_cannot_be_called_from_a_background_thread);
            }

            var currentSolution = this.CurrentSolution;
            var projectChanges = newSolution.GetChanges(currentSolution).GetProjectChanges().ToList();

            // first make sure we can edit the document we will be updating (check them out from source control, etc)
            var changedDocs = projectChanges.SelectMany(pd => pd.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true).Concat(pd.GetChangedAdditionalDocuments()))
                                            .Where(CanApplyChange).ToList();
            if (changedDocs.Count > 0)
            {
                this.EnsureEditableDocuments(changedDocs);
            }

            return base.TryApplyChanges(newSolution, progressTracker);

            bool CanApplyChange(DocumentId documentId)
            {
                var document = newSolution.GetDocument(documentId) ?? currentSolution.GetDocument(documentId);
                if (document == null)
                {
                    // we can have null if documentId is for additional files
                    return true;
                }

                return document.CanApplyChange();
            }
        }

        public override bool CanOpenDocuments
        {
            get
            {
                return true;
            }
        }

        internal override bool CanChangeActiveContextDocument
        {
            get
            {
                return true;
            }
        }

        internal bool IsCPSProject(CodeAnalysis.Project project)
            => IsCPSProject(project.Id);

        internal bool IsCPSProject(ProjectId projectId)
        {
            _foregroundObject.AssertIsForeground();

            if (this.TryGetHierarchy(projectId, out var hierarchy))
            {
                return hierarchy.IsCapabilityMatch("CPS");
            }

            return false;
        }

        internal bool IsPrimaryProject(ProjectId projectId)
        {
            lock (_gate)
            {
                foreach (var (_, projects) in this._projectSystemNameToProjectsMap)
                {
                    foreach (var project in projects)
                    {
                        if (project.Id == projectId)
                            return project.IsPrimary;
                    }
                }
            }

            return true;
        }

        protected override bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, CodeAnalysis.Project project)
            => project.LanguageServices.GetRequiredService<ICompilationOptionsChangingService>().CanApplyChange(oldOptions, newOptions);

        public override bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, CodeAnalysis.Project project)
        {
            _projectToMaxSupportedLangVersionMap.TryGetValue(project.Id, out var maxSupportLangVersion);

            return project.LanguageServices.GetRequiredService<IParseOptionsChangingService>().CanApplyChange(oldOptions, newOptions, maxSupportLangVersion);
        }

        private void AddTextBufferCloneServiceToBuffer(object sender, TextBufferCreatedEventArgs e)
            => e.TextBuffer.Properties.AddProperty(typeof(ITextBufferCloneService), _textBufferCloneService);

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.AddDocument:
                case ApplyChangesKind.RemoveDocument:
                case ApplyChangesKind.ChangeDocument:
                case ApplyChangesKind.AddMetadataReference:
                case ApplyChangesKind.RemoveMetadataReference:
                case ApplyChangesKind.AddProjectReference:
                case ApplyChangesKind.RemoveProjectReference:
                case ApplyChangesKind.AddAnalyzerReference:
                case ApplyChangesKind.RemoveAnalyzerReference:
                case ApplyChangesKind.AddAdditionalDocument:
                case ApplyChangesKind.RemoveAdditionalDocument:
                case ApplyChangesKind.ChangeAdditionalDocument:
                case ApplyChangesKind.ChangeCompilationOptions:
                case ApplyChangesKind.ChangeParseOptions:
                case ApplyChangesKind.ChangeDocumentInfo:
                case ApplyChangesKind.AddAnalyzerConfigDocument:
                case ApplyChangesKind.RemoveAnalyzerConfigDocument:
                case ApplyChangesKind.ChangeAnalyzerConfigDocument:
                case ApplyChangesKind.AddSolutionAnalyzerReference:
                case ApplyChangesKind.RemoveSolutionAnalyzerReference:
                    return true;

                default:
                    return false;
            }
        }

        private bool TryGetProjectData(ProjectId projectId, [NotNullWhen(returnValue: true)] out IVsHierarchy? hierarchy, [NotNullWhen(returnValue: true)] out EnvDTE.Project? project)
        {
            project = null;

            return
                this.TryGetHierarchy(projectId, out hierarchy) &&
                hierarchy.TryGetProject(out project);
        }

        internal void GetProjectData(ProjectId projectId, out IVsHierarchy hierarchy, out EnvDTE.Project project)
        {
            if (!TryGetProjectData(projectId, out hierarchy!, out project!))
            {
                throw new ArgumentException(string.Format(ServicesVSResources.Could_not_find_project_0, projectId));
            }
        }

        internal EnvDTE.Project? TryGetDTEProject(ProjectId projectId)
            => TryGetProjectData(projectId, out var _, out var project) ? project : null;

        internal bool TryAddReferenceToProject(ProjectId projectId, string assemblyName)
        {
            if (!TryGetProjectData(projectId, out _, out var project))
            {
                return false;
            }

            var vsProject = (VSProject)project.Object;
            try
            {
                vsProject.References.Add(assemblyName);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private string? GetAnalyzerPath(AnalyzerReference analyzerReference)
            => analyzerReference.FullPath;

        protected override void ApplyCompilationOptionsChanged(ProjectId projectId, CompilationOptions options)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var compilationOptionsService = CurrentSolution.GetRequiredProject(projectId).LanguageServices.GetRequiredService<ICompilationOptionsChangingService>();
            var storage = ProjectPropertyStorage.Create(TryGetDTEProject(projectId), ServiceProvider.GlobalProvider);
            compilationOptionsService.Apply(options, storage);
        }

        protected override void ApplyParseOptionsChanged(ProjectId projectId, ParseOptions options)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var parseOptionsService = CurrentSolution.GetRequiredProject(projectId).LanguageServices.GetRequiredService<IParseOptionsChangingService>();
            var storage = ProjectPropertyStorage.Create(TryGetDTEProject(projectId), ServiceProvider.GlobalProvider);
            parseOptionsService.Apply(options, storage);
        }

        protected override void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (analyzerReference == null)
            {
                throw new ArgumentNullException(nameof(analyzerReference));
            }

            GetProjectData(projectId, out _, out var project);

            var filePath = GetAnalyzerPath(analyzerReference);
            if (filePath != null)
            {
                var vsProject = (VSProject3)project.Object;
                vsProject.AnalyzerReferences.Add(filePath);
            }
        }

        protected override void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (analyzerReference == null)
            {
                throw new ArgumentNullException(nameof(analyzerReference));
            }

            GetProjectData(projectId, out _, out var project);

            var filePath = GetAnalyzerPath(analyzerReference);
            if (filePath != null)
            {
                var vsProject = (VSProject3)project.Object;
                vsProject.AnalyzerReferences.Remove(filePath);
            }
        }

        private string? GetMetadataPath(MetadataReference metadataReference)
        {
            if (metadataReference is PortableExecutableReference fileMetadata)
            {
                return fileMetadata.FilePath;
            }

            return null;
        }

        protected override void ApplyMetadataReferenceAdded(
            ProjectId projectId, MetadataReference metadataReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (metadataReference == null)
            {
                throw new ArgumentNullException(nameof(metadataReference));
            }

            GetProjectData(projectId, out _, out var project);

            var filePath = GetMetadataPath(metadataReference);
            if (filePath != null)
            {
                var vsProject = (VSProject)project.Object;
                vsProject.References.Add(filePath);

                var undoManager = TryGetUndoManager();
                undoManager?.Add(new RemoveMetadataReferenceUndoUnit(this, projectId, filePath));
            }
        }

        protected override void ApplyMetadataReferenceRemoved(
            ProjectId projectId, MetadataReference metadataReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (metadataReference == null)
            {
                throw new ArgumentNullException(nameof(metadataReference));
            }

            GetProjectData(projectId, out _, out var project);

            var filePath = GetMetadataPath(metadataReference);
            if (filePath != null)
            {
                var vsProject = (VSProject)project.Object;
                foreach (Reference reference in vsProject.References)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(reference.Path, filePath))
                    {
                        reference.Remove();
                        var undoManager = TryGetUndoManager();
                        undoManager?.Add(new AddMetadataReferenceUndoUnit(this, projectId, filePath));
                        break;
                    }
                }
            }
        }

        internal override void ApplyMappedFileChanges(SolutionChanges solutionChanges)
        {
            // Get the original text changes from all documents and call the span mapping service to get span mappings for the text changes.
            // Create mapped text changes using the mapped spans and original text changes' text.

            // Mappings for opened razor files are retrieved via the LSP client making a request to the razor server.
            // If we wait for the result on the UI thread, we will hit a bug in the LSP client that brings us to a code path
            // using ConfigureAwait(true).  This deadlocks as it then attempts to return to the UI thread which is already blocked by us.
            // Instead, we invoke this in JTF run which will mitigate deadlocks when the ConfigureAwait(true)
            // tries to switch back to the main thread in the LSP client.
            // Link to LSP client bug for ConfigureAwait(true) - https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1216657
            var mappedChanges = _threadingContext.JoinableTaskFactory.Run(() => GetMappedTextChanges(solutionChanges));

            // Group the mapped text changes by file, then apply all mapped text changes for the file.
            foreach (var changesForFile in mappedChanges)
            {
                // It doesn't matter which of the file's projectIds we pass to the invisible editor, so just pick the first.
                var projectId = changesForFile.Value.First().ProjectId;
                // Make sure we only take distinct changes - we'll have duplicates from different projects for linked files or multi-targeted files.
                var distinctTextChanges = changesForFile.Value.Select(change => change.TextChange).Distinct().ToImmutableArray();
                using var invisibleEditor = new InvisibleEditor(ServiceProvider.GlobalProvider, changesForFile.Key, GetHierarchy(projectId), needsSave: true, needsUndoDisabled: false);
                TextEditApplication.UpdateText(distinctTextChanges, invisibleEditor.TextBuffer, EditOptions.None);
            }

            return;

            async Task<MultiDictionary<string, (TextChange TextChange, ProjectId ProjectId)>> GetMappedTextChanges(SolutionChanges solutionChanges)
            {
                var filePathToMappedTextChanges = new MultiDictionary<string, (TextChange TextChange, ProjectId ProjectId)>();
                foreach (var projectChanges in solutionChanges.GetProjectChanges())
                {
                    foreach (var changedDocumentId in projectChanges.GetChangedDocuments())
                    {
                        var oldDocument = projectChanges.OldProject.GetRequiredDocument(changedDocumentId);
                        if (!ShouldApplyChangesToMappedDocuments(oldDocument, out var mappingService))
                        {
                            continue;
                        }

                        var newDocument = projectChanges.NewProject.GetRequiredDocument(changedDocumentId);
                        var textChanges = (await newDocument.GetTextChangesAsync(oldDocument, CancellationToken.None).ConfigureAwait(false)).ToImmutableArray();
                        var mappedSpanResults = await mappingService.MapSpansAsync(oldDocument, textChanges.Select(tc => tc.Span), CancellationToken.None).ConfigureAwait(false);

                        Contract.ThrowIfFalse(mappedSpanResults.Length == textChanges.Length);

                        for (var i = 0; i < mappedSpanResults.Length; i++)
                        {
                            // Only include changes that could be mapped.
                            var newText = textChanges[i].NewText;
                            if (!mappedSpanResults[i].IsDefault && newText != null)
                            {
                                var newTextChange = new TextChange(mappedSpanResults[i].Span, newText);
                                filePathToMappedTextChanges.Add(mappedSpanResults[i].FilePath, (newTextChange, projectChanges.ProjectId));
                            }
                        }
                    }
                }

                return filePathToMappedTextChanges;
            }

            bool ShouldApplyChangesToMappedDocuments(CodeAnalysis.Document document, [NotNullWhen(true)] out ISpanMappingService? spanMappingService)
            {
                spanMappingService = document.Services.GetService<ISpanMappingService>();
                // Only consider files that are mapped and that we are unable to apply changes to.
                // TODO - refactor how this is determined - https://github.com/dotnet/roslyn/issues/47908
                return spanMappingService != null && document?.CanApplyChange() == false;
            }
        }

        protected override void ApplyProjectReferenceAdded(
            ProjectId projectId, ProjectReference projectReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (projectReference == null)
            {
                throw new ArgumentNullException(nameof(projectReference));
            }

            GetProjectData(projectId, out _, out var project);
            GetProjectData(projectReference.ProjectId, out _, out var refProject);

            var vsProject = (VSProject)project.Object;
            vsProject.References.AddProject(refProject);

            var undoManager = TryGetUndoManager();
            undoManager?.Add(new RemoveProjectReferenceUndoUnit(
                this, projectId, projectReference.ProjectId));
        }

        private OleInterop.IOleUndoManager? TryGetUndoManager()
        {
            var documentTrackingService = this.Services.GetService<IDocumentTrackingService>();
            if (documentTrackingService != null)
            {
                var documentId = documentTrackingService.TryGetActiveDocument() ?? documentTrackingService.GetVisibleDocuments().FirstOrDefault();
                if (documentId != null)
                {
                    var composition = (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
                    var exportProvider = composition.DefaultExportProvider;
                    var editorAdaptersService = exportProvider.GetExportedValue<IVsEditorAdaptersFactoryService>();

                    return editorAdaptersService.TryGetUndoManager(this, documentId, CancellationToken.None);
                }
            }

            return null;
        }

        protected override void ApplyProjectReferenceRemoved(
            ProjectId projectId, ProjectReference projectReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (projectReference == null)
            {
                throw new ArgumentNullException(nameof(projectReference));
            }

            GetProjectData(projectId, out _, out var project);
            GetProjectData(projectReference.ProjectId, out _, out var refProject);

            var vsProject = (VSProject)project.Object;
            foreach (Reference reference in vsProject.References)
            {
                if (reference.SourceProject == refProject)
                {
                    reference.Remove();
                    var undoManager = TryGetUndoManager();
                    undoManager?.Add(new AddProjectReferenceUndoUnit(this, projectId, projectReference.ProjectId));
                }
            }
        }

        protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
            => AddDocumentCore(info, text, TextDocumentKind.Document);

        protected override void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text)
            => AddDocumentCore(info, text, TextDocumentKind.AdditionalDocument);

        protected override void ApplyAnalyzerConfigDocumentAdded(DocumentInfo info, SourceText text)
            => AddDocumentCore(info, text, TextDocumentKind.AnalyzerConfigDocument);

        private void AddDocumentCore(DocumentInfo info, SourceText initialText, TextDocumentKind documentKind)
        {
            GetProjectData(info.Id.ProjectId, out _, out var project);

            // If the first namespace name matches the name of the project, then we don't want to
            // generate a folder for that.  The project is implicitly a folder with that name.
            var folders = info.Folders.AsEnumerable();
            if (folders.FirstOrDefault() == project.Name)
            {
                folders = folders.Skip(1);
            }

            folders = FilterFolderForProjectType(project, folders);

            if (IsWebsite(project))
            {
                AddDocumentToFolder(project, info.Id, SpecializedCollections.SingletonEnumerable(AppCodeFolderName), info.Name, documentKind, initialText, info.FilePath);
            }
            else if (folders.Any())
            {
                AddDocumentToFolder(project, info.Id, folders, info.Name, documentKind, initialText, info.FilePath);
            }
            else
            {
                AddDocumentToProject(project, info.Id, info.Name, documentKind, initialText, info.FilePath);
            }

            var undoManager = TryGetUndoManager();

            switch (documentKind)
            {
                case TextDocumentKind.AdditionalDocument:
                    undoManager?.Add(new RemoveAdditionalDocumentUndoUnit(this, info.Id));
                    break;

                case TextDocumentKind.AnalyzerConfigDocument:
                    undoManager?.Add(new RemoveAnalyzerConfigDocumentUndoUnit(this, info.Id));
                    break;

                case TextDocumentKind.Document:
                    undoManager?.Add(new RemoveDocumentUndoUnit(this, info.Id));
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(documentKind);
            }
        }

        private bool IsWebsite(EnvDTE.Project project)
            => project.Kind == VsWebSite.PrjKind.prjKindVenusProject;

        private IEnumerable<string> FilterFolderForProjectType(EnvDTE.Project project, IEnumerable<string> folders)
        {
            foreach (var folder in folders)
            {
                var items = GetAllItems(project.ProjectItems);
                var folderItem = items.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Compare(p.Name, folder) == 0);
                if (folderItem == null || folderItem.Kind != EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                {
                    yield return folder;
                }
            }
        }

        private IEnumerable<ProjectItem> GetAllItems(ProjectItems projectItems)
        {
            if (projectItems == null)
            {
                return SpecializedCollections.EmptyEnumerable<ProjectItem>();
            }

            var items = projectItems.OfType<ProjectItem>();
            return items.Concat(items.SelectMany(i => GetAllItems(i.ProjectItems)));
        }

#if false
        protected override void AddExistingDocument(DocumentId documentId, string filePath, IEnumerable<string> folders)
        {
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            IVisualStudioHostProject hostProject;
            GetProjectData(documentId.ProjectId, out hostProject, out hierarchy, out project);

            // If the first namespace name matches the name of the project, then we don't want to
            // generate a folder for that.  The project is implicitly a folder with that name.
            if (folders.FirstOrDefault() == project.Name)
            {
                folders = folders.Skip(1);
            }

            var name = Path.GetFileName(filePath);

            if (folders.Any())
            {
                AddDocumentToFolder(hostProject, project, documentId, folders, name, SourceCodeKind.Regular, initialText: null, filePath: filePath);
            }
            else
            {
                AddDocumentToProject(hostProject, project, documentId, name, SourceCodeKind.Regular, initialText: null, filePath: filePath);
            }
        }
#endif

        private ProjectItem AddDocumentToProject(
            EnvDTE.Project project,
            DocumentId documentId,
            string documentName,
            TextDocumentKind documentKind,
            SourceText? initialText = null,
            string? filePath = null)
        {
            string? folderPath = null;
            if (filePath == null && !project.TryGetFullPath(out folderPath))
            {
                // TODO(cyrusn): Throw an appropriate exception here.
                throw new Exception(ServicesVSResources.Could_not_find_location_of_folder_on_disk);
            }

            return AddDocumentToProjectItems(project.ProjectItems, documentId, folderPath, documentName, initialText, filePath, documentKind);
        }

        private ProjectItem AddDocumentToFolder(
            EnvDTE.Project project,
            DocumentId documentId,
            IEnumerable<string> folders,
            string documentName,
            TextDocumentKind documentKind,
            SourceText? initialText = null,
            string? filePath = null)
        {
            var folder = project.FindOrCreateFolder(folders);

            string? folderPath = null;
            if (filePath == null && !folder.TryGetFullPath(out folderPath))
            {
                // TODO(cyrusn): Throw an appropriate exception here.
                throw new Exception(ServicesVSResources.Could_not_find_location_of_folder_on_disk);
            }

            return AddDocumentToProjectItems(folder.ProjectItems, documentId, folderPath, documentName, initialText, filePath, documentKind);
        }

        private ProjectItem AddDocumentToProjectItems(
            ProjectItems projectItems,
            DocumentId documentId,
            string? folderPath,
            string documentName,
            SourceText? initialText,
            string? filePath,
            TextDocumentKind documentKind)
        {
            if (filePath == null)
            {
                Contract.ThrowIfNull(folderPath, "If we didn't have a file path, then we expected a folder path to generate the file path from.");
                var baseName = Path.GetFileNameWithoutExtension(documentName);
                var extension = documentKind == TextDocumentKind.Document ? GetPreferredExtension(documentId) : Path.GetExtension(documentName);
                var uniqueName = projectItems.GetUniqueName(baseName, extension);
                filePath = Path.Combine(folderPath, uniqueName);
            }

            if (initialText != null)
            {
                using var writer = new StreamWriter(filePath, append: false, encoding: initialText.Encoding ?? Encoding.UTF8);
                initialText.Write(writer);
            }

            // TODO: restore document ID hinting -- we previously ensured that the AddFromFile will introduce the document ID being used here.
            // (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/677956)
            return projectItems.AddFromFile(filePath);
        }

        private void RemoveDocumentCore(DocumentId documentId, TextDocumentKind documentKind)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            var document = this.CurrentSolution.GetTextDocument(documentId);
            if (document != null)
            {
                var hierarchy = this.GetHierarchy(documentId.ProjectId);
                Contract.ThrowIfNull(hierarchy, "Removing files from projects without hierarchies are not supported.");

                var text = document.GetTextSynchronously(CancellationToken.None);

                Contract.ThrowIfNull(document.FilePath, "Removing files from projects that don't have file names are not supported.");
                var itemId = hierarchy.TryGetItemId(document.FilePath);
                if (itemId == (uint)VSConstants.VSITEMID.Nil)
                {
                    // it is no longer part of the solution
                    return;
                }

                var project = (IVsProject3)hierarchy;
                project.RemoveItem(0, itemId, out _);

                var undoManager = TryGetUndoManager();
                var docInfo = CreateDocumentInfoWithoutText(document);

                switch (documentKind)
                {
                    case TextDocumentKind.AdditionalDocument:
                        undoManager?.Add(new AddAdditionalDocumentUndoUnit(this, docInfo, text));
                        break;

                    case TextDocumentKind.AnalyzerConfigDocument:
                        undoManager?.Add(new AddAnalyzerConfigDocumentUndoUnit(this, docInfo, text));
                        break;

                    case TextDocumentKind.Document:
                        undoManager?.Add(new AddDocumentUndoUnit(this, docInfo, text));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(documentKind);
                }
            }
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
            => RemoveDocumentCore(documentId, TextDocumentKind.Document);

        protected override void ApplyAdditionalDocumentRemoved(DocumentId documentId)
            => RemoveDocumentCore(documentId, TextDocumentKind.AdditionalDocument);

        protected override void ApplyAnalyzerConfigDocumentRemoved(DocumentId documentId)
            => RemoveDocumentCore(documentId, TextDocumentKind.AnalyzerConfigDocument);

        public override void OpenDocument(DocumentId documentId, bool activate = true)
            => OpenDocumentCore(documentId, activate);

        public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
            => OpenDocumentCore(documentId, activate);

        public override void OpenAnalyzerConfigDocument(DocumentId documentId, bool activate = true)
            => OpenDocumentCore(documentId, activate);

        public override void CloseDocument(DocumentId documentId)
            => CloseDocumentCore(documentId);

        public override void CloseAdditionalDocument(DocumentId documentId)
            => CloseDocumentCore(documentId);

        public override void CloseAnalyzerConfigDocument(DocumentId documentId)
            => CloseDocumentCore(documentId);

        public void OpenDocumentCore(DocumentId documentId, bool activate = true)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (!_foregroundObject.IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.This_workspace_only_supports_opening_documents_on_the_UI_thread);
            }

            var document = this.CurrentSolution.GetTextDocument(documentId);
            if (document != null)
            {
                OpenDocumentFromPath(document.FilePath, document.Project.Id, activate);
            }
        }

        internal void OpenDocumentFromPath(string? filePath, ProjectId projectId, bool activate = true)
        {
            if (TryGetFrame(filePath, projectId, out var frame))
            {
                if (activate)
                {
                    frame.Show();
                }
                else
                {
                    frame.ShowNoActivate();
                }
            }
        }

        /// <summary>
        /// Opens a file and retrieves the window frame.
        /// </summary>
        /// <param name="filePath">the file path of the file to open.</param>
        /// <param name="projectId">used to retrieve the IVsHierarchy to ensure the file is opened in a matching context.</param>
        /// <param name="frame">the window frame.</param>
        /// <returns></returns>
        private bool TryGetFrame(string? filePath, ProjectId projectId, [NotNullWhen(returnValue: true)] out IVsWindowFrame? frame)
        {
            frame = null;

            if (filePath == null)
            {
                return false;
            }

            var hierarchy = GetHierarchy(projectId);
            var itemId = hierarchy?.TryGetItemId(filePath) ?? (uint)VSConstants.VSITEMID.Nil;
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // If the ItemId is Nil, then IVsProject would not be able to open the
                // document using its ItemId. Thus, we must use OpenDocumentViaProject, which only
                // depends on the file path.

                var openDocumentService = IServiceProviderExtensions.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(ServiceProvider.GlobalProvider);
                return ErrorHandler.Succeeded(openDocumentService.OpenDocumentViaProject(
                    filePath,
                    VSConstants.LOGVIEWID.TextView_guid,
                    out _,
                    out _,
                    out _,
                    out frame));
            }
            else
            {
                // If the ItemId is not Nil, then we should not call IVsUIShellDocument
                // .OpenDocumentViaProject here because that simply takes a file path and opens the
                // file within the context of the first project it finds. That would cause problems
                // if the document we're trying to open is actually a linked file in another
                // project. So, we get the project's hierarchy and open the document using its item
                // ID.

                // It's conceivable that IVsHierarchy might not implement IVsProject. However,
                // OpenDocumentViaProject itself relies upon this QI working, so it should be OK to
                // use here.

                return hierarchy is IVsProject vsProject &&
                    ErrorHandler.Succeeded(vsProject.OpenItem(itemId, VSConstants.LOGVIEWID.TextView_guid, s_docDataExisting_Unknown, out frame));
            }
        }

        public void CloseDocumentCore(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (this.IsDocumentOpen(documentId))
            {
                var filePath = this.GetFilePath(documentId);
                if (filePath != null)
                {
                    var openDocumentService = IServiceProviderExtensions.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(ServiceProvider.GlobalProvider);
                    if (ErrorHandler.Succeeded(openDocumentService.IsDocumentOpen(null, 0, filePath, Guid.Empty, 0, out _, null, out var frame, out _)))
                    {
                        // TODO: do we need save argument for CloseDocument?
                        frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                    }
                }
            }
        }

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText newText)
            => ApplyTextDocumentChange(documentId, newText);

        protected override void ApplyAdditionalDocumentTextChanged(DocumentId documentId, SourceText newText)
            => ApplyTextDocumentChange(documentId, newText);

        protected override void ApplyAnalyzerConfigDocumentTextChanged(DocumentId documentId, SourceText newText)
            => ApplyTextDocumentChange(documentId, newText);

        private void ApplyTextDocumentChange(DocumentId documentId, SourceText newText)
        {
            EnsureEditableDocuments(documentId);
            var containedDocument = TryGetContainedDocument(documentId);

            if (containedDocument != null)
            {
                containedDocument.UpdateText(newText);
            }
            else
            {
                if (IsDocumentOpen(documentId))
                {
                    var textBuffer = this.CurrentSolution.GetTextDocument(documentId)!.GetTextSynchronously(CancellationToken.None).Container.TryGetTextBuffer();

                    if (textBuffer != null)
                    {
                        TextEditApplication.UpdateText(newText, textBuffer, EditOptions.DefaultMinimalChange);
                        return;
                    }
                }

                // The document wasn't open in a normal way, so invisible editor time
                using var invisibleEditor = OpenInvisibleEditor(documentId);
                TextEditApplication.UpdateText(newText, invisibleEditor.TextBuffer, EditOptions.None);
            }
        }

        protected override void ApplyDocumentInfoChanged(DocumentId documentId, DocumentInfo updatedInfo)
        {
            var document = CurrentSolution.GetRequiredDocument(documentId);

            FailIfDocumentInfoChangesNotSupported(document, updatedInfo);

            if (document.Name != updatedInfo.Name)
            {
                GetProjectData(updatedInfo.Id.ProjectId, out var _, out var dteProject);

                if (document.FilePath == null)
                {
                    FatalError.ReportAndCatch(new Exception("Attempting to change the information of a document without a file path."));
                    return;
                }

                var projectItemForDocument = dteProject.FindItemByPath(document.FilePath, StringComparer.OrdinalIgnoreCase);

                if (projectItemForDocument == null)
                {
                    // TODO(https://github.com/dotnet/roslyn/issues/34276):
                    Debug.Fail("Attempting to change the name of a file in a Shared Project");
                    return;
                }

                // Must save the document first for things like Breakpoints to be preserved.
                // WORKAROUND: Check if the document needs to be saved before calling save. 
                // Should remove the if below and just call save() once 
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1163405
                // is fixed
                if (!projectItemForDocument.Saved)
                {
                    projectItemForDocument.Save();
                }

                var uniqueName = projectItemForDocument.Collection
                    .GetUniqueNameIgnoringProjectItem(
                        projectItemForDocument,
                        Path.GetFileNameWithoutExtension(updatedInfo.Name),
                        Path.GetExtension(updatedInfo.Name));

                // Get the current undoManager before any file renames/documentId changes happen
                var undoManager = TryGetUndoManager();

                // By setting this property, Visual Studio will perform the file rename, which 
                // will cause the workspace's current solution to update and will fire the 
                // necessary workspace changed events.
                projectItemForDocument.Name = uniqueName;

                if (projectItemForDocument.TryGetFullPath(out var newPath))
                {
                    undoManager?.Add(new RenameDocumentUndoUnit(this, uniqueName, document.Name, newPath));
                }
            }
        }

        /// <summary>
        /// The <see cref="VisualStudioWorkspace"/> currently supports only a subset of <see cref="DocumentInfo"/> 
        /// changes.
        /// </summary>
        private void FailIfDocumentInfoChangesNotSupported(CodeAnalysis.Document document, DocumentInfo updatedInfo)
        {
            if (document.SourceCodeKind != updatedInfo.SourceCodeKind)
            {
                throw new InvalidOperationException(
                    $"This Workspace does not support changing a document's {nameof(document.SourceCodeKind)}.");
            }

            if (document.FilePath != updatedInfo.FilePath)
            {
                throw new InvalidOperationException(
                    $"This Workspace does not support changing a document's {nameof(document.FilePath)}.");
            }

            if (document.Id != updatedInfo.Id)
            {
                throw new InvalidOperationException(
                    $"This Workspace does not support changing a document's {nameof(document.Id)}.");
            }

            if (document.Folders != updatedInfo.Folders)
            {
                throw new InvalidOperationException(
                    $"This Workspace does not support changing a document's {nameof(document.Folders)}.");
            }

            if (document.State.Attributes.IsGenerated != updatedInfo.IsGenerated)
            {
                throw new InvalidOperationException(
                    $"This Workspace does not support changing a document's {nameof(document.State.Attributes.IsGenerated)} state.");
            }
        }

        private string GetPreferredExtension(DocumentId documentId)
        {
            // No extension was provided.  Pick a good one based on the type of host project.
            return CurrentSolution.GetRequiredProject(documentId.ProjectId).Language switch
            {
                // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                //return sourceCodeKind == SourceCodeKind.Regular ? ".cs" : ".csx";
                LanguageNames.CSharp => ".cs",

                // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                //return sourceCodeKind == SourceCodeKind.Regular ? ".vb" : ".vbx";
                LanguageNames.VisualBasic => ".vb",
                _ => throw new InvalidOperationException(),
            };
        }

        public override IVsHierarchy? GetHierarchy(ProjectId projectId)
        {
            // This doesn't take a lock since _projectToHierarchyMap is immutable
            return _projectToHierarchyMap.GetValueOrDefault(projectId, defaultValue: null);
        }

        internal override Guid GetProjectGuid(ProjectId projectId)
        {
            // This doesn't take a lock since _projectToGuidMap is immutable
            return _projectToGuidMap.GetValueOrDefault(projectId, defaultValue: Guid.Empty);
        }

        internal string? TryGetDependencyNodeTargetIdentifier(ProjectId projectId)
        {
            lock (_gate)
            {
                return _projectToDependencyNodeTargetIdentifier.GetValueOrDefault(projectId, defaultValue: null);
            }
        }

        internal override void SetDocumentContext(DocumentId documentId)
        {
            _foregroundObject.AssertIsForeground();

            // Note: this method does not actually call into any workspace code here to change the workspace's context. The assumption is updating the running document table or
            // IVsHierarchies will raise the appropriate events which we are subscribed to.

            lock (_gate)
            {
                var hierarchy = GetHierarchy(documentId.ProjectId);
                if (hierarchy == null)
                {
                    // If we don't have a hierarchy then there's nothing we can do
                    return;
                }

                // The hierarchy might be supporting multitargeting; in that case, let's update the context. Unfortunately the IVsHierarchies that support this
                // don't necessarily let us read it first, so we have to fire-and-forget here.
                foreach (var (projectSystemName, projects) in _projectSystemNameToProjectsMap)
                {
                    if (projects.Any(p => p.Id == documentId.ProjectId))
                    {
                        hierarchy.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, projectSystemName);

                        // We've updated that property, but we still need to continue the rest of this process to ensure the Running Document Table is updated
                        // and any shared asset projects are also updated.
                        break;
                    }
                }

                var filePath = GetFilePath(documentId);
                if (filePath == null)
                {
                    return;
                }

                var itemId = hierarchy.TryGetItemId(filePath);
                if (itemId != VSConstants.VSITEMID_NIL)
                {
                    // Is this owned by a shared asset project? If so, we need to put the shared asset project into the running document table, and need to set the
                    // current hierarchy as the active context of that shared hierarchy. This is kept as a loop that we do multiple times in the case that you
                    // have multiple pointers. This used to be the case for multitargeting projects, but that was now handled by setting the active context property
                    // above. Some project systems out there might still be supporting it, so we'll support it too.
                    while (SharedProjectUtilities.TryGetItemInSharedAssetsProject(hierarchy, itemId, out var sharedHierarchy, out var sharedItemId) &&
                           hierarchy != sharedHierarchy)
                    {
                        // Ensure the shared context is set correctly
                        if (sharedHierarchy.GetActiveProjectContext() != hierarchy)
                        {
                            ErrorHandler.ThrowOnFailure(sharedHierarchy.SetActiveProjectContext(hierarchy));
                        }

                        // We now need to ensure the outer project is also set up
                        hierarchy = sharedHierarchy;
                        itemId = sharedItemId;
                    }
                }

                // Update the ownership of the file in the Running Document Table
                var project = (IVsProject3)hierarchy;
                project.TransferItem(filePath, filePath, punkWindowFrame: null);
            }
        }

        internal bool TryGetHierarchy(ProjectId projectId, [NotNullWhen(returnValue: true)] out IVsHierarchy? hierarchy)
        {
            hierarchy = this.GetHierarchy(projectId);
            return hierarchy != null;
        }

        protected override void Dispose(bool finalize)
        {
            if (!finalize)
            {
                _textBufferFactoryService.TextBufferCreated -= AddTextBufferCloneServiceToBuffer;
                _projectionBufferFactoryService.ProjectionBufferCreated -= AddTextBufferCloneServiceToBuffer;
                FileWatchedReferenceFactory.ReferenceChanged -= RefreshMetadataReferencesForFile;
            }

            base.Dispose(finalize);
        }

        public void EnsureEditableDocuments(IEnumerable<DocumentId> documents)
        {
            var queryEdit = (IVsQueryEditQuerySave2)ServiceProvider.GlobalProvider.GetService(typeof(SVsQueryEditQuerySave));

            // make sure given document id actually exist in current solution and the file is marked as supporting modifications
            // and actually has non null file path
            var fileNames = documents.Select(GetFilePath).ToArray();

            // TODO: meditate about the flags we can pass to this and decide what is most appropriate for Roslyn
            var result = queryEdit.QueryEditFiles(
                rgfQueryEdit: 0,
                cFiles: fileNames.Length,
                rgpszMkDocuments: fileNames,
                rgrgf: new uint[fileNames.Length],
                rgFileInfo: new VSQEQS_FILE_ATTRIBUTE_DATA[fileNames.Length],
                pfEditVerdict: out var editVerdict,
                prgfMoreInfo: out var editResultFlags);

            if (ErrorHandler.Failed(result) ||
                editVerdict != (uint)tagVSQueryEditResult.QER_EditOK)
            {
                throw new Exception("Unable to check out the files from source control.");
            }

            if ((editResultFlags & (uint)(tagVSQueryEditResultFlags2.QER_Changed | tagVSQueryEditResultFlags2.QER_Reloaded)) != 0)
            {
                throw new Exception("A file was reloaded during the source control checkout.");
            }
        }

        public void EnsureEditableDocuments(params DocumentId[] documents)
            => this.EnsureEditableDocuments((IEnumerable<DocumentId>)documents);

        internal override bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject)
        {
            _foregroundObject.AssertIsForeground();

            if (!TryGetHierarchy(referencingProject, out var referencingHierarchy) ||
                !TryGetHierarchy(referencedProject, out var referencedHierarchy))
            {
                // Couldn't even get a hierarchy for this project. So we have to assume
                // that adding a reference is disallowed.
                return false;
            }

            // First we have to see if either project disallows the reference being added.
            const int ContextFlags = (int)__VSQUERYFLAVORREFERENCESCONTEXT.VSQUERYFLAVORREFERENCESCONTEXT_RefreshReference;

            var canAddProjectReference = (uint)__VSREFERENCEQUERYRESULT.REFERENCE_UNKNOWN;
            var canBeReferenced = (uint)__VSREFERENCEQUERYRESULT.REFERENCE_UNKNOWN;

            if (referencingHierarchy is IVsProjectFlavorReferences3 referencingProjectFlavor3)
            {
                if (ErrorHandler.Failed(referencingProjectFlavor3.QueryAddProjectReferenceEx(referencedHierarchy, ContextFlags, out canAddProjectReference, out _)))
                {
                    // Something went wrong even trying to see if the reference would be allowed.
                    // Assume it won't be allowed.
                    return false;
                }

                if (canAddProjectReference == (uint)__VSREFERENCEQUERYRESULT.REFERENCE_DENY)
                {
                    // Adding this project reference is not allowed.
                    return false;
                }
            }

            if (referencedHierarchy is IVsProjectFlavorReferences3 referencedProjectFlavor3)
            {
                if (ErrorHandler.Failed(referencedProjectFlavor3.QueryCanBeReferencedEx(referencingHierarchy, ContextFlags, out canBeReferenced, out _)))
                {
                    // Something went wrong even trying to see if the reference would be allowed.
                    // Assume it won't be allowed.
                    return false;
                }

                if (canBeReferenced == (uint)__VSREFERENCEQUERYRESULT.REFERENCE_DENY)
                {
                    // Adding this project reference is not allowed.
                    return false;
                }
            }

            // Neither project denied the reference being added.  At this point, if either project
            // allows the reference to be added, and the other doesn't block it, then we can add
            // the reference.
            if (canAddProjectReference == (int)__VSREFERENCEQUERYRESULT.REFERENCE_ALLOW ||
                canBeReferenced == (int)__VSREFERENCEQUERYRESULT.REFERENCE_ALLOW)
            {
                return true;
            }

            // In both directions things are still unknown.  Fallback to the reference manager
            // to make the determination here.
            var referenceManager = (IVsReferenceManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsReferenceManager));
            if (referenceManager == null)
            {
                // Couldn't get the reference manager.  Have to assume it's not allowed.
                return false;
            }

            // As long as the reference manager does not deny things, then we allow the
            // reference to be added.
            var result = referenceManager.QueryCanReferenceProject(referencingHierarchy, referencedHierarchy);
            return result != (uint)__VSREFERENCEQUERYRESULT.REFERENCE_DENY;
        }

        /// <summary>
        /// Applies a single operation to the workspace. <paramref name="action"/> should be a call to one of the protected Workspace.On* methods.
        /// </summary>
        public void ApplyChangeToWorkspace(Action<Workspace> action)
        {
            lock (_gate)
            {
                action(this);
            }
        }

        /// <summary>
        /// Applies a solution transformation to the workspace and triggers workspace changed event for specified <paramref name="projectId"/>.
        /// The transformation shall only update the project of the solution with the specified <paramref name="projectId"/>.
        /// </summary>
        public void ApplyChangeToWorkspace(ProjectId projectId, Func<CodeAnalysis.Solution, CodeAnalysis.Solution> solutionTransformation)
        {
            lock (_gate)
            {
                SetCurrentSolution(solutionTransformation, WorkspaceChangeKind.ProjectChanged, projectId);
            }
        }

        /// <summary>
        /// Applies a change to the workspace that can do any number of project changes.
        /// </summary>
        /// <remarks>This is needed to synchronize with <see cref="ApplyChangeToWorkspace(Action{Workspace})" /> to avoid any races. This
        /// method could be moved down to the core Workspace layer and then could use the synchronization lock there.</remarks>
        public void ApplyBatchChangeToWorkspace(Func<CodeAnalysis.Solution, SolutionChangeAccumulator> mutation)
        {
            lock (_gate)
            {
                var oldSolution = this.CurrentSolution;
                var solutionChangeAccumulator = mutation(oldSolution);

                if (!solutionChangeAccumulator.HasChange)
                {
                    return;
                }

                foreach (var documentId in solutionChangeAccumulator.DocumentIdsRemoved)
                {
                    this.ClearDocumentData(documentId);
                }

                SetCurrentSolution(solutionChangeAccumulator.Solution);
                RaiseWorkspaceChangedEventAsync(
                    solutionChangeAccumulator.WorkspaceChangeKind,
                    oldSolution,
                    solutionChangeAccumulator.Solution,
                    solutionChangeAccumulator.WorkspaceChangeProjectId,
                    solutionChangeAccumulator.WorkspaceChangeDocumentId);
            }
        }

        private readonly Dictionary<ProjectId, ProjectReferenceInformation> _projectReferenceInfoMap = new();

        private ProjectReferenceInformation GetReferenceInfo_NoLock(ProjectId projectId)
        {
            Debug.Assert(Monitor.IsEntered(_gate));

            return _projectReferenceInfoMap.GetOrAdd(projectId, _ => new ProjectReferenceInformation());
        }

        protected internal override void OnProjectRemoved(ProjectId projectId)
        {
            lock (_gate)
            {
                var languageName = CurrentSolution.GetRequiredProject(projectId).Language;

                if (_projectReferenceInfoMap.TryGetValue(projectId, out var projectReferenceInfo))
                {
                    // If we still had any output paths, we'll want to remove them to cause conversion back to metadata references.
                    // The call below implicitly is modifying the collection we've fetched, so we'll make a copy.
                    foreach (var outputPath in projectReferenceInfo.OutputPaths.ToList())
                    {
                        RemoveProjectOutputPath(projectId, outputPath);
                    }

                    _projectReferenceInfoMap.Remove(projectId);
                }

                _projectToHierarchyMap = _projectToHierarchyMap.Remove(projectId);
                _projectToGuidMap = _projectToGuidMap.Remove(projectId);
                _projectToMaxSupportedLangVersionMap.Remove(projectId);
                _projectToDependencyNodeTargetIdentifier.Remove(projectId);
                _projectToRuleSetFilePath.Remove(projectId);

                foreach (var (projectName, projects) in _projectSystemNameToProjectsMap)
                {
                    if (projects.RemoveAll(p => p.Id == projectId) > 0)
                    {
                        if (projects.Count == 0)
                        {
                            _projectSystemNameToProjectsMap.Remove(projectName);
                        }

                        break;
                    }
                }

                base.OnProjectRemoved(projectId);

                // Try to update the UI context info.  But cancel that work if we're shutting down.
                _threadingContext.RunWithShutdownBlockAsync(async cancellationToken =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    RefreshProjectExistsUIContextForLanguage(languageName);
                });
            }
        }

        private sealed class ProjectReferenceInformation
        {
            public readonly List<string> OutputPaths = new();
            public readonly List<(string path, ProjectReference projectReference)> ConvertedProjectReferences = new List<(string path, ProjectReference)>();
        }

        /// <summary>
        /// A multimap from an output path to the project outputting to it. Ideally, this shouldn't ever
        /// actually be a true multimap, since we shouldn't have two projects outputting to the same path, but
        /// any bug by a project adding the wrong output path means we could end up with some duplication.
        /// In that case, we'll temporarily have two until (hopefully) somebody removes it.
        /// </summary>
        private readonly Dictionary<string, List<ProjectId>> _projectsByOutputPath = new(StringComparer.OrdinalIgnoreCase);

        public void AddProjectOutputPath(ProjectId projectId, string outputPath)
        {
            lock (_gate)
            {
                var projectReferenceInformation = GetReferenceInfo_NoLock(projectId);

                projectReferenceInformation.OutputPaths.Add(outputPath);
                _projectsByOutputPath.MultiAdd(outputPath, projectId);

                var projectsForOutputPath = _projectsByOutputPath[outputPath];
                var distinctProjectsForOutputPath = projectsForOutputPath.Distinct().ToList();

                // If we have exactly one, then we're definitely good to convert
                if (projectsForOutputPath.Count == 1)
                {
                    ConvertMetadataReferencesToProjectReferences_NoLock(projectId, outputPath);
                }
                else if (distinctProjectsForOutputPath.Count == 1)
                {
                    // The same project has multiple output paths that are the same. Any project would have already been converted
                    // by the prior add, so nothing further to do
                }
                else
                {
                    // We have more than one project outputting to the same path. This shouldn't happen but we'll convert back
                    // because now we don't know which project to reference.
                    foreach (var otherProjectId in projectsForOutputPath)
                    {
                        // We know that since we're adding a path to projectId and we're here that we couldn't have already
                        // had a converted reference to us, instead we need to convert things that are pointing to the project
                        // we're colliding with
                        if (otherProjectId != projectId)
                        {
                            ConvertProjectReferencesToMetadataReferences_NoLock(otherProjectId, outputPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to convert all metadata references to <paramref name="outputPath"/> to a project reference to <paramref name="projectId"/>.
        /// </summary>
        /// <param name="projectId">The <see cref="ProjectId"/> of the project that could be referenced in place of the output path.</param>
        /// <param name="outputPath">The output path to replace.</param>
        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/31306",
            Constraint = "Avoid calling " + nameof(CodeAnalysis.Solution.GetProject) + " to avoid realizing all projects.")]
        private void ConvertMetadataReferencesToProjectReferences_NoLock(ProjectId projectId, string outputPath)
        {
            Debug.Assert(Monitor.IsEntered(_gate));

            var modifiedSolution = this.CurrentSolution;
            using var _ = PooledHashSet<ProjectId>.GetInstance(out var projectIdsChanged);

            foreach (var projectIdToRetarget in this.CurrentSolution.ProjectIds)
            {
                if (CanConvertMetadataReferenceToProjectReference_NoLock(projectIdToRetarget, referencedProjectId: projectId))
                {
                    // PERF: call GetProjectState instead of GetProject, otherwise creating a new project might force all
                    // Project instances to get created.
                    foreach (PortableExecutableReference reference in modifiedSolution.GetProjectState(projectIdToRetarget)!.MetadataReferences)
                    {
                        if (string.Equals(reference.FilePath, outputPath, StringComparison.OrdinalIgnoreCase))
                        {
                            FileWatchedReferenceFactory.StopWatchingReference(reference);

                            var projectReference = new ProjectReference(projectId, reference.Properties.Aliases, reference.Properties.EmbedInteropTypes);
                            modifiedSolution = modifiedSolution.RemoveMetadataReference(projectIdToRetarget, reference)
                                                               .AddProjectReference(projectIdToRetarget, projectReference);

                            projectIdsChanged.Add(projectIdToRetarget);

                            GetReferenceInfo_NoLock(projectIdToRetarget).ConvertedProjectReferences.Add(
                                (reference.FilePath!, projectReference));

                            // We have converted one, but you could have more than one reference with different aliases
                            // that we need to convert, so we'll keep going
                        }
                    }
                }
            }

            SetSolutionAndRaiseWorkspaceChanged_NoLock(modifiedSolution, projectIdsChanged);
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/31306",
            Constraint = "Avoid calling " + nameof(CodeAnalysis.Solution.GetProject) + " to avoid realizing all projects.")]
        private bool CanConvertMetadataReferenceToProjectReference_NoLock(ProjectId projectIdWithMetadataReference, ProjectId referencedProjectId)
        {
            Debug.Assert(Monitor.IsEntered(_gate));

            // We can never make a project reference ourselves. This isn't a meaningful scenario, but if somebody does this by accident
            // we do want to throw exceptions.
            if (projectIdWithMetadataReference == referencedProjectId)
            {
                return false;
            }

            // PERF: call GetProjectState instead of GetProject, otherwise creating a new project might force all
            // Project instances to get created.
            var projectWithMetadataReference = CurrentSolution.GetProjectState(projectIdWithMetadataReference);
            var referencedProject = CurrentSolution.GetProjectState(referencedProjectId);

            Contract.ThrowIfNull(projectWithMetadataReference);
            Contract.ThrowIfNull(referencedProject);

            // We don't want to convert a metadata reference to a project reference if the project being referenced isn't something
            // we can create a Compilation for. For example, if we have a C# project, and it's referencing a F# project via a metadata reference
            // everything would be fine if we left it a metadata reference. Converting it to a project reference means we couldn't create a Compilation
            // anymore in the IDE, since the C# compilation would need to reference an F# compilation. F# projects referencing other F# projects though
            // do expect this to work, and so we'll always allow references through of the same language.
            if (projectWithMetadataReference.Language != referencedProject.Language)
            {
                if (projectWithMetadataReference.LanguageServices.GetService<ICompilationFactoryService>() != null &&
                    referencedProject.LanguageServices.GetService<ICompilationFactoryService>() == null)
                {
                    // We're referencing something that we can't create a compilation from something that can, so keep the metadata reference
                    return false;
                }
            }

            // If this is going to cause a circular reference, also disallow it
            if (CurrentSolution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(referencedProjectId).Contains(projectIdWithMetadataReference))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds all projects that had a project reference to <paramref name="projectId"/> and convert it back to a metadata reference.
        /// </summary>
        /// <param name="projectId">The <see cref="ProjectId"/> of the project being referenced.</param>
        /// <param name="outputPath">The output path of the given project to remove the link to.</param>
        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/37616",
            Constraint = "Update ConvertedProjectReferences in place to avoid duplicate list allocations.")]
        private void ConvertProjectReferencesToMetadataReferences_NoLock(ProjectId projectId, string outputPath)
        {
            Debug.Assert(Monitor.IsEntered(_gate));

            var modifiedSolution = this.CurrentSolution;
            using var _ = PooledHashSet<ProjectId>.GetInstance(out var projectIdsChanged);

            foreach (var projectIdToRetarget in this.CurrentSolution.ProjectIds)
            {
                var referenceInfo = GetReferenceInfo_NoLock(projectIdToRetarget);

                // Update ConvertedProjectReferences in place to avoid duplicate list allocations
                for (var i = 0; i < referenceInfo.ConvertedProjectReferences.Count; i++)
                {
                    var convertedReference = referenceInfo.ConvertedProjectReferences[i];

                    if (string.Equals(convertedReference.path, outputPath, StringComparison.OrdinalIgnoreCase) &&
                        convertedReference.projectReference.ProjectId == projectId)
                    {
                        var metadataReference =
                            FileWatchedReferenceFactory.CreateReferenceAndStartWatchingFile(
                                convertedReference.path,
                                new MetadataReferenceProperties(
                                    aliases: convertedReference.projectReference.Aliases,
                                    embedInteropTypes: convertedReference.projectReference.EmbedInteropTypes));

                        modifiedSolution = modifiedSolution.RemoveProjectReference(projectIdToRetarget, convertedReference.projectReference)
                                                           .AddMetadataReference(projectIdToRetarget, metadataReference);

                        projectIdsChanged.Add(projectIdToRetarget);

                        referenceInfo.ConvertedProjectReferences.RemoveAt(i);

                        // We have converted one, but you could have more than one reference with different aliases
                        // that we need to convert, so we'll keep going. Make sure to decrement the index so we don't
                        // skip any items.
                        i--;
                    }
                }
            }

            SetSolutionAndRaiseWorkspaceChanged_NoLock(modifiedSolution, projectIdsChanged);
        }

        public ProjectReference? TryCreateConvertedProjectReference_NoLock(ProjectId referencingProject, string path, MetadataReferenceProperties properties)
        {
            // Any conversion to or from project references must be done under the global workspace lock,
            // since that needs to be coordinated with updating all projects simultaneously.
            Debug.Assert(Monitor.IsEntered(_gate));

            if (_projectsByOutputPath.TryGetValue(path, out var ids) && ids.Distinct().Count() == 1)
            {
                var projectIdToReference = ids.First();

                if (CanConvertMetadataReferenceToProjectReference_NoLock(referencingProject, projectIdToReference))
                {
                    var projectReference = new ProjectReference(
                        projectIdToReference,
                        aliases: properties.Aliases,
                        embedInteropTypes: properties.EmbedInteropTypes);

                    GetReferenceInfo_NoLock(referencingProject).ConvertedProjectReferences.Add((path, projectReference));

                    return projectReference;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public ProjectReference? TryRemoveConvertedProjectReference_NoLock(ProjectId referencingProject, string path, MetadataReferenceProperties properties)
        {
            // Any conversion to or from project references must be done under the global workspace lock,
            // since that needs to be coordinated with updating all projects simultaneously.
            Debug.Assert(Monitor.IsEntered(_gate));

            var projectReferenceInformation = GetReferenceInfo_NoLock(referencingProject);
            foreach (var convertedProject in projectReferenceInformation.ConvertedProjectReferences)
            {
                if (convertedProject.path == path &&
                    convertedProject.projectReference.EmbedInteropTypes == properties.EmbedInteropTypes &&
                    convertedProject.projectReference.Aliases.SequenceEqual(properties.Aliases))
                {
                    projectReferenceInformation.ConvertedProjectReferences.Remove(convertedProject);
                    return convertedProject.projectReference;
                }
            }

            return null;
        }

        private void SetSolutionAndRaiseWorkspaceChanged_NoLock(CodeAnalysis.Solution modifiedSolution, ICollection<ProjectId> projectIdsChanged)
        {
            if (projectIdsChanged.Count > 0)
            {
                Debug.Assert(modifiedSolution != CurrentSolution);

                var originalSolution = this.CurrentSolution;
                SetCurrentSolution(modifiedSolution);

                if (projectIdsChanged.Count == 1)
                {
                    RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, originalSolution, this.CurrentSolution, projectIdsChanged.Single());
                }
                else
                {
                    RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, originalSolution, this.CurrentSolution);
                }
            }
            else
            {
                // If they said nothing changed, than definitely nothing should have changed!
                Debug.Assert(modifiedSolution == CurrentSolution);
            }
        }

        public void RemoveProjectOutputPath(ProjectId projectId, string outputPath)
        {
            lock (_gate)
            {
                var projectReferenceInformation = GetReferenceInfo_NoLock(projectId);
                if (!projectReferenceInformation.OutputPaths.Contains(outputPath))
                {
                    throw new ArgumentException($"Project does not contain output path '{outputPath}'", nameof(outputPath));
                }

                projectReferenceInformation.OutputPaths.Remove(outputPath);
                _projectsByOutputPath.MultiRemove(outputPath, projectId);

                // When a project is closed, we may need to convert project references to metadata references (or vice
                // versa). Failure to convert the references could leave a project in the workspace with a project
                // reference to a project which is not open.
                //
                // For the specific case where the entire solution is closing, we do not need to update the state for
                // remaining projects as each project closes, because we know those projects will be closed without
                // further use. Avoiding reference conversion when the solution is closing improves performance for both
                // IDE close scenarios and solution reload scenarios that occur after complex branch switches.
                if (!_solutionClosing)
                {
                    if (_projectsByOutputPath.TryGetValue(outputPath, out var remainingProjectsForOutputPath))
                    {
                        var distinctRemainingProjects = remainingProjectsForOutputPath.Distinct();
                        if (distinctRemainingProjects.Count() == 1)
                        {
                            // We had more than one project outputting to the same path. Now we're back down to one
                            // so we can reference that one again
                            ConvertMetadataReferencesToProjectReferences_NoLock(distinctRemainingProjects.Single(), outputPath);
                        }
                    }
                    else
                    {
                        // No projects left, we need to convert back to metadata references
                        ConvertProjectReferencesToMetadataReferences_NoLock(projectId, outputPath);
                    }
                }
            }
        }

        private void RefreshMetadataReferencesForFile(object sender, string fullFilePath)
        {
            lock (_gate)
            {
                var newSolution = CurrentSolution;
                using var _ = PooledHashSet<ProjectId>.GetInstance(out var changedProjectIds);

                foreach (var project in CurrentSolution.Projects)
                {
                    // Loop to find each reference with the given path. It's possible that there might be multiple references of the same path;
                    // the project system could concievably add the same reference multiple times but with different aliases. It's also possible
                    // we might not find the path at all: when we receive the file changed event, we aren't checking if the file is still
                    // in the workspace at that time; it's possible it might have already been removed.
                    foreach (var portableExecutableReference in project.MetadataReferences.OfType<PortableExecutableReference>())
                    {
                        if (portableExecutableReference.FilePath == fullFilePath)
                        {
                            FileWatchedReferenceFactory.StopWatchingReference(portableExecutableReference);

                            var newPortableExecutableReference =
                                FileWatchedReferenceFactory.CreateReferenceAndStartWatchingFile(
                                    portableExecutableReference.FilePath,
                                    portableExecutableReference.Properties);

                            newSolution = newSolution.RemoveMetadataReference(project.Id, portableExecutableReference)
                                                     .AddMetadataReference(project.Id, newPortableExecutableReference);

                            changedProjectIds.Add(project.Id);
                        }
                    }
                }

                SetSolutionAndRaiseWorkspaceChanged_NoLock(newSolution, changedProjectIds);
            }
        }

        internal async Task EnsureDocumentOptionProvidersInitializedAsync(CancellationToken cancellationToken)
        {
            // HACK: switch to the UI thread, ensure we initialize our options provider which depends on a
            // UI-affinitized experimentation service
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _foregroundObject.AssertIsForeground();

            if (_documentOptionsProvidersInitialized)
            {
                return;
            }

            _documentOptionsProvidersInitialized = true;
            RegisterDocumentOptionProviders(_documentOptionsProviderFactories);
        }

        internal void SetMaxLanguageVersion(ProjectId projectId, string? maxLanguageVersion)
        {
            lock (_gate)
            {
                _projectToMaxSupportedLangVersionMap[projectId] = maxLanguageVersion;
            }
        }

        internal void SetDependencyNodeTargetIdentifier(ProjectId projectId, string targetIdentifier)
        {
            lock (_gate)
            {
                _projectToDependencyNodeTargetIdentifier[projectId] = targetIdentifier;
            }
        }

        internal void RefreshProjectExistsUIContextForLanguage(string language)
        {
            // We must assert the call is on the foreground as setting UIContext.IsActive would otherwise do a COM RPC.
            _foregroundObject.AssertIsForeground();

            lock (_gate)
            {
                var uiContext =
                    _languageToProjectExistsUIContext.GetOrAdd(
                        language,
                        l => Services.GetLanguageServices(l).GetService<IProjectExistsUIContextProviderLanguageService>()?.GetUIContext());

                // UIContexts can be "zombied" if UIContexts aren't supported because we're in a command line build or in other scenarios.
                if (uiContext != null && !uiContext.IsZombie)
                {
                    uiContext.IsActive = CurrentSolution.Projects.Any(p => p.Language == language);
                }
            }
        }
    }
}
