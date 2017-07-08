// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(MiscellaneousFilesWorkspace))]
    internal sealed partial class MiscellaneousFilesWorkspace : Workspace, IVsRunningDocTableEvents2, IVisualStudioHostProjectContainer
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IMetadataAsSourceFileService _fileTrackingMetadataAsSourceService;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private readonly IVsTextManager _textManager;

        private readonly DocumentProvider _documentProvider;

        private readonly Dictionary<Guid, LanguageInformation> _languageInformationByLanguageGuid = new Dictionary<Guid, LanguageInformation>();

        /// <summary>
        /// <see cref="WorkspaceRegistration"/> instances for all open buffers being tracked by by this object
        /// for possible inclusion into this workspace.
        /// </summary>
        private IBidirectionalMap<uint, WorkspaceRegistration> _docCookieToWorkspaceRegistration = BidirectionalMap<uint, WorkspaceRegistration>.Empty;

        private readonly Dictionary<ProjectId, HostProject> _hostProjects = new Dictionary<ProjectId, HostProject>();
        private readonly Dictionary<uint, HostProject> _docCookiesToHostProject = new Dictionary<uint, HostProject>();

        private readonly ImmutableArray<MetadataReference> _metadataReferences;
        private uint _runningDocumentTableEventsCookie;

        private readonly ForegroundThreadAffinitizedObject _foregroundThreadAffinitization;

        [ImportingConstructor]
        public MiscellaneousFilesWorkspace(
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IMetadataAsSourceFileService fileTrackingMetadataAsSourceService,
            SaveEventsService saveEventsService,
            VisualStudioWorkspace visualStudioWorkspace,
            SVsServiceProvider serviceProvider) :
            base(visualStudioWorkspace.Services.HostServices, WorkspaceKind.MiscellaneousFiles)
        {
            _foregroundThreadAffinitization = new ForegroundThreadAffinitizedObject(assertIsForeground: true);

            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _fileTrackingMetadataAsSourceService = fileTrackingMetadataAsSourceService;
            _runningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            _textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));

            ((IVsRunningDocumentTable)_runningDocumentTable).AdviseRunningDocTableEvents(this, out _runningDocumentTableEventsCookie);

            _metadataReferences = ImmutableArray.CreateRange(CreateMetadataReferences());
            _documentProvider = new DocumentProvider(this, serviceProvider, documentTrackingService: null);
            saveEventsService.StartSendingSaveEvents();
        }

        public void RegisterLanguage(Guid languageGuid, string languageName, string scriptExtension)
        {
            _languageInformationByLanguageGuid.Add(languageGuid, new LanguageInformation(languageName, scriptExtension));
        }

        internal void StartSolutionCrawler()
        {
            DiagnosticProvider.Enable(this, DiagnosticProvider.Options.Syntax);
        }

        internal void StopSolutionCrawler()
        {
            DiagnosticProvider.Disable(this);
        }

        private LanguageInformation TryGetLanguageInformation(string filename)
        {
            LanguageInformation languageInformation = null;

            if (ErrorHandler.Succeeded(_textManager.MapFilenameToLanguageSID(filename, out var fileLanguageGuid)))
            {
                _languageInformationByLanguageGuid.TryGetValue(fileLanguageGuid, out languageInformation);
            }

            return languageInformation;
        }

        private IEnumerable<MetadataReference> CreateMetadataReferences()
        {
            var manager = this.Services.GetService<VisualStudioMetadataReferenceManager>();
            var searchPaths = ReferencePathUtilities.GetReferencePaths();

            return from fileName in new[] { "mscorlib.dll", "System.dll", "System.Core.dll" }
                   let fullPath = FileUtilities.ResolveRelativePath(fileName, basePath: null, baseDirectory: null, searchPaths: searchPaths, fileExists: File.Exists)
                   where fullPath != null
                   select manager.CreateMetadataReferenceSnapshot(fullPath, MetadataReferenceProperties.Assembly);
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            // Did we rename?
            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_MkDocument) != 0)
            {
                // We want to consider this file to be added in one of two situations:
                //
                // 1) the old file already was a misc file, at which point we might just be doing a rename from
                //    one name to another with the same extension
                // 2) the old file was a different extension that we weren't tracking, which may have now changed
                if (TryUntrackClosingDocument(docCookie, pszMkDocumentOld) || TryGetLanguageInformation(pszMkDocumentOld) == null)
                {
                    // Add the new one, if appropriate. 
                    TrackOpenedDocument(docCookie, pszMkDocumentNew);
                }
            }

            // When starting a diff, the RDT doesn't call OnBeforeDocumentWindowShow, but it does call 
            // OnAfterAttributeChangeEx for the temporary buffer. The native IDE used this even to 
            // add misc files, so we'll do the same.
            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_DocDataReloaded) != 0)
            {
                var moniker = _runningDocumentTable.GetDocumentMoniker(docCookie);

                if (moniker != null && TryGetLanguageInformation(moniker) != null && !_docCookiesToHostProject.ContainsKey(docCookie))
                {
                    TrackOpenedDocument(docCookie, moniker);
                }
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB3.RDTA_DocumentInitialized) != 0)
            {
                // The document is now initialized, we should try tracking it
                TrackOpenedDocument(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
            }

            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            _foregroundThreadAffinitization.AssertIsForeground();

            if (dwReadLocksRemaining + dwEditLocksRemaining == 0)
            {
                TryUntrackClosingDocument(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
            }

            return VSConstants.S_OK;
        }

        private void TrackOpenedDocument(uint docCookie, string moniker)
        {
            _foregroundThreadAffinitization.AssertIsForeground();

            var languageInformation = TryGetLanguageInformation(moniker);

            if (languageInformation == null)
            {
                // We can never put this document in a workspace, so just bail
                return;
            }

            // We don't want to realize the document here unless it's already initialized. Document initialization is watched in 
            // OnAfterAttributeChangeEx and will retrigger this if it wasn't already done.
            if (_runningDocumentTable.IsDocumentInitialized(docCookie) && !_docCookieToWorkspaceRegistration.ContainsKey(docCookie))
            {
                var vsTextBuffer = (IVsTextBuffer)_runningDocumentTable.GetDocumentData(docCookie);
                var textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(vsTextBuffer);

                // As long as the buffer is initialized, then we should see if we should attach
                if (textBuffer != null)
                {
                    var registration = Workspace.GetWorkspaceRegistration(textBuffer.AsTextContainer());

                    registration.WorkspaceChanged += Registration_WorkspaceChanged;
                    _docCookieToWorkspaceRegistration = _docCookieToWorkspaceRegistration.Add(docCookie, registration);

                    if (!IsClaimedByAnotherWorkspace(registration))
                    {
                        AttachToDocument(docCookie, moniker);
                    }
                }
            }
        }

        private void Registration_WorkspaceChanged(object sender, EventArgs e)
        {
            // We may or may not be getting this notification from the foreground thread if another workspace
            // is raising events on a background. Let's send it back to the UI thread since we can't talk
            // to the RDT in the background thread. Since this is all asynchronous a bit more asynchrony is fine.
            if (!_foregroundThreadAffinitization.IsForeground())
            {
                ScheduleTask(() => Registration_WorkspaceChanged(sender, e));
                return;
            }

            _foregroundThreadAffinitization.AssertIsForeground();

            var workspaceRegistration = (WorkspaceRegistration)sender;

            // Since WorkspaceChanged notifications may be asynchronous and happened on a different thread,
            // we might have already unsubscribed for this synchronously from the RDT while we were in the process of sending this
            // request back to the UI thread.
            if (!_docCookieToWorkspaceRegistration.TryGetKey(workspaceRegistration, out var docCookie))
            {
                return;
            }

            // It's also theoretically possible that we are getting notified about a workspace change to a document that has
            // been simultaneously removed from the RDT but we haven't gotten the notification. In that case, also bail.
            if (!_runningDocumentTable.IsCookieValid(docCookie))
            {
                return;
            }

            var moniker = _runningDocumentTable.GetDocumentMoniker(docCookie);

            if (workspaceRegistration.Workspace == null)
            {
                if (_docCookiesToHostProject.TryGetValue(docCookie, out var hostProject))
                {
                    // The workspace was taken from us and released and we have only asynchronously found out now.
                    var document = hostProject.Document;

                    if (document.IsOpen)
                    {
                        RegisterText(document.GetOpenTextContainer());
                    }
                }
                else
                {
                    // We should now try to claim this. The moniker we have here is the moniker after the rename if we're currently processing
                    // a rename. It's possible in that case that this is being closed by the other workspace due to that rename. If the rename
                    // is changing or removing the file extension, we wouldn't want to try attaching, which is why we have to re-check
                    // the moniker. Once we observe the rename later in OnAfterAttributeChangeEx we'll completely disconnect.
                    if (TryGetLanguageInformation(moniker) != null)
                    {
                        AttachToDocument(docCookie, moniker);
                    }
                }
            }
            else if (IsClaimedByAnotherWorkspace(workspaceRegistration))
            {
                // It's now claimed by another workspace, so we should unclaim it
                if (_docCookiesToHostProject.ContainsKey(docCookie))
                {
                    DetachFromDocument(docCookie, moniker);
                }
            }
        }

        /// <summary>
        /// Stops tracking a document in the RDT for whether we should attach to it.
        /// </summary>
        /// <returns>true if we were previously tracking it.</returns>
        private bool TryUntrackClosingDocument(uint docCookie, string moniker)
        {
            bool unregisteredRegistration = false;
            // Remove our registration changing handler before we call DetachFromDocument. Otherwise, calling DetachFromDocument
            // causes us to set the workspace to null, which we then respond to as an indication that we should
            // attach again.
            if (_docCookieToWorkspaceRegistration.TryGetValue(docCookie, out var registration))
            {
                registration.WorkspaceChanged -= Registration_WorkspaceChanged;
                _docCookieToWorkspaceRegistration = _docCookieToWorkspaceRegistration.RemoveKey(docCookie);
                unregisteredRegistration = true;
            }

            DetachFromDocument(docCookie, moniker);

            return unregisteredRegistration;
        }

        private bool IsClaimedByAnotherWorkspace(WorkspaceRegistration registration)
        {
            // Currently, we are also responsible for pushing documents to the metadata as source workspace,
            // so we count that here as well
            return registration.Workspace != null && registration.Workspace.Kind != WorkspaceKind.MetadataAsSource && registration.Workspace.Kind != WorkspaceKind.MiscellaneousFiles;
        }

        private void AttachToDocument(uint docCookie, string moniker)
        {
            _foregroundThreadAffinitization.AssertIsForeground();

            var vsTextBuffer = (IVsTextBuffer)_runningDocumentTable.GetDocumentData(docCookie);
            var textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(vsTextBuffer);

            if (_fileTrackingMetadataAsSourceService.TryAddDocumentToWorkspace(moniker, textBuffer))
            {
                // We already added it, so we will keep it excluded from the misc files workspace
                return;
            }

            // This should always succeed since we only got here if we already confirmed the moniker is acceptable
            var languageInformation = TryGetLanguageInformation(moniker);
            Contract.ThrowIfNull(languageInformation);

            var languageServices = Services.GetLanguageServices(languageInformation.LanguageName);
            var compilationOptionsOpt = languageServices.GetService<ICompilationFactoryService>()?.GetDefaultCompilationOptions();
            var parseOptionsOpt = languageServices.GetService<ISyntaxTreeFactoryService>()?.GetDefaultParseOptions();

            if (parseOptionsOpt != null && 
                compilationOptionsOpt != null &&
                PathUtilities.GetExtension(moniker) == languageInformation.ScriptExtension)
            {
                parseOptionsOpt = parseOptionsOpt.WithKind(SourceCodeKind.Script);

                var metadataService = Services.GetService<IMetadataService>();
                var directory = PathUtilities.GetDirectoryName(moniker);

                // TODO (https://github.com/dotnet/roslyn/issues/5325, https://github.com/dotnet/roslyn/issues/13886): 
                // - Need to have a way to specify these somewhere in VS options.
                // - Use RuntimeMetadataReferenceResolver like in InteractiveEvaluator.CreateMetadataReferenceResolver
                // - Add default namespace imports
                // - Add default script globals available in 'csi foo.csx' environment: CommandLineScriptGlobals

                var referenceSearchPaths = ImmutableArray<string>.Empty;
                var sourceSearchPaths = ImmutableArray<string>.Empty;

                var referenceResolver = new WorkspaceMetadataFileReferenceResolver(
                    metadataService,
                    new RelativePathResolver(referenceSearchPaths, directory));

                compilationOptionsOpt = compilationOptionsOpt.
                    WithMetadataReferenceResolver(referenceResolver).
                    WithSourceReferenceResolver(new SourceFileResolver(sourceSearchPaths, directory));
            }

            // First, create the project
            var hostProject = new HostProject(this, CurrentSolution.Id, languageInformation.LanguageName, parseOptionsOpt, compilationOptionsOpt, _metadataReferences);

            // Now try to find the document. We accept any text buffer, since we've already verified it's an appropriate file in ShouldIncludeFile.
            var document = _documentProvider.TryGetDocumentForFile(
                hostProject,
                moniker,
                parseOptionsOpt?.Kind ?? SourceCodeKind.Regular,
                getFolderNames: _ => SpecializedCollections.EmptyReadOnlyList<string>(),
                canUseTextBuffer: _ => true);

            // If the buffer has not yet been initialized, we won't get a document.
            if (document == null)
            {
                return;
            }

            // Since we have a document, we can do the rest of the project setup.
            _hostProjects.Add(hostProject.Id, hostProject);
            OnProjectAdded(hostProject.CreateProjectInfoForCurrentState());

            OnDocumentAdded(document.GetInitialState());
            hostProject.Document = document;

            // Notify the document provider, so it knows the document is now open and a part of
            // the project
            _documentProvider.NotifyDocumentRegisteredToProjectAndStartToRaiseEvents(document);

            Contract.ThrowIfFalse(document.IsOpen);

            var buffer = document.GetOpenTextBuffer();
            OnDocumentOpened(document.Id, document.GetOpenTextContainer());

            _docCookiesToHostProject.Add(docCookie, hostProject);
        }

        private void DetachFromDocument(uint docCookie, string moniker)
        {
            _foregroundThreadAffinitization.AssertIsForeground();
            if (_fileTrackingMetadataAsSourceService.TryRemoveDocumentFromWorkspace(moniker))
            {
                return;
            }

            if (_docCookiesToHostProject.TryGetValue(docCookie, out var hostProject))
            {
                var document = hostProject.Document;

                OnDocumentClosed(document.Id, document.Loader);
                OnDocumentRemoved(document.Id);
                OnProjectRemoved(hostProject.Id);

                _hostProjects.Remove(hostProject.Id);
                _docCookiesToHostProject.Remove(docCookie);

                document.Dispose();

                return;
            }
        }

        protected override void Dispose(bool finalize)
        {
            StopSolutionCrawler();

            var runningDocumentTableForEvents = (IVsRunningDocumentTable)_runningDocumentTable;
            runningDocumentTableForEvents.UnadviseRunningDocTableEvents(_runningDocumentTableEventsCookie);
            _runningDocumentTableEventsCookie = 0;
            base.Dispose(finalize);
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.ChangeDocument:
                    return true;

                default:
                    return false;
            }
        }

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText newText)
        {
            var hostDocument = this.GetDocument(documentId);
            hostDocument.UpdateText(newText);
        }

        private HostProject GetHostProject(ProjectId id)
        {
            _hostProjects.TryGetValue(id, out var project);
            return project;
        }

        internal IVisualStudioHostDocument GetDocument(DocumentId id)
        {
            var project = GetHostProject(id.ProjectId);
            if (project != null && project.Document.Id == id)
            {
                return project.Document;
            }

            return null;
        }

        IReadOnlyList<IVisualStudioHostProject> IVisualStudioHostProjectContainer.GetProjects()
        {
            return _hostProjects.Values.ToImmutableReadOnlyListOrEmpty<IVisualStudioHostProject>();
        }

        void IVisualStudioHostProjectContainer.NotifyNonDocumentOpenedForProject(IVisualStudioHostProject project)
        {
            // Since the MiscellaneousFilesWorkspace doesn't do anything lazily, this is a no-op
        }

        private class LanguageInformation
        {
            public LanguageInformation(string languageName, string scriptExtension)
            {
                this.LanguageName = languageName;
                this.ScriptExtension = scriptExtension;
            }

            public string LanguageName { get; }
            public string ScriptExtension { get; }
        }
    }
}
