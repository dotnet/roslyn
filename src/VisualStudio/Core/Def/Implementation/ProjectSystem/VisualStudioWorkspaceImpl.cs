// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// The Workspace for running inside Visual Studio.
    /// </summary>
    internal abstract partial class VisualStudioWorkspaceImpl : VisualStudioWorkspace
    {
        private static readonly IntPtr s_docDataExisting_Unknown = new IntPtr(-1);
        private const string AppCodeFolderName = "App_Code";

        private readonly IThreadingContext _threadingContext;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        private readonly Lazy<VisualStudioProjectFactory> _projectFactory;

        private readonly ITextBufferCloneService _textBufferCloneService;

        // document worker coordinator
        private ISolutionCrawlerRegistrationService _registrationService;

        /// <summary>
        /// A <see cref="ForegroundThreadAffinitizedObject"/> to make assertions that stuff is on the right thread.
        /// </summary>
        private readonly ForegroundThreadAffinitizedObject _foregroundObject;

        private ImmutableDictionary<ProjectId, IVsHierarchy> _projectToHierarchyMap = ImmutableDictionary<ProjectId, IVsHierarchy>.Empty;
        private ImmutableDictionary<ProjectId, Guid> _projectToGuidMap = ImmutableDictionary<ProjectId, Guid>.Empty;
        private Dictionary<string, List<VisualStudioProject>> _projectSystemNameToProjectsMap = new Dictionary<string, List<VisualStudioProject>>();

        /// <summary>
        /// A set of documents that were added by <see cref="VisualStudioProject.AddSourceTextContainer"/>, and aren't otherwise
        /// tracked for opening/closing.
        /// </summary>
        private ImmutableHashSet<DocumentId> _documentsNotFromFiles = ImmutableHashSet<DocumentId>.Empty;

        internal VisualStudioProjectTracker _projectTracker;

        private OpenFileTracker _openFileTrackerOpt;
        internal FileChangeWatcher FileChangeWatcher { get; }

        public VisualStudioWorkspaceImpl(ExportProvider exportProvider, IAsyncServiceProvider asyncServiceProvider)
            : base(VisualStudioMefHostServices.Create(exportProvider))
        {
            _threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
            _textBufferCloneService = exportProvider.GetExportedValue<ITextBufferCloneService>();
            _textBufferFactoryService = exportProvider.GetExportedValue<ITextBufferFactoryService>();
            _projectionBufferFactoryService = exportProvider.GetExportedValue<IProjectionBufferFactoryService>();

            // We fetch this lazily because VisualStudioProjectFactory depends on VisualStudioWorkspaceImpl -- we have a circularity. Since this
            // exists right now as a compat shim, we'll just do this.
#pragma warning disable CS0618 // Type or member is obsolete
            _projectFactory = exportProvider.GetExport<VisualStudioProjectFactory>();
#pragma warning restore CS0618 // Type or member is obsolete

            _foregroundObject = new ForegroundThreadAffinitizedObject(_threadingContext);

            _textBufferFactoryService.TextBufferCreated += AddTextBufferCloneServiceToBuffer;
            _projectionBufferFactoryService.ProjectionBufferCreated += AddTextBufferCloneServiceToBuffer;
            exportProvider.GetExportedValue<PrimaryWorkspace>().Register(this);

            System.Threading.Tasks.Task.Run(() => ConnectToOpenFileTrackerOnUIThreadAsync(asyncServiceProvider));

            var fileChangeWatcherProvider = exportProvider.GetExportedValue<FileChangeWatcherProvider>();

            FileChangeWatcher = fileChangeWatcherProvider.Watcher;
            System.Threading.Tasks.Task.Run(async () =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var fileChangeService = (IVsFileChangeEx)ServiceProvider.GlobalProvider.GetService(typeof(SVsFileChangeEx));
                    fileChangeWatcherProvider.SetFileChangeService(fileChangeService);
                });
        }

        public async System.Threading.Tasks.Task ConnectToOpenFileTrackerOnUIThreadAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            // Create services that are bound to the UI thread
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

            var openFileTracker = await OpenFileTracker.CreateAsync(this, asyncServiceProvider).ConfigureAwait(true);

            // Update our fields first, so any asynchronous work that needs to use these is able to see the service.
            lock (_gate)
            {
                _openFileTrackerOpt = openFileTracker;
            }

            openFileTracker.CheckForOpenDocumentsByEnumeratingTheRunningDocumentTable();
        }

        public void CheckForOpenDocuments(ImmutableArray<string> newFileNames)
        {
            _openFileTrackerOpt?.CheckForFilesBeingOpen(newFileNames);
        }

        internal void AddProjectToInternalMaps(VisualStudioProject project, IVsHierarchy hierarchy, Guid guid, string projectSystemName)
        {
            lock (_gate)
            {
                _projectToHierarchyMap = _projectToHierarchyMap.Add(project.Id, hierarchy);
                _projectToGuidMap = _projectToGuidMap.Add(project.Id, guid);
                _projectSystemNameToProjectsMap.MultiAdd(projectSystemName, project);
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

        internal ContainedDocument TryGetContainedDocument(DocumentId documentId)
        {
            // TODO: move everybody off of this method
            return ContainedDocument.TryGetContainedDocument(documentId);
        }

        internal VisualStudioProject GetProjectWithHierarchyAndName(IVsHierarchy hierarchy, string projectName)
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

        [Obsolete("This is a compatibility shim for Live Unit Testing; please do not use it.")]
        internal AbstractProject GetHostProject(ProjectId projectId)
        {
            var project = CurrentSolution.GetProject(projectId);

            if (project == null)
            {
                return null;
            }

            return new StubProject(ProjectTracker, project, GetHierarchy(projectId), project.OutputFilePath);
        }

        private sealed class StubProject : AbstractProject
        {
            private readonly string _outputPath;

            public StubProject(VisualStudioProjectTracker projectTracker, CodeAnalysis.Project project, IVsHierarchy hierarchy, string outputPath)
                : base(projectTracker, null, project.Name + "_Stub", null, hierarchy, project.Language, Guid.Empty, null, null, null, null)
            {
                _outputPath = outputPath;
            }

            protected override string GetOutputFilePath()
            {
                return _outputPath;
            }
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal IVisualStudioHostDocument GetHostDocument(DocumentId documentId)
        {
            // TypeScript only calls this to immediately check if the document is a ContainedDocument. Because of that we can just check for
            // ContainedDocuments
            return ContainedDocument.TryGetContainedDocument(documentId);
        }

        internal override bool TryApplyChanges(
            Microsoft.CodeAnalysis.Solution newSolution,
            IProgressTracker progressTracker)
        {
            if (!ThreadHelper.JoinableTaskContext.IsOnMainThread)
            {
                throw new InvalidOperationException(ServicesVSResources.VisualStudioWorkspace_TryApplyChanges_cannot_be_called_from_a_background_thread);
            }

            var currentSolution = this.CurrentSolution;
            var projectChanges = newSolution.GetChanges(currentSolution).GetProjectChanges().ToList();

            var projectsToLoad = new HashSet<Guid>();
            foreach (var pc in projectChanges)
            {
                if (pc.GetAddedAdditionalDocuments().Any() ||
                    pc.GetAddedAnalyzerReferences().Any() ||
                    pc.GetAddedDocuments().Any() ||
                    pc.GetAddedMetadataReferences().Any() ||
                    pc.GetAddedProjectReferences().Any() ||
                    pc.GetRemovedAdditionalDocuments().Any() ||
                    pc.GetRemovedAnalyzerReferences().Any() ||
                    pc.GetRemovedDocuments().Any() ||
                    pc.GetRemovedMetadataReferences().Any() ||
                    pc.GetRemovedProjectReferences().Any())
                {
                    var projectGuid = GetProjectGuid(pc.ProjectId);

                    if (projectGuid != Guid.Empty)
                    {
                        projectsToLoad.Add(projectGuid);
                    }
                }
            }

            if (projectsToLoad.Any())
            {
                var vsSolution4 = (IVsSolution4)ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution));
                vsSolution4.EnsureProjectsAreLoaded(
                    (uint)projectsToLoad.Count,
                    projectsToLoad.ToArray(),
                    (uint)__VSBSLFLAGS.VSBSLFLAGS_None);
            }

            // first make sure we can edit the document we will be updating (check them out from source control, etc)
            var changedDocs = projectChanges.SelectMany(pd => pd.GetChangedDocuments(true).Concat(pd.GetChangedAdditionalDocuments())).Where(CanApplyChange).ToList();
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

        internal override bool CanRenameFilesDuringCodeActions(CodeAnalysis.Project project)
            => !IsCPSProject(project);

        internal bool IsCPSProject(CodeAnalysis.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.TryGetHierarchy(project.Id, out var hierarchy))
            {
                // Currently renaming files in CPS projects (i.e. .Net Core) doesn't work proprey.
                // This is because the remove/add of the documents in CPS is not synchronous
                // (despite the DTE interfaces being synchronous).  So Roslyn calls the methods
                // expecting the changes to happen immediately.  Because they are deferred in CPS
                // this causes problems.
                return hierarchy.IsCapabilityMatch("CPS");
            }

            return false;
        }

        protected override bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, CodeAnalysis.Project project)
        {
            var parseOptionsService = project.LanguageServices.GetService<IParseOptionsService>();
            if (parseOptionsService == null)
            {
                return false;
            }

            // Currently, only changes to the LanguageVersion of parse options are supported.
            var newLanguageVersion = parseOptionsService.GetLanguageVersion(newOptions);
            var updated = parseOptionsService.WithLanguageVersion(oldOptions, newLanguageVersion);

            return newOptions == updated;
        }

        private void AddTextBufferCloneServiceToBuffer(object sender, TextBufferCreatedEventArgs e)
        {
            e.TextBuffer.Properties.AddProperty(typeof(ITextBufferCloneService), _textBufferCloneService);
        }

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
                case ApplyChangesKind.ChangeParseOptions:
                    return true;

                default:
                    return false;
            }
        }

        private bool TryGetProjectData(ProjectId projectId, out IVsHierarchy hierarchy, out EnvDTE.Project project)
        {
            hierarchy = null;
            project = null;

            return
                this.TryGetHierarchy(projectId, out hierarchy) &&
                hierarchy.TryGetProject(out project);
        }

        internal void GetProjectData(ProjectId projectId, out IVsHierarchy hierarchy, out EnvDTE.Project project)
        {
            if (!TryGetProjectData(projectId, out hierarchy, out project))
            {
                throw new ArgumentException(string.Format(ServicesVSResources.Could_not_find_project_0, projectId));
            }
        }

        internal EnvDTE.Project TryGetDTEProject(ProjectId projectId)
        {
            return TryGetProjectData(projectId, out var hierarchy, out var project) ? project : null;
        }

        internal bool TryAddReferenceToProject(ProjectId projectId, string assemblyName)
        {
            EnvDTE.Project project;
            try
            {
                GetProjectData(projectId, out var hierarchy, out project);
            }
            catch (ArgumentException)
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

        private string GetAnalyzerPath(AnalyzerReference analyzerReference)
        {
            return analyzerReference.FullPath;
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

            var parseOptionsService = CurrentSolution.GetProject(projectId).LanguageServices.GetService<IParseOptionsService>();
            Contract.ThrowIfNull(parseOptionsService, nameof(parseOptionsService));

            string newVersion = parseOptionsService.GetLanguageVersion(options);

            GetProjectData(projectId, out var hierarchy, out var project);
            foreach (string configurationName in (object[])project.ConfigurationManager.ConfigurationRowNames)
            {
                switch (CurrentSolution.GetProject(projectId).Language)
                {
                    case LanguageNames.CSharp:
                        var csharpProperties = (VSLangProj80.CSharpProjectConfigurationProperties3)project.ConfigurationManager
                            .ConfigurationRow(configurationName).Item(1).Object;

                        if (newVersion != csharpProperties.LanguageVersion)
                        {
                            csharpProperties.LanguageVersion = newVersion;
                        }
                        break;

                    case LanguageNames.VisualBasic:
                        throw new InvalidOperationException(ServicesVSResources.This_workspace_does_not_support_updating_Visual_Basic_parse_options);
                }
            }
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

            GetProjectData(projectId, out var hierarchy, out var project);

            string filePath = GetAnalyzerPath(analyzerReference);
            if (filePath != null)
            {
                VSProject3 vsProject = (VSProject3)project.Object;
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

            GetProjectData(projectId, out var hierarchy, out var project);

            string filePath = GetAnalyzerPath(analyzerReference);
            if (filePath != null)
            {
                VSProject3 vsProject = (VSProject3)project.Object;
                vsProject.AnalyzerReferences.Remove(filePath);
            }
        }

        private string GetMetadataPath(MetadataReference metadataReference)
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

            GetProjectData(projectId, out var hierarchy, out var project);

            string filePath = GetMetadataPath(metadataReference);
            if (filePath != null)
            {
                VSProject vsProject = (VSProject)project.Object;
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

            GetProjectData(projectId, out var hierarchy, out var project);

            string filePath = GetMetadataPath(metadataReference);
            if (filePath != null)
            {
                VSProject vsProject = (VSProject)project.Object;
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

            GetProjectData(projectId, out var hierarchy, out var project);
            GetProjectData(projectReference.ProjectId, out var refHierarchy, out var refProject);

            var vsProject = (VSProject)project.Object;
            vsProject.References.AddProject(refProject);

            var undoManager = TryGetUndoManager();
            undoManager?.Add(new RemoveProjectReferenceUndoUnit(
                this, projectId, projectReference.ProjectId));
        }

        private OleInterop.IOleUndoManager TryGetUndoManager()
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

            GetProjectData(projectId, out var hierarchy, out var project);
            GetProjectData(projectReference.ProjectId, out var refHierarchy, out var refProject);

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
        {
            AddDocumentCore(info, text, isAdditionalDocument: false);
        }

        protected override void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text)
        {
            AddDocumentCore(info, text, isAdditionalDocument: true);
        }

        private void AddDocumentCore(DocumentInfo info, SourceText initialText, bool isAdditionalDocument)
        {
            GetProjectData(info.Id.ProjectId, out var hierarchy, out var project);

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
                AddDocumentToFolder(project, info.Id, SpecializedCollections.SingletonEnumerable(AppCodeFolderName), info.Name, info.SourceCodeKind, initialText, isAdditionalDocument: isAdditionalDocument, filePath: info.FilePath);
            }
            else if (folders.Any())
            {
                AddDocumentToFolder(project, info.Id, folders, info.Name, info.SourceCodeKind, initialText, isAdditionalDocument: isAdditionalDocument, filePath: info.FilePath);
            }
            else
            {
                AddDocumentToProject(project, info.Id, info.Name, info.SourceCodeKind, initialText, isAdditionalDocument: isAdditionalDocument, filePath: info.FilePath);
            }

            var undoManager = TryGetUndoManager();

            if (isAdditionalDocument)
            {
                undoManager?.Add(new RemoveAdditionalDocumentUndoUnit(this, info.Id));
            }
            else
            {
                undoManager?.Add(new RemoveDocumentUndoUnit(this, info.Id));
            }
        }

        private bool IsWebsite(EnvDTE.Project project)
        {
            return project.Kind == VsWebSite.PrjKind.prjKindVenusProject;
        }

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
            SourceCodeKind sourceCodeKind,
            SourceText initialText = null,
            string filePath = null,
            bool isAdditionalDocument = false)
        {
            string folderPath = null;
            if (filePath == null && !project.TryGetFullPath(out folderPath))
            {
                // TODO(cyrusn): Throw an appropriate exception here.
                throw new Exception(ServicesVSResources.Could_not_find_location_of_folder_on_disk);
            }

            return AddDocumentToProjectItems(project.ProjectItems, documentId, folderPath, documentName, sourceCodeKind, initialText, filePath, isAdditionalDocument);
        }

        private ProjectItem AddDocumentToFolder(
            EnvDTE.Project project,
            DocumentId documentId,
            IEnumerable<string> folders,
            string documentName,
            SourceCodeKind sourceCodeKind,
            SourceText initialText = null,
            string filePath = null,
            bool isAdditionalDocument = false)
        {
            var folder = project.FindOrCreateFolder(folders);

            string folderPath = null;
            if (filePath == null && !folder.TryGetFullPath(out folderPath))
            {
                // TODO(cyrusn): Throw an appropriate exception here.
                throw new Exception(ServicesVSResources.Could_not_find_location_of_folder_on_disk);
            }

            return AddDocumentToProjectItems(folder.ProjectItems, documentId, folderPath, documentName, sourceCodeKind, initialText, filePath, isAdditionalDocument);
        }

        private ProjectItem AddDocumentToProjectItems(
            ProjectItems projectItems,
            DocumentId documentId,
            string folderPath,
            string documentName,
            SourceCodeKind sourceCodeKind,
            SourceText initialText,
            string filePath,
            bool isAdditionalDocument)
        {
            if (filePath == null)
            {
                var baseName = Path.GetFileNameWithoutExtension(documentName);
                var extension = isAdditionalDocument ? Path.GetExtension(documentName) : GetPreferredExtension(documentId, sourceCodeKind);
                var uniqueName = projectItems.GetUniqueName(baseName, extension);
                filePath = Path.Combine(folderPath, uniqueName);
            }

            if (initialText != null)
            {
                using (var writer = new StreamWriter(filePath, append: false, encoding: initialText.Encoding ?? Encoding.UTF8))
                {
                    initialText.Write(writer);
                }
            }

            // TODO: restore document ID hinting -- we previously ensured that the AddFromFile will introduce the document ID being used here.
            // (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/677956)
            return projectItems.AddFromFile(filePath);
        }

        private void RemoveDocumentCore(
            DocumentId documentId, bool isAdditionalDocument)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            var document = this.CurrentSolution.GetDocument(documentId);
            var hierarchy = this.GetHierarchy(documentId.ProjectId);
            if (document != null)
            {
                var text = document.GetTextSynchronously(CancellationToken.None);
                var project = hierarchy as IVsProject3;

                var itemId = hierarchy.TryGetItemId(document.FilePath);
                if (itemId == (uint)VSConstants.VSITEMID.Nil)
                {
                    // it is no longer part of the solution
                    return;
                }

                project.RemoveItem(0, itemId, out var result);

                var undoManager = TryGetUndoManager();
                var docInfo = CreateDocumentInfoWithoutText(document);

                if (isAdditionalDocument)
                {
                    undoManager?.Add(new AddAdditionalDocumentUndoUnit(this, docInfo, text));
                }
                else
                {
                    undoManager?.Add(new AddDocumentUndoUnit(this, docInfo, text));
                }
            }
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
        {
            RemoveDocumentCore(documentId, isAdditionalDocument: false);
        }

        protected override void ApplyAdditionalDocumentRemoved(DocumentId documentId)
        {
            RemoveDocumentCore(documentId, isAdditionalDocument: true);
        }

        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            OpenDocumentCore(documentId, activate);
        }

        public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        {
            OpenDocumentCore(documentId, activate);
        }

        public override void CloseDocument(DocumentId documentId)
        {
            CloseDocumentCore(documentId);
        }

        public override void CloseAdditionalDocument(DocumentId documentId)
        {
            CloseDocumentCore(documentId);
        }

        public void OpenDocumentCore(DocumentId documentId, bool activate = true)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (!ThreadHelper.JoinableTaskContext.IsOnMainThread)
            {
                throw new InvalidOperationException(ServicesVSResources.This_workspace_only_supports_opening_documents_on_the_UI_thread);
            }

            var document = this.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                if (TryGetFrame(document, out var frame))
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
        }

        private bool TryGetFrame(CodeAnalysis.Document document, out IVsWindowFrame frame)
        {
            frame = null;

            var hierarchy = GetHierarchy(document.Project.Id);
            var itemId = hierarchy?.TryGetItemId(document.FilePath) ?? (uint)VSConstants.VSITEMID.Nil;
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // If the ItemId is Nil, then IVsProject would not be able to open the
                // document using its ItemId. Thus, we must use OpenDocumentViaProject, which only
                // depends on the file path.

                var openDocumentService = ServiceProvider.GlobalProvider.GetService<IVsUIShellOpenDocument, SVsUIShellOpenDocument>();
                return ErrorHandler.Succeeded(openDocumentService.OpenDocumentViaProject(
                    document.FilePath,
                    VSConstants.LOGVIEWID.TextView_guid,
                    out var oleServiceProvider,
                    out var uiHierarchy,
                    out var itemid,
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

                var vsProject = hierarchy as IVsProject;
                return vsProject != null &&
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
                var filePath = this.CurrentSolution.GetDocument(documentId).FilePath;
                if (filePath != null)
                {
                    var openDocumentService = ServiceProvider.GlobalProvider.GetService<IVsUIShellOpenDocument, SVsUIShellOpenDocument>();
                    if (ErrorHandler.Succeeded(openDocumentService.IsDocumentOpen(null, 0, filePath, Guid.Empty, 0, out var uiHierarchy, null, out var frame, out var isOpen)))
                    {
                        // TODO: do we need save argument for CloseDocument?
                        frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                    }
                }
            }
        }

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText newText)
        {
            ApplyTextDocumentChange(documentId, newText);
        }

        protected override void ApplyAdditionalDocumentTextChanged(DocumentId documentId, SourceText newText)
        {
            ApplyTextDocumentChange(documentId, newText);
        }

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
                    var textBuffer = this.CurrentSolution.GetDocument(documentId).GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None).Container.TryGetTextBuffer();

                    if (textBuffer != null)
                    {
                        TextEditApplication.UpdateText(newText, textBuffer, EditOptions.DefaultMinimalChange);
                        return;
                    }
                }

                // The document wasn't open in a normal way, so invisible editor time
                using (var invisibleEditor = OpenInvisibleEditor(documentId))
                {
                    TextEditApplication.UpdateText(newText, invisibleEditor.TextBuffer, EditOptions.None);
                }
            }
        }

        private string GetPreferredExtension(DocumentId documentId, SourceCodeKind sourceCodeKind)
        {
            // No extension was provided.  Pick a good one based on the type of host project.
            switch (CurrentSolution.GetProject(documentId.ProjectId).Language)
            {
                case LanguageNames.CSharp:
                    // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                    //return sourceCodeKind == SourceCodeKind.Regular ? ".cs" : ".csx";
                    return ".cs";
                case LanguageNames.VisualBasic:
                    // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                    //return sourceCodeKind == SourceCodeKind.Regular ? ".vb" : ".vbx";
                    return ".vb";
                default:
                    throw new InvalidOperationException();
            }
        }

        public override IVsHierarchy GetHierarchy(ProjectId projectId)
        {
            // This doesn't take a lock since _projectToHierarchyMap is immutable
            return _projectToHierarchyMap.GetValueOrDefault(projectId, defaultValue: null);
        }

        internal override Guid GetProjectGuid(ProjectId projectId)
        {
            // This doesn't take a lock since _projectToGuidMap is immutable
            return _projectToGuidMap.GetValueOrDefault(projectId, defaultValue: Guid.Empty);
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

                var filePath = CurrentSolution.GetDocument(documentId)?.FilePath;
                if (filePath == null)
                {
                    return;
                }

                var itemId = hierarchy.TryGetItemId(filePath);
                if (itemId == VSConstants.VSITEMID_NIL)
                {
                    return;
                }

                // Is this owned by a shared project? If so, go recursively. We can put this in a loop because in the case of mixed
                // scenarios where you have shared assets projects and multitargeting projects, this same code works in both cases.
                // Some shared hierarchies, when queried about items also give themselves back, so we'll only loop if we're actually
                // going somewhere else.
                while (SharedProjectUtilities.TryGetItemInSharedAssetsProject(hierarchy, itemId, out IVsHierarchy sharedHierarchy, out uint sharedItemId) &&
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

                // Update the ownership of the file in the Running Document Table
                var project = (IVsProject3)hierarchy;
                project.TransferItem(filePath, filePath, punkWindowFrame: null);
            }
        }

        internal bool TryGetHierarchy(ProjectId projectId, out IVsHierarchy hierarchy)
        {
            hierarchy = this.GetHierarchy(projectId);
            return hierarchy != null;
        }

        internal void StartSolutionCrawler()
        {
            if (_registrationService == null)
            {
                lock (this)
                {
                    if (_registrationService == null)
                    {
                        _registrationService = this.Services.GetService<ISolutionCrawlerRegistrationService>();
                        _registrationService.Register(this);
                    }
                }
            }
        }

        internal void StopSolutionCrawler()
        {
            if (_registrationService != null)
            {
                lock (this)
                {
                    if (_registrationService != null)
                    {
                        _registrationService.Unregister(this, blockingShutdown: true);
                        _registrationService = null;
                    }
                }
            }
        }

        protected override void Dispose(bool finalize)
        {
            if (!finalize)
            {
                _textBufferFactoryService.TextBufferCreated -= AddTextBufferCloneServiceToBuffer;
                _projectionBufferFactoryService.ProjectionBufferCreated -= AddTextBufferCloneServiceToBuffer;
            }

            // workspace is going away. unregister this workspace from work coordinator
            StopSolutionCrawler();

            // We should consider calling this here. It is commented out because Solution event tracking was
            // moved from VisualStudioProjectTracker, which is never Dispose()'d.  Rather than risk the
            // UnadviseSolutionEvents causing another issue (calling into dead COM objects, etc), we'll just
            // continue to skip it for now.
            // UnadviseSolutionEvents();

            base.Dispose(finalize);
        }

        public void EnsureEditableDocuments(IEnumerable<DocumentId> documents)
        {
            var queryEdit = (IVsQueryEditQuerySave2)ServiceProvider.GlobalProvider.GetService(typeof(SVsQueryEditQuerySave));

            // make sure given document id actually exist in current solution and the file is marked as supporting modifications
            // and actually has non null file path
            var fileNames = documents.Select(GetFilePath).ToArray();

            // TODO: meditate about the flags we can pass to this and decide what is most appropriate for Roslyn
            int result = queryEdit.QueryEditFiles(
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
        {
            this.EnsureEditableDocuments((IEnumerable<DocumentId>)documents);
        }

        internal override bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!TryGetHierarchy(referencingProject, out var referencingHierarchy) ||
                !TryGetHierarchy(referencedProject, out var referencedHierarchy))
            {
                // Couldn't even get a hierarchy for this project. So we have to assume
                // that adding a reference is disallowed.
                return false;
            }

            // First we have to see if either project disallows the reference being added.
            const int ContextFlags = (int)__VSQUERYFLAVORREFERENCESCONTEXT.VSQUERYFLAVORREFERENCESCONTEXT_RefreshReference;

            uint canAddProjectReference = (uint)__VSREFERENCEQUERYRESULT.REFERENCE_UNKNOWN;
            uint canBeReferenced = (uint)__VSREFERENCEQUERYRESULT.REFERENCE_UNKNOWN;

            if (referencingHierarchy is IVsProjectFlavorReferences3 referencingProjectFlavor3)
            {
                if (ErrorHandler.Failed(referencingProjectFlavor3.QueryAddProjectReferenceEx(referencedHierarchy, ContextFlags, out canAddProjectReference, out var unused)))
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
                if (ErrorHandler.Failed(referencedProjectFlavor3.QueryCanBeReferencedEx(referencingHierarchy, ContextFlags, out canBeReferenced, out var unused)))
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

        private readonly object _gate = new object();

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
        /// Applies a change to the workspace that can do any number of project changes.
        /// </summary>
        /// <remarks>This is needed to synchronize with <see cref="ApplyChangeToWorkspace(Action{Workspace})" /> to avoid any races. This
        /// method could be moved down to the core Workspace layer and then could use the synchronization lock there.</remarks>
        /// <param name="projectId">The <see cref="ProjectId" /> to change.</param>
        /// <param name="mutation">A function that, given the old <see cref="CodeAnalysis.Project"/> will produce a new one.</param>
        public void ApplyBatchChangeToProject(ProjectId projectId, Func<CodeAnalysis.Solution, CodeAnalysis.Solution> mutation)
        {
            lock (_gate)
            {
                var oldSolution = this.CurrentSolution;
                var newSolution = mutation(oldSolution);

                if (oldSolution == newSolution)
                {
                    return;
                }

                SetCurrentSolution(newSolution);
                RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, oldSolution, newSolution, projectId);
            }
        }

        private Dictionary<ProjectId, ProjectReferenceInformation> _projectReferenceInfoMap = new Dictionary<ProjectId, ProjectReferenceInformation>();

        private ProjectReferenceInformation GetReferenceInfo_NoLock(ProjectId projectId)
        {
            return _projectReferenceInfoMap.GetOrAdd(projectId, _ => new ProjectReferenceInformation());
        }

        protected internal override void OnProjectRemoved(ProjectId projectId)
        {
            lock (_gate)
            {
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

                _projectToGuidMap = _projectToGuidMap.Remove(projectId);
                _projectToHierarchyMap = _projectToHierarchyMap.Remove(projectId);

                foreach (var (projectName, projects) in _projectSystemNameToProjectsMap)
                {
                    if (projects.RemoveAll(p => p.Id == projectId) > 0)
                    {
                        if (projects.Count == 0)
                        {
                            _projectSystemNameToProjectsMap.Remove(projectName);
                        }

                        return;
                    }
                }

                base.OnProjectRemoved(projectId);
            }
        }

        private class ProjectReferenceInformation
        {
            public List<string> OutputPaths = new List<string>();
            public List<(string path, ProjectReference projectReference)> ConvertedProjectReferences = new List<(string path, ProjectReference)>();
        }

        /// <summary>
        /// A multimap from an output path to the project outputting to it. Ideally, this shouldn't ever
        /// actually be a true multimap, since we shouldn't have two projects outputting to the same path, but
        /// any bug by a project adding the wrong output path means we could end up with some duplication.
        /// In that case, we'll temporarily have two until (hopefully) somebody removes it.
        /// </summary>
        private readonly Dictionary<string, List<ProjectId>> _projectsByOutputPath = new Dictionary<string, List<ProjectId>>(StringComparer.OrdinalIgnoreCase);

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
                    ConvertProjectReferencesToMetadataReferences_NoLock(projectId, outputPath);
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
            var modifiedSolution = this.CurrentSolution;
            var projectIdsChanged = new HashSet<ProjectId>();

            foreach (var projectIdToRetarget in this.CurrentSolution.ProjectIds)
            {
                if (CanConvertMetadataReferenceToProjectReference(projectIdToRetarget, referencedProjectId: projectId))
                {
                    // PERF: call GetProjectState instead of GetProject, otherwise creating a new project might force all
                    // Project instances to get created.
                    foreach (PortableExecutableReference reference in modifiedSolution.GetProjectState(projectIdToRetarget).MetadataReferences)
                    {
                        if (string.Equals(reference.FilePath, outputPath, StringComparison.OrdinalIgnoreCase))
                        {
                            var projectReference = new ProjectReference(projectId, reference.Properties.Aliases, reference.Properties.EmbedInteropTypes);
                            modifiedSolution = modifiedSolution.RemoveMetadataReference(projectIdToRetarget, reference)
                                                               .AddProjectReference(projectIdToRetarget, projectReference);

                            projectIdsChanged.Add(projectIdToRetarget);

                            GetReferenceInfo_NoLock(projectIdToRetarget).ConvertedProjectReferences.Add(
                                (reference.FilePath, projectReference));

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
        private bool CanConvertMetadataReferenceToProjectReference(ProjectId projectIdWithMetadataReference, ProjectId referencedProjectId)
        {
            // PERF: call GetProjectState instead of GetProject, otherwise creating a new project might force all
            // Project instances to get created.
            var projectWithMetadataReference = CurrentSolution.GetProjectState(projectIdWithMetadataReference);
            var referencedProject = CurrentSolution.GetProjectState(referencedProjectId);

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
                    // We're referencing something that we can't create a compilation from something that can, so keep the metadtata reference
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Finds all projects that had a project reference to <paramref name="projectId"/> and convert it back to a metadata reference.
        /// </summary>
        /// <param name="projectId">The <see cref="ProjectId"/> of the project being referenced.</param>
        /// <param name="outputPath">The output path of the given project to remove the link to.</param>
        private void ConvertProjectReferencesToMetadataReferences_NoLock(ProjectId projectId, string outputPath)
        {
            var modifiedSolution = this.CurrentSolution;
            var projectIdsChanged = new HashSet<ProjectId>();

            foreach (var projectIdToRetarget in this.CurrentSolution.ProjectIds)
            {
                var referenceInfo = GetReferenceInfo_NoLock(projectIdToRetarget);

                foreach (var convertedReference in referenceInfo.ConvertedProjectReferences.ToList())
                {
                    if (string.Equals(convertedReference.path, outputPath, StringComparison.OrdinalIgnoreCase) &&
                        convertedReference.projectReference.ProjectId == projectId)
                    {
                        var metadataReference =
                            CreatePortableExecutableReference(
                                convertedReference.path,
                                new MetadataReferenceProperties(
                                    aliases: convertedReference.projectReference.Aliases,
                                    embedInteropTypes: convertedReference.projectReference.EmbedInteropTypes));

                        modifiedSolution = modifiedSolution.RemoveProjectReference(projectIdToRetarget, convertedReference.projectReference)
                                                           .AddMetadataReference(projectIdToRetarget, metadataReference);

                        projectIdsChanged.Add(projectIdToRetarget);

                        referenceInfo.ConvertedProjectReferences.Remove(convertedReference);

                        // We have converted one, but you could have more than one reference with different aliases
                        // that we need to convert, so we'll keep going
                    }
                }
            }

            SetSolutionAndRaiseWorkspaceChanged_NoLock(modifiedSolution, projectIdsChanged);
        }

        public ProjectReference TryCreateConvertedProjectReference(ProjectId referencingProject, string path, MetadataReferenceProperties properties)
        {
            lock (_gate)
            {
                if (_projectsByOutputPath.TryGetValue(path, out var ids) && ids.Distinct().Count() == 1)
                {
                    var projectIdToReference = ids.First();

                    if (CanConvertMetadataReferenceToProjectReference(referencingProject, projectIdToReference))
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
        }

        public ProjectReference TryRemoveConvertedProjectReference(ProjectId referencingProject, string path, MetadataReferenceProperties properties)
        {
            lock (_gate)
            {
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
            }

            return null;
        }

        private void SetSolutionAndRaiseWorkspaceChanged_NoLock(CodeAnalysis.Solution modifiedSolution, ICollection<ProjectId> projectIdsChanged)
        {
            if (projectIdsChanged.Count > 0)
            {
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

                if (_projectsByOutputPath.TryGetValue(outputPath, out var remainingProjectsForOutputPath))
                {
                    if (remainingProjectsForOutputPath.Distinct().Count() == 1)
                    {
                        // We had more than one project outputting to the same path. Now we're back down to one
                        // so we can reference that one again
                        ConvertMetadataReferencesToProjectReferences_NoLock(_projectsByOutputPath[outputPath].Single(), outputPath);
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
}
