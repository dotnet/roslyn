// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Feedback.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using Roslyn.VisualStudio.ProjectSystem;
using VSLangProj;
using VSLangProj140;
using OLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// The Workspace for running inside Visual Studio.
    /// </summary>
    internal abstract class VisualStudioWorkspaceImpl : VisualStudioWorkspace
    {
        private static readonly IntPtr s_docDataExisting_Unknown = new IntPtr(-1);
        private const string AppCodeFolderName = "App_Code";

        protected readonly IServiceProvider ServiceProvider;
        private readonly IVsUIShellOpenDocument _shellOpenDocument;
        private readonly IVsTextManager _textManager;

        // Not readonly because it needs to be set in the derived class' constructor.
        private VisualStudioProjectTracker _projectTracker;

        // document worker coordinator
        private ISolutionCrawlerRegistrationService _registrationService;

        private readonly ForegroundThreadAffinitizedObject _foregroundObject = new ForegroundThreadAffinitizedObject();

        public VisualStudioWorkspaceImpl(
            SVsServiceProvider serviceProvider,
            WorkspaceBackgroundWork backgroundWork)
            : base(
                CreateHostServices(serviceProvider),
                backgroundWork)
        {
            this.ServiceProvider = serviceProvider;
            _textManager = serviceProvider.GetService(typeof(SVsTextManager)) as IVsTextManager;
            _shellOpenDocument = serviceProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

            // Ensure the options factory services are initialized on the UI thread
            this.Services.GetService<IOptionService>();

            var session = serviceProvider.GetService(typeof(SVsLog)) as IVsSqmMulti;
            var profileService = serviceProvider.GetService(typeof(SVsFeedbackProfile)) as IVsFeedbackProfile;

            // We have Watson hits where this came back null, so guard against it
            if (profileService != null)
            {
                Sqm.LogSession(session, profileService.IsMicrosoftInternal);
            }
        }

        internal static HostServices CreateHostServices(SVsServiceProvider serviceProvider)
        {
            var composition = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            return MefV1HostServices.Create(composition.DefaultExportProvider);
        }

        protected void InitializeStandardVisualStudioWorkspace(SVsServiceProvider serviceProvider, SaveEventsService saveEventsService)
        {
            var projectTracker = new VisualStudioProjectTracker(serviceProvider);

            // Ensure the document tracking service is initialized on the UI thread
            var documentTrackingService = this.Services.GetService<IDocumentTrackingService>();
            var documentProvider = new RoslynDocumentProvider(projectTracker, serviceProvider, documentTrackingService);
            projectTracker.DocumentProvider = documentProvider;

            projectTracker.MetadataReferenceProvider = this.Services.GetService<VisualStudioMetadataReferenceManager>();
            projectTracker.RuleSetFileProvider = this.Services.GetService<VisualStudioRuleSetManager>();

            this.SetProjectTracker(projectTracker);

            var workspaceHost = new VisualStudioWorkspaceHost(this);
            projectTracker.RegisterWorkspaceHost(workspaceHost);
            projectTracker.StartSendingEventsToWorkspaceHost(workspaceHost);

            saveEventsService.StartSendingSaveEvents();

            // Ensure the options factory services are initialized on the UI thread
            this.Services.GetService<IOptionService>();
        }

        /// <summary>NOTE: Call only from derived class constructor</summary>
        protected void SetProjectTracker(VisualStudioProjectTracker projectTracker)
        {
            _projectTracker = projectTracker;
        }

        internal VisualStudioProjectTracker ProjectTracker
        {
            get
            {
                return _projectTracker;
            }
        }

        internal void ClearReferenceCache()
        {
            _projectTracker.MetadataReferenceProvider.ClearCache();
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

        internal IVisualStudioHostProject GetHostProject(ProjectId projectId)
        {
            return this.ProjectTracker.GetProject(projectId);
        }

        private bool TryGetHostProject(ProjectId projectId, out IVisualStudioHostProject project)
        {
            project = GetHostProject(projectId);
            return project != null;
        }

        public override bool TryApplyChanges(Microsoft.CodeAnalysis.Solution newSolution)
        {
            // first make sure we can edit the document we will be updating (check them out from source control, etc)
            var changedDocs = newSolution.GetChanges(this.CurrentSolution).GetProjectChanges().SelectMany(pd => pd.GetChangedDocuments()).ToList();
            if (changedDocs.Count > 0)
            {
                this.EnsureEditableDocuments(changedDocs);
            }

            return base.TryApplyChanges(newSolution);
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
                    return true;

                default:
                    return false;
            }
        }

        private bool TryGetProjectData(ProjectId projectId, out IVisualStudioHostProject hostProject, out IVsHierarchy hierarchy, out EnvDTE.Project project)
        {
            hierarchy = null;
            project = null;

            return this.TryGetHostProject(projectId, out hostProject)
                && this.TryGetHierarchy(projectId, out hierarchy)
                && hierarchy.TryGetProject(out project);
        }

        internal void GetProjectData(ProjectId projectId, out IVisualStudioHostProject hostProject, out IVsHierarchy hierarchy, out EnvDTE.Project project)
        {
            if (!TryGetProjectData(projectId, out hostProject, out hierarchy, out project))
            {
                throw new ArgumentException(string.Format(ServicesVSResources.CouldNotFindProject, projectId));
            }
        }

        internal bool TryAddReferenceToProject(ProjectId projectId, string assemblyName)
        {
            IVisualStudioHostProject hostProject;
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            try
            {
                GetProjectData(projectId, out hostProject, out hierarchy, out project);
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

            IVisualStudioHostProject hostProject;
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            GetProjectData(projectId, out hostProject, out hierarchy, out project);

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

            IVisualStudioHostProject hostProject;
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            GetProjectData(projectId, out hostProject, out hierarchy, out project);

            string filePath = GetAnalyzerPath(analyzerReference);
            if (filePath != null)
            {
                VSProject3 vsProject = (VSProject3)project.Object;
                vsProject.AnalyzerReferences.Remove(filePath);
            }
        }

        private string GetMetadataPath(MetadataReference metadataReference)
        {
            var fileMetadata = metadataReference as PortableExecutableReference;
            if (fileMetadata != null)
            {
                return fileMetadata.FilePath;
            }

            return null;
        }

        protected override void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (metadataReference == null)
            {
                throw new ArgumentNullException(nameof(metadataReference));
            }

            IVisualStudioHostProject hostProject;
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            GetProjectData(projectId, out hostProject, out hierarchy, out project);

            string filePath = GetMetadataPath(metadataReference);
            if (filePath != null)
            {
                VSProject vsProject = (VSProject)project.Object;
                vsProject.References.Add(filePath);
            }
        }

        protected override void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (metadataReference == null)
            {
                throw new ArgumentNullException(nameof(metadataReference));
            }

            IVisualStudioHostProject hostProject;
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            GetProjectData(projectId, out hostProject, out hierarchy, out project);

            string filePath = GetMetadataPath(metadataReference);
            if (filePath != null)
            {
                VSLangProj.VSProject vsProject = (VSLangProj.VSProject)project.Object;
                VSLangProj.Reference reference = vsProject.References.Find(filePath);
                if (reference != null)
                {
                    reference.Remove();
                }
            }
        }

        protected override void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (projectReference == null)
            {
                throw new ArgumentNullException(nameof(projectReference));
            }

            IVisualStudioHostProject hostProject;
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            GetProjectData(projectId, out hostProject, out hierarchy, out project);

            IVisualStudioHostProject refHostProject;
            IVsHierarchy refHierarchy;
            EnvDTE.Project refProject;
            GetProjectData(projectReference.ProjectId, out refHostProject, out refHierarchy, out refProject);

            VSLangProj.VSProject vsProject = (VSLangProj.VSProject)project.Object;
            vsProject.References.AddProject(refProject);
        }

        protected override void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (projectReference == null)
            {
                throw new ArgumentNullException(nameof(projectReference));
            }

            IVisualStudioHostProject hostProject;
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            GetProjectData(projectId, out hostProject, out hierarchy, out project);

            IVisualStudioHostProject refHostProject;
            IVsHierarchy refHierarchy;
            EnvDTE.Project refProject;
            GetProjectData(projectReference.ProjectId, out refHostProject, out refHierarchy, out refProject);

            VSLangProj.VSProject vsProject = (VSLangProj.VSProject)project.Object;
            foreach (VSLangProj.Reference reference in vsProject.References)
            {
                if (reference.SourceProject == refProject)
                {
                    reference.Remove();
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
            IVsHierarchy hierarchy;
            EnvDTE.Project project;
            IVisualStudioHostProject hostProject;
            GetProjectData(info.Id.ProjectId, out hostProject, out hierarchy, out project);

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
            IVisualStudioHostProject hostProject,
            EnvDTE.Project project,
            DocumentId documentId,
            string documentName,
            SourceCodeKind sourceCodeKind,
            SourceText initialText = null,
            string filePath = null,
            bool isAdditionalDocument = false)
        {
            string folderPath;
            if (!project.TryGetFullPath(out folderPath))
            {
                // TODO(cyrusn): Throw an appropriate exception here.
                throw new Exception(ServicesVSResources.CouldNotFindLocationOfFol);
            }

            return AddDocumentToProjectItems(hostProject, project.ProjectItems, documentId, folderPath, documentName, sourceCodeKind, initialText, filePath, isAdditionalDocument);
        }

        private ProjectItem AddDocumentToFolder(
            IVisualStudioHostProject hostProject,
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

            string folderPath;
            if (!folder.TryGetFullPath(out folderPath))
            {
                // TODO(cyrusn): Throw an appropriate exception here.
                throw new Exception(ServicesVSResources.CouldNotFindLocationOfFol);
            }

            return AddDocumentToProjectItems(hostProject, folder.ProjectItems, documentId, folderPath, documentName, sourceCodeKind, initialText, filePath, isAdditionalDocument);
        }

        private ProjectItem AddDocumentToProjectItems(
            IVisualStudioHostProject hostProject,
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

            using (var documentIdHint = _projectTracker.DocumentProvider.ProvideDocumentIdHint(filePath, documentId))
            {
                return projectItems.AddFromFile(filePath);
            }
        }

        protected void RemoveDocumentCore(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            var document = this.GetHostDocument(documentId);
            if (document != null)
            {
                var project = document.Project.Hierarchy as IVsProject3;

                var itemId = document.GetItemId();
                if (itemId == (uint)VSConstants.VSITEMID.Nil)
                {
                    // it is no longer part of the solution
                    return;
                }

                int result;
                project.RemoveItem(0, itemId, out result);
            }
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
        {
            RemoveDocumentCore(documentId);
        }

        protected override void ApplyAdditionalDocumentRemoved(DocumentId documentId)
        {
            RemoveDocumentCore(documentId);
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

        public bool TryGetInfoBarData(out IVsWindowFrame frame, out IVsInfoBarUIFactory factory)
        {
            frame = null;
            factory = null;
            var monitorSelectionService = ServiceProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            object value = null;

            // We want to get whichever window is currently in focus (including toolbars) as we could have had an exception thrown from the error list or interactive window
            if (monitorSelectionService != null &&
               ErrorHandler.Succeeded(monitorSelectionService.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out value)))
            {
                frame = value as IVsWindowFrame;
            }
            else
            {
                return false;
            }

            factory = ServiceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
            return frame != null && factory != null;
        }

        public void OpenDocumentCore(DocumentId documentId, bool activate = true)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (!_foregroundObject.IsForeground())
            {
                throw new InvalidOperationException(ServicesVSResources.ThisWorkspaceOnlySupportsOpeningDocumentsOnTheUIThread);
            }

            var document = this.GetHostDocument(documentId);
            if (document != null && document.Project != null)
            {
                IVsWindowFrame frame;
                if (TryGetFrame(document, out frame))
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

                uint itemid;
                IVsUIHierarchy uiHierarchy;
                OLEServiceProvider oleServiceProvider;

                return ErrorHandler.Succeeded(_shellOpenDocument.OpenDocumentViaProject(
                    document.FilePath,
                    VSConstants.LOGVIEWID.TextView_guid,
                    out oleServiceProvider,
                    out uiHierarchy,
                    out itemid,
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
                    IVsUIHierarchy uiHierarchy;
                    IVsWindowFrame frame;
                    int isOpen;
                    if (ErrorHandler.Succeeded(_shellOpenDocument.IsDocumentOpen(null, 0, document.FilePath, Guid.Empty, 0, out uiHierarchy, null, out frame, out isOpen)))
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

        private static string GetPreferredExtension(IVisualStudioHostProject hostProject, SourceCodeKind sourceCodeKind)
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
                        ProjectTracker.GetProject(documentId.ProjectId).ProjectSystemName) == VSConstants.S_OK)
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

            var hostProject = LinkedFileUtilities.GetContextHostProject(sharedHierarchy, ProjectTracker);
            if (hostProject.Hierarchy == sharedHierarchy)
            {
                // How?
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
            var hostProject = LinkedFileUtilities.GetContextHostProject(sharedHierarchy, ProjectTracker);
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

        private bool TryGetHierarchy(ProjectId projectId, out IVsHierarchy hierarchy)
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

            base.Dispose(finalize);
        }

        public void EnsureEditableDocuments(IEnumerable<DocumentId> documents)
        {
            var queryEdit = (IVsQueryEditQuerySave2)ServiceProvider.GetService(typeof(SVsQueryEditQuerySave));

            var fileNames = documents.Select(GetFilePath).ToArray();

            uint editVerdict;
            uint editResultFlags;

            // TODO: meditate about the flags we can pass to this and decide what is most appropriate for Roslyn
            int result = queryEdit.QueryEditFiles(
                rgfQueryEdit: 0,
                cFiles: fileNames.Length,
                rgpszMkDocuments: fileNames,
                rgrgf: new uint[fileNames.Length],
                rgFileInfo: new VSQEQS_FILE_ATTRIBUTE_DATA[fileNames.Length],
                pfEditVerdict: out editVerdict,
                prgfMoreInfo: out editResultFlags);

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

        public TInterface GetVsService<TService, TInterface>()
            where TService : class
            where TInterface : class
        {
            return this.ServiceProvider.GetService(typeof(TService)) as TInterface;
        }

        public object GetVsService(Type serviceType)
        {
            return ServiceProvider.GetService(serviceType);
        }

        public DTE GetVsDte()
        {
            return GetVsService<SDTE, DTE>();
        }

        /// <summary>
        /// A trivial implementation of <see cref="IVisualStudioWorkspaceHost" /> that just
        /// forwards the calls down to the underlying Workspace.
        /// </summary>
        protected class VisualStudioWorkspaceHost : IVisualStudioWorkspaceHost, IVisualStudioWorkingFolder
        {
            private readonly VisualStudioWorkspaceImpl _workspace;

            private Dictionary<DocumentId, uint> _documentIdToHierarchyEventsCookieMap = new Dictionary<DocumentId, uint>();

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
                    uint cookie;
                    var eventSink = new HierarchyEventsSink(_workspace, sharedHierarchy, documentId);
                    var hr = sharedHierarchy.AdviseHierarchyEvents(eventSink, out cookie);

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
                    uint cookie;
                    if (_documentIdToHierarchyEventsCookieMap.TryGetValue(documentId, out cookie))
                    {
                        var hr = sharedHierarchy.UnadviseHierarchyEvents(cookie);
                        _documentIdToHierarchyEventsCookieMap.Remove(documentId);
                    }
                }
            }

            private void RegisterPrimarySolutionForPersistentStorage(SolutionId solutionId)
            {
                var service = _workspace.Services.GetService<IPersistentStorageService>() as PersistentStorageService;
                if (service == null)
                {
                    return;
                }

                service.RegisterPrimarySolution(solutionId);
            }

            private void UnregisterPrimarySolutionForPersistentStorage(SolutionId solutionId, bool synchronousShutdown)
            {
                var service = _workspace.Services.GetService<IPersistentStorageService>() as PersistentStorageService;
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

            void IVisualStudioWorkspaceHost.OnAdditionalDocumentAdded(DocumentInfo additionalDocumentInfo)
            {
                _workspace.OnAdditionalDocumentAdded(additionalDocumentInfo);
            }

            void IVisualStudioWorkspaceHost.OnAdditionalDocumentRemoved(DocumentId additionalDocumentId)
            {
                _workspace.OnAdditionalDocumentRemoved(additionalDocumentId);
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

            void IVisualStudioWorkingFolder.OnBeforeWorkingFolderChange()
            {
                UnregisterPrimarySolutionForPersistentStorage(_workspace.CurrentSolution.Id, synchronousShutdown: true);
            }

            void IVisualStudioWorkingFolder.OnAfterWorkingFolderChange()
            {
                var solutionId = _workspace.CurrentSolution.Id;

                _workspace.ProjectTracker.UpdateSolutionProperties(solutionId);
                RegisterPrimarySolutionForPersistentStorage(solutionId);
            }
        }
    }
}
