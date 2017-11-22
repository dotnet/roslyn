// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
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
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using Roslyn.VisualStudio.ProjectSystem;
using VSLangProj;
using VSLangProj140;
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

        // document worker coordinator
        private ISolutionCrawlerRegistrationService _registrationService;

        /// <summary>
        /// A <see cref="ForegroundThreadAffinitizedObject"/> to make assertions that stuff is on the right thread.
        /// This is Lazy because it might be created on a background thread when nothing is initialized yet.
        /// </summary>
        private readonly Lazy<ForegroundThreadAffinitizedObject> _foregroundObject
            = new Lazy<ForegroundThreadAffinitizedObject>(() => new ForegroundThreadAffinitizedObject());

        /// <summary>
        /// The <see cref="DeferredInitializationState"/> that consists of the <see cref="VisualStudioProjectTracker" />
        /// and other UI-initialized types. It will be created as long as a single project has been created.
        /// </summary>
        internal DeferredInitializationState DeferredState { get; private set; }

        public VisualStudioWorkspaceImpl(ExportProvider exportProvider)
            : base(
                MefV1HostServices.Create(exportProvider),
                backgroundWork: WorkspaceBackgroundWork.ParseAndCompile)
        {
            PrimaryWorkspace.Register(this);
        }

        /// <summary>
        /// Ensures the workspace is fully hooked up to the host by subscribing to all sorts of VS
        /// UI thread affinitized events.
        /// </summary>
        internal VisualStudioProjectTracker GetProjectTrackerAndInitializeIfNecessary(IServiceProvider serviceProvider)
        {
            if (DeferredState == null)
            {
                _foregroundObject.Value.AssertIsForeground();
                DeferredState = new DeferredInitializationState(this, serviceProvider);
            }

            return DeferredState.ProjectTracker;
        }

        /// <summary>
        /// A compatibility shim to ensure that F# and TypeScript continue to work after the deferred work goes in. This will be
        /// removed once they move to calling <see cref="GetProjectTrackerAndInitializeIfNecessary"/>.
        /// </summary>
        internal VisualStudioProjectTracker ProjectTracker
        {
            get
            {
                return GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider);
            }
        }

        internal void ClearReferenceCache()
        {
            DeferredState?.ProjectTracker.MetadataReferenceProvider.ClearCache();
        }

        internal IVisualStudioHostDocument GetHostDocument(DocumentId documentId)
        {
            var project = GetHostProject(documentId.ProjectId);
            if (project != null)
            {
                return project.GetDocumentOrAdditionalDocument(documentId);
            }

            return null;
        }

        internal AbstractProject GetHostProject(ProjectId projectId)
        {
            return DeferredState?.ProjectTracker.GetProject(projectId);
        }

        private bool TryGetHostProject(ProjectId projectId, out AbstractProject project)
        {
            project = GetHostProject(projectId);
            return project != null;
        }

        internal override bool TryApplyChanges(
            Microsoft.CodeAnalysis.Solution newSolution,
            IProgressTracker progressTracker)
        {
            if (_foregroundObject.IsValueCreated && !_foregroundObject.Value.IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.VisualStudioWorkspace_TryApplyChanges_cannot_be_called_from_a_background_thread);
            }

            var projectChanges = newSolution.GetChanges(this.CurrentSolution).GetProjectChanges().ToList();
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
                    projectsToLoad.Add(GetHostProject(pc.ProjectId).Guid);
                }
            }

            if (projectsToLoad.Any())
            {
                var vsSolution4 = (IVsSolution4)DeferredState.ServiceProvider.GetService(typeof(SVsSolution));
                vsSolution4.EnsureProjectsAreLoaded(
                    (uint)projectsToLoad.Count,
                    projectsToLoad.ToArray(),
                    (uint)__VSBSLFLAGS.VSBSLFLAGS_None);
            }

            // first make sure we can edit the document we will be updating (check them out from source control, etc)
            var changedDocs = projectChanges.SelectMany(pd => pd.GetChangedDocuments()).ToList();
            if (changedDocs.Count > 0)
            {
                this.EnsureEditableDocuments(changedDocs);
            }

            return base.TryApplyChanges(newSolution, progressTracker);
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
            _foregroundObject.Value.AssertIsForeground();

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

        private bool TryGetProjectData(ProjectId projectId, out AbstractProject hostProject, out IVsHierarchy hierarchy, out EnvDTE.Project project)
        {
            hierarchy = null;
            project = null;

            return this.TryGetHostProject(projectId, out hostProject)
                && this.TryGetHierarchy(projectId, out hierarchy)
                && hierarchy.TryGetProject(out project);
        }

        internal void GetProjectData(ProjectId projectId, out AbstractProject hostProject, out IVsHierarchy hierarchy, out EnvDTE.Project project)
        {
            if (!TryGetProjectData(projectId, out hostProject, out hierarchy, out project))
            {
                throw new ArgumentException(string.Format(ServicesVSResources.Could_not_find_project_0, projectId));
            }
        }

        internal EnvDTE.Project TryGetDTEProject(ProjectId projectId)
        {
            return TryGetProjectData(projectId, out var hostProject, out var hierarchy, out var project) ? project : null;
        }

        internal bool TryAddReferenceToProject(ProjectId projectId, string assemblyName)
        {
            EnvDTE.Project project;
            try
            {
                GetProjectData(projectId, out var hostProject, out var hierarchy, out project);
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

            GetProjectData(projectId, out var hostProject, out var hierarchy, out var project);
            foreach (string configurationName in (object[])project.ConfigurationManager.ConfigurationRowNames)
            {
                switch (hostProject.Language)
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

            GetProjectData(projectId, out var hostProject, out var hierarchy, out var project);

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

            GetProjectData(projectId, out var hostProject, out var hierarchy, out var project);

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

            GetProjectData(projectId, out var hostProject, out var hierarchy, out var project);

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

            GetProjectData(projectId, out var hostProject, out var hierarchy, out var project);

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

            GetProjectData(projectId, out var hostProject, out var hierarchy, out var project);
            GetProjectData(projectReference.ProjectId, out var refHostProject, out var refHierarchy, out var refProject);

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
                var documentId = documentTrackingService.GetActiveDocument() ?? documentTrackingService.GetVisibleDocuments().FirstOrDefault();
                if (documentId != null)
                {
                    var composition = (IComponentModel)this.DeferredState.ServiceProvider.GetService(typeof(SComponentModel));
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

            GetProjectData(projectId, out var hostProject, out var hierarchy, out var project);
            GetProjectData(projectReference.ProjectId, out var refHostProject, out var refHierarchy, out var refProject);

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
            GetProjectData(info.Id.ProjectId, out var hostProject, out var hierarchy, out var project);

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
                AddDocumentToFolder(hostProject, project, info.Id, SpecializedCollections.SingletonEnumerable(AppCodeFolderName), info.Name, info.SourceCodeKind, initialText, isAdditionalDocument: isAdditionalDocument);
            }
            else if (folders.Any())
            {
                AddDocumentToFolder(hostProject, project, info.Id, folders, info.Name, info.SourceCodeKind, initialText, isAdditionalDocument: isAdditionalDocument);
            }
            else
            {
                AddDocumentToProject(hostProject, project, info.Id, info.Name, info.SourceCodeKind, initialText, isAdditionalDocument: isAdditionalDocument);
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
            AbstractProject hostProject,
            EnvDTE.Project project,
            DocumentId documentId,
            string documentName,
            SourceCodeKind sourceCodeKind,
            SourceText initialText = null,
            string filePath = null,
            bool isAdditionalDocument = false)
        {
            if (!project.TryGetFullPath(out var folderPath))
            {
                // TODO(cyrusn): Throw an appropriate exception here.
                throw new Exception(ServicesVSResources.Could_not_find_location_of_folder_on_disk);
            }

            return AddDocumentToProjectItems(hostProject, project.ProjectItems, documentId, folderPath, documentName, sourceCodeKind, initialText, filePath, isAdditionalDocument);
        }

        private ProjectItem AddDocumentToFolder(
            AbstractProject hostProject,
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
            if (!folder.TryGetFullPath(out var folderPath))
            {
                // TODO(cyrusn): Throw an appropriate exception here.
                throw new Exception(ServicesVSResources.Could_not_find_location_of_folder_on_disk);
            }

            return AddDocumentToProjectItems(hostProject, folder.ProjectItems, documentId, folderPath, documentName, sourceCodeKind, initialText, filePath, isAdditionalDocument);
        }

        private ProjectItem AddDocumentToProjectItems(
            AbstractProject hostProject,
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
                var extension = isAdditionalDocument ? Path.GetExtension(documentName) : GetPreferredExtension(hostProject, sourceCodeKind);
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

            using (var documentIdHint = DeferredState.ProjectTracker.DocumentProvider.ProvideDocumentIdHint(filePath, documentId))
            {
                return projectItems.AddFromFile(filePath);
            }
        }

        protected void RemoveDocumentCore(
            DocumentId documentId, bool isAdditionalDocument)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            var hostDocument = this.GetHostDocument(documentId);
            if (hostDocument != null)
            {
                var document = this.CurrentSolution.GetDocument(documentId);
                var text = this.GetTextForced(document);

                var project = hostDocument.Project.Hierarchy as IVsProject3;

                var itemId = hostDocument.GetItemId();
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

            if (!_foregroundObject.Value.IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.This_workspace_only_supports_opening_documents_on_the_UI_thread);
            }

            var document = this.GetHostDocument(documentId);
            if (document != null && document.Project != null)
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

        private bool TryGetFrame(IVisualStudioHostDocument document, out IVsWindowFrame frame)
        {
            frame = null;

            var itemId = document.GetItemId();
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // If the ItemId is Nil, then IVsProject would not be able to open the 
                // document using its ItemId. Thus, we must use OpenDocumentViaProject, which only 
                // depends on the file path.

                return ErrorHandler.Succeeded(DeferredState.ShellOpenDocumentService.OpenDocumentViaProject(
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

                var vsProject = document.Project.Hierarchy as IVsProject;
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
                var document = this.GetHostDocument(documentId);
                if (document != null)
                {
                    if (ErrorHandler.Succeeded(DeferredState.ShellOpenDocumentService.IsDocumentOpen(null, 0, document.FilePath, Guid.Empty, 0, out var uiHierarchy, null, out var frame, out var isOpen)))
                    {
                        // TODO: do we need save argument for CloseDocument?
                        frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                    }
                }
            }
        }

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText newText)
        {
            EnsureEditableDocuments(documentId);
            var hostDocument = GetHostDocument(documentId);
            hostDocument.UpdateText(newText);
        }

        protected override void ApplyAdditionalDocumentTextChanged(DocumentId documentId, SourceText newText)
        {
            EnsureEditableDocuments(documentId);
            var hostDocument = GetHostDocument(documentId);
            hostDocument.UpdateText(newText);
        }

        private static string GetPreferredExtension(AbstractProject hostProject, SourceCodeKind sourceCodeKind)
        {
            // No extension was provided.  Pick a good one based on the type of host project.
            switch (hostProject.Language)
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
            var project = this.GetHostProject(projectId);

            if (project == null)
            {
                return null;
            }

            return project.Hierarchy;
        }

        internal override void SetDocumentContext(DocumentId documentId)
        {
            var hostDocument = GetHostDocument(documentId);
            if (hostDocument == null)
            {
                // the document or project is not being tracked
                return;
            }

            var itemId = hostDocument.GetItemId();
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // the document has been removed from the solution
                return;
            }

            var hierarchy = hostDocument.Project.Hierarchy;
            var sharedHierarchy = LinkedFileUtilities.GetSharedHierarchyForItem(hierarchy, itemId);
            if (sharedHierarchy != null)
            {
                if (sharedHierarchy.SetProperty(
                        (uint)VSConstants.VSITEMID.Root,
                        (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext,
                        DeferredState.ProjectTracker.GetProject(documentId.ProjectId).ProjectSystemName) == VSConstants.S_OK)
                {
                    // The ASP.NET 5 intellisense project is now updated.
                    return;
                }
                else
                {
                    // Universal Project shared files
                    //     Change the SharedItemContextHierarchy of the project's parent hierarchy, then
                    //     hierarchy events will trigger the workspace to update.
                    var hr = sharedHierarchy.SetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy, hierarchy);
                }
            }
            else
            {
                // Regular linked files
                //     Transfer the item (open buffer) to the new hierarchy, and then hierarchy events 
                //     will trigger the workspace to update.
                var vsproj = hierarchy as IVsProject3;
                var hr = vsproj.TransferItem(hostDocument.FilePath, hostDocument.FilePath, punkWindowFrame: null);
            }
        }

        internal void UpdateDocumentContextIfContainsDocument(IVsHierarchy sharedHierarchy, DocumentId documentId)
        {
            // TODO: This is a very roundabout way to update the context

            // The sharedHierarchy passed in has a new context, but we don't know what it is.
            // The documentId passed in is associated with this sharedHierarchy, and this method
            // will be called once for each such documentId. During this process, one of these
            // documentIds will actually belong to the new SharedItemContextHierarchy. Once we
            // find that one, we can map back to the open buffer and set its active context to
            // the appropriate project.

            // Note that if there is a single head project and it's in the process of being unloaded
            // there might not be a host project.
            var hostProject = LinkedFileUtilities.GetContextHostProject(sharedHierarchy, DeferredState.ProjectTracker);
            if (hostProject?.Hierarchy == sharedHierarchy)
            {
                return;
            }

            if (hostProject.Id != documentId.ProjectId)
            {
                // While this documentId is associated with one of the head projects for this
                // sharedHierarchy, it is not associated with the new context hierarchy. Another
                // documentId will be passed to this method and update the context.
                return;
            }

            // This documentId belongs to the new SharedItemContextHierarchy. Update the associated
            // buffer.
            OnDocumentContextUpdated(documentId);
        }

        /// <summary>
        /// Finds the <see cref="DocumentId"/> related to the given <see cref="DocumentId"/> that
        /// is in the current context. For regular files (non-shared and non-linked) and closed
        /// linked files, this is always the provided <see cref="DocumentId"/>. For open linked
        /// files and open shared files, the active context is already tracked by the
        /// <see cref="Workspace"/> and can be looked up directly. For closed shared files, the
        /// document in the shared project's <see cref="__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy"/> 
        /// is preferred.
        /// </summary>
        internal override DocumentId GetDocumentIdInCurrentContext(DocumentId documentId)
        {
            // If the document is open, then the Workspace knows the current context for both 
            // linked and shared files
            if (IsDocumentOpen(documentId))
            {
                return base.GetDocumentIdInCurrentContext(documentId);
            }

            var hostDocument = GetHostDocument(documentId);
            if (hostDocument == null)
            {
                // This can happen if the document was temporary and has since been closed/deleted.
                return base.GetDocumentIdInCurrentContext(documentId);
            }

            var itemId = hostDocument.GetItemId();
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // An itemid is required to determine whether the file belongs to a Shared Project
                return base.GetDocumentIdInCurrentContext(documentId);
            }

            // If this is a regular document or a closed linked (non-shared) document, then use the
            // default logic for determining current context.
            var sharedHierarchy = LinkedFileUtilities.GetSharedHierarchyForItem(hostDocument.Project.Hierarchy, itemId);
            if (sharedHierarchy == null)
            {
                return base.GetDocumentIdInCurrentContext(documentId);
            }

            // This is a closed shared document, so we must determine the correct context.
            var hostProject = LinkedFileUtilities.GetContextHostProject(sharedHierarchy, DeferredState.ProjectTracker);
            var matchingProject = CurrentSolution.GetProject(hostProject.Id);
            if (matchingProject == null || hostProject.Hierarchy == sharedHierarchy)
            {
                return base.GetDocumentIdInCurrentContext(documentId);
            }

            if (matchingProject.ContainsDocument(documentId))
            {
                // The provided documentId is in the current context project
                return documentId;
            }

            // The current context document is from another project.
            var linkedDocumentIds = CurrentSolution.GetDocument(documentId).GetLinkedDocumentIds();
            var matchingDocumentId = linkedDocumentIds.FirstOrDefault(id => id.ProjectId == matchingProject.Id);
            return matchingDocumentId ?? base.GetDocumentIdInCurrentContext(documentId);
        }

        internal bool TryGetHierarchy(ProjectId projectId, out IVsHierarchy hierarchy)
        {
            hierarchy = this.GetHierarchy(projectId);
            return hierarchy != null;
        }

        public override string GetFilePath(DocumentId documentId)
        {
            var document = this.GetHostDocument(documentId);

            if (document == null)
            {
                return null;
            }
            else
            {
                return document.FilePath;
            }
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
            var queryEdit = (IVsQueryEditQuerySave2)DeferredState.ServiceProvider.GetService(typeof(SVsQueryEditQuerySave));

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

        internal void OnDocumentTextUpdatedOnDisk(DocumentId documentId)
        {
            var vsDoc = this.GetHostDocument(documentId);
            this.OnDocumentTextLoaderChanged(documentId, vsDoc.Loader);
        }

        internal void OnAdditionalDocumentTextUpdatedOnDisk(DocumentId documentId)
        {
            var vsDoc = this.GetHostDocument(documentId);
            this.OnAdditionalDocumentTextLoaderChanged(documentId, vsDoc.Loader);
        }

        internal override bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject)
        {
            _foregroundObject.Value.AssertIsForeground();
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
            var referenceManager = (IVsReferenceManager)DeferredState.ServiceProvider.GetService(typeof(SVsReferenceManager));
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
        /// A trivial implementation of <see cref="IVisualStudioWorkspaceHost" /> that just
        /// forwards the calls down to the underlying Workspace.
        /// </summary>
        protected sealed class VisualStudioWorkspaceHost : IVisualStudioWorkspaceHost, IVisualStudioWorkspaceHost2, IVisualStudioWorkingFolder
        {
            private readonly VisualStudioWorkspaceImpl _workspace;

            private readonly Dictionary<DocumentId, uint> _documentIdToHierarchyEventsCookieMap = new Dictionary<DocumentId, uint>();

            public VisualStudioWorkspaceHost(VisualStudioWorkspaceImpl workspace)
            {
                _workspace = workspace;
            }

            void IVisualStudioWorkspaceHost.OnOptionsChanged(ProjectId projectId, CompilationOptions compilationOptions, ParseOptions parseOptions)
            {
                _workspace.OnCompilationOptionsChanged(projectId, compilationOptions);
                _workspace.OnParseOptionsChanged(projectId, parseOptions);
            }

            void IVisualStudioWorkspaceHost.OnDocumentAdded(DocumentInfo documentInfo)
            {
                _workspace.OnDocumentAdded(documentInfo);
            }

            void IVisualStudioWorkspaceHost.OnDocumentClosed(DocumentId documentId, ITextBuffer textBuffer, TextLoader loader, bool updateActiveContext)
            {
                // TODO: Move this out to DocumentProvider. As is, this depends on being able to 
                // access the host document which will already be deleted in some cases, causing 
                // a crash. Until this is fixed, we will leak a HierarchyEventsSink every time a
                // Mercury shared document is closed.
                // UnsubscribeFromSharedHierarchyEvents(documentId);
                using (_workspace.Services.GetService<IGlobalOperationNotificationService>().Start("Document Closed"))
                {
                    _workspace.OnDocumentClosed(documentId, loader, updateActiveContext);
                }
            }

            void IVisualStudioWorkspaceHost.OnDocumentOpened(DocumentId documentId, ITextBuffer textBuffer, bool currentContext)
            {
                SubscribeToSharedHierarchyEvents(documentId);
                _workspace.OnDocumentOpened(documentId, textBuffer.AsTextContainer(), currentContext);
            }

            private void SubscribeToSharedHierarchyEvents(DocumentId documentId)
            {
                // Todo: maybe avoid double alerts.
                var hostDocument = _workspace.GetHostDocument(documentId);
                if (hostDocument == null)
                {
                    return;
                }

                var hierarchy = hostDocument.Project.Hierarchy;

                var itemId = hostDocument.GetItemId();
                if (itemId == (uint)VSConstants.VSITEMID.Nil)
                {
                    // the document has been removed from the solution
                    return;
                }

                var sharedHierarchy = LinkedFileUtilities.GetSharedHierarchyForItem(hierarchy, itemId);
                if (sharedHierarchy != null)
                {
                    var eventSink = new HierarchyEventsSink(_workspace, sharedHierarchy, documentId);
                    var hr = sharedHierarchy.AdviseHierarchyEvents(eventSink, out var cookie);

                    if (hr == VSConstants.S_OK && !_documentIdToHierarchyEventsCookieMap.ContainsKey(documentId))
                    {
                        _documentIdToHierarchyEventsCookieMap.Add(documentId, cookie);
                    }
                }
            }

            private void UnsubscribeFromSharedHierarchyEvents(DocumentId documentId)
            {
                var hostDocument = _workspace.GetHostDocument(documentId);
                var itemId = hostDocument.GetItemId();
                if (itemId == (uint)VSConstants.VSITEMID.Nil)
                {
                    // the document has been removed from the solution
                    return;
                }

                var sharedHierarchy = LinkedFileUtilities.GetSharedHierarchyForItem(hostDocument.Project.Hierarchy, itemId);
                if (sharedHierarchy != null)
                {
                    if (_documentIdToHierarchyEventsCookieMap.TryGetValue(documentId, out var cookie))
                    {
                        var hr = sharedHierarchy.UnadviseHierarchyEvents(cookie);
                        _documentIdToHierarchyEventsCookieMap.Remove(documentId);
                    }
                }
            }

            private void RegisterPrimarySolutionForPersistentStorage(
                SolutionId solutionId)
            {
                var service = _workspace.Services.GetService<IPersistentStorageService>() as AbstractPersistentStorageService;
                if (service == null)
                {
                    return;
                }

                service.RegisterPrimarySolution(solutionId);
            }

            private void UnregisterPrimarySolutionForPersistentStorage(
                SolutionId solutionId, bool synchronousShutdown)
            {
                var service = _workspace.Services.GetService<IPersistentStorageService>() as AbstractPersistentStorageService;
                if (service == null)
                {
                    return;
                }

                service.UnregisterPrimarySolution(solutionId, synchronousShutdown);
            }

            void IVisualStudioWorkspaceHost.OnDocumentRemoved(DocumentId documentId)
            {
                _workspace.OnDocumentRemoved(documentId);
            }

            void IVisualStudioWorkspaceHost.OnMetadataReferenceAdded(ProjectId projectId, PortableExecutableReference metadataReference)
            {
                _workspace.OnMetadataReferenceAdded(projectId, metadataReference);
            }

            void IVisualStudioWorkspaceHost.OnMetadataReferenceRemoved(ProjectId projectId, PortableExecutableReference metadataReference)
            {
                _workspace.OnMetadataReferenceRemoved(projectId, metadataReference);
            }

            void IVisualStudioWorkspaceHost.OnProjectAdded(ProjectInfo projectInfo)
            {
                using (_workspace.Services.GetService<IGlobalOperationNotificationService>()?.Start("Add Project"))
                {
                    _workspace.OnProjectAdded(projectInfo);
                }
            }

            void IVisualStudioWorkspaceHost.OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
            {
                _workspace.OnProjectReferenceAdded(projectId, projectReference);
            }

            void IVisualStudioWorkspaceHost.OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
            {
                _workspace.OnProjectReferenceRemoved(projectId, projectReference);
            }

            void IVisualStudioWorkspaceHost.OnProjectRemoved(ProjectId projectId)
            {
                using (_workspace.Services.GetService<IGlobalOperationNotificationService>()?.Start("Remove Project"))
                {
                    _workspace.OnProjectRemoved(projectId);
                }
            }

            void IVisualStudioWorkspaceHost.OnSolutionAdded(SolutionInfo solutionInfo)
            {
                RegisterPrimarySolutionForPersistentStorage(solutionInfo.Id);

                _workspace.OnSolutionAdded(solutionInfo);
            }

            void IVisualStudioWorkspaceHost.OnSolutionRemoved()
            {
                var solutionId = _workspace.CurrentSolution.Id;

                _workspace.OnSolutionRemoved();
                _workspace.ClearReferenceCache();

                UnregisterPrimarySolutionForPersistentStorage(solutionId, synchronousShutdown: false);
            }

            void IVisualStudioWorkspaceHost.ClearSolution()
            {
                _workspace.ClearSolution();
                _workspace.ClearReferenceCache();
            }

            void IVisualStudioWorkspaceHost.OnDocumentTextUpdatedOnDisk(DocumentId id)
            {
                _workspace.OnDocumentTextUpdatedOnDisk(id);
            }

            void IVisualStudioWorkspaceHost.OnAssemblyNameChanged(ProjectId id, string assemblyName)
            {
                _workspace.OnAssemblyNameChanged(id, assemblyName);
            }

            void IVisualStudioWorkspaceHost.OnOutputFilePathChanged(ProjectId id, string outputFilePath)
            {
                _workspace.OnOutputFilePathChanged(id, outputFilePath);
            }

            void IVisualStudioWorkspaceHost.OnProjectNameChanged(ProjectId projectId, string name, string filePath)
            {
                _workspace.OnProjectNameChanged(projectId, name, filePath);
            }

            void IVisualStudioWorkspaceHost.OnAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
            {
                _workspace.OnAnalyzerReferenceAdded(projectId, analyzerReference);
            }

            void IVisualStudioWorkspaceHost.OnAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
            {
                _workspace.OnAnalyzerReferenceRemoved(projectId, analyzerReference);
            }

            void IVisualStudioWorkspaceHost.OnAdditionalDocumentAdded(DocumentInfo documentInfo)
            {
                _workspace.OnAdditionalDocumentAdded(documentInfo);
            }

            void IVisualStudioWorkspaceHost.OnAdditionalDocumentRemoved(DocumentId documentInfo)
            {
                _workspace.OnAdditionalDocumentRemoved(documentInfo);
            }

            void IVisualStudioWorkspaceHost.OnAdditionalDocumentOpened(DocumentId documentId, ITextBuffer textBuffer, bool isCurrentContext)
            {
                _workspace.OnAdditionalDocumentOpened(documentId, textBuffer.AsTextContainer(), isCurrentContext);
            }

            void IVisualStudioWorkspaceHost.OnAdditionalDocumentClosed(DocumentId documentId, ITextBuffer textBuffer, TextLoader loader)
            {
                _workspace.OnAdditionalDocumentClosed(documentId, loader);
            }

            void IVisualStudioWorkspaceHost.OnAdditionalDocumentTextUpdatedOnDisk(DocumentId id)
            {
                _workspace.OnAdditionalDocumentTextUpdatedOnDisk(id);
            }

            void IVisualStudioWorkspaceHost2.OnHasAllInformation(ProjectId projectId, bool hasAllInformation)
            {
                _workspace.OnHasAllInformationChanged(projectId, hasAllInformation);
            }

            void IVisualStudioWorkingFolder.OnBeforeWorkingFolderChange()
            {
                UnregisterPrimarySolutionForPersistentStorage(_workspace.CurrentSolution.Id, synchronousShutdown: true);
            }

            void IVisualStudioWorkingFolder.OnAfterWorkingFolderChange()
            {
                var solutionId = _workspace.CurrentSolution.Id;

                _workspace.DeferredState.ProjectTracker.UpdateSolutionProperties(solutionId);
                RegisterPrimarySolutionForPersistentStorage(solutionId);
            }
        }
    }
}
