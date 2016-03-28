// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(MiscellaneousFilesWorkspace))]
    internal sealed partial class MiscellaneousFilesWorkspace : Workspace, IVsRunningDocTableEvents2, IVisualStudioHostProjectContainer
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IMetadataAsSourceFileService _fileTrackingMetadataAsSourceService;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private readonly IVsTextManager _textManager;

        private readonly RoslynDocumentProvider _documentProvider;

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

        // document worker coordinator
        private ISolutionCrawlerRegistrationService _registrationService;

        [ImportingConstructor]
        public MiscellaneousFilesWorkspace(
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IMetadataAsSourceFileService fileTrackingMetadataAsSourceService,
            SaveEventsService saveEventsService,
            VisualStudioWorkspace visualStudioWorkspace,
            SVsServiceProvider serviceProvider) :
            base(visualStudioWorkspace.Services.HostServices, "MiscellaneousFiles")
        {
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _fileTrackingMetadataAsSourceService = fileTrackingMetadataAsSourceService;
            _runningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            _textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));

            ((IVsRunningDocumentTable)_runningDocumentTable).AdviseRunningDocTableEvents(this, out _runningDocumentTableEventsCookie);

            _metadataReferences = ImmutableArray.CreateRange(CreateMetadataReferences());
            _documentProvider = new RoslynDocumentProvider(this, serviceProvider);
            saveEventsService.StartSendingSaveEvents();
        }

        public void RegisterLanguage(Guid languageGuid, string languageName, string scriptExtension, ParseOptions parseOptions)
        {
            _languageInformationByLanguageGuid.Add(languageGuid, new LanguageInformation(languageName, scriptExtension, parseOptions));
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

        private LanguageInformation TryGetLanguageInformation(string filename)
        {
            Guid fileLanguageGuid;
            LanguageInformation languageInformation = null;

            if (ErrorHandler.Succeeded(_textManager.MapFilenameToLanguageSID(filename, out fileLanguageGuid)))
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
            if (dwReadLocksRemaining + dwEditLocksRemaining == 0)
            {
                TryUntrackClosingDocument(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
            }

            return VSConstants.S_OK;
        }

        private void TrackOpenedDocument(uint docCookie, string moniker)
        {
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
            var workspaceRegistration = (WorkspaceRegistration)sender;
            uint docCookie;

            // external workspace that listens to events from IVSRunningDocumentTable4 (just like MiscellaneousFilesWorkspace does) and registers the text buffer for opened document prior to Miscellaneous workspace getting notified of the docCookie - the prior contract assumes that MiscellaneousFilesWorkspace is always the first one to get notified
            if (!_docCookieToWorkspaceRegistration.TryGetKey(workspaceRegistration, out docCookie))
            {
                // We haven't even started tracking the document corresponding to this registration - likely because some external workspace registered the document's buffer prior to MiscellaneousWorkspace getting notified of it.
                // Just bail out for now, we will eventually receive the opened document notification and track its docCookie registration.
                return;
            }

            var moniker = _runningDocumentTable.GetDocumentMoniker(docCookie);

            if (workspaceRegistration.Workspace == null)
            {
                HostProject hostProject;

                if (_docCookiesToHostProject.TryGetValue(docCookie, out hostProject))
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
                    // We should now claim this
                    AttachToDocument(docCookie, moniker);
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
            WorkspaceRegistration registration;
            if (_docCookieToWorkspaceRegistration.TryGetValue(docCookie, out registration))
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
            var parseOptions = languageInformation.ParseOptions;

            if (Path.GetExtension(moniker) == languageInformation.ScriptExtension)
            {
                parseOptions = parseOptions.WithKind(SourceCodeKind.Script);
            }

            // First, create the project
            var hostProject = new HostProject(this, CurrentSolution.Id, languageInformation.LanguageName, parseOptions, _metadataReferences);

            // Now try to find the document. We accept any text buffer, since we've already verified it's an appropriate file in ShouldIncludeFile.
            var document = _documentProvider.TryGetDocumentForFile(
                hostProject,
                (uint)VSConstants.VSITEMID.Nil,
                moniker,
                parseOptions.Kind,
                isGenerated: false,
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
            _documentProvider.NotifyDocumentRegisteredToProject(document);

            Contract.ThrowIfFalse(document.IsOpen);

            var buffer = document.GetOpenTextBuffer();
            OnDocumentOpened(document.Id, document.GetOpenTextContainer());

            _docCookiesToHostProject.Add(docCookie, hostProject);
        }

        private void DetachFromDocument(uint docCookie, string moniker)
        {
            HostProject hostProject;

            if (_fileTrackingMetadataAsSourceService.TryRemoveDocumentFromWorkspace(moniker))
            {
                return;
            }

            if (_docCookiesToHostProject.TryGetValue(docCookie, out hostProject))
            {
                var document = hostProject.Document;

                OnDocumentClosed(document.Id, document.Loader);
                OnDocumentRemoved(document.Id);
                OnProjectRemoved(hostProject.Id);

                _hostProjects.Remove(hostProject.Id);
                _docCookiesToHostProject.Remove(docCookie);

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
            HostProject project;
            _hostProjects.TryGetValue(id, out project);
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

        IEnumerable<IVisualStudioHostProject> IVisualStudioHostProjectContainer.GetProjects()
        {
            return _hostProjects.Values;
        }

        void IVisualStudioHostProjectContainer.NotifyNonDocumentOpenedForProject(IVisualStudioHostProject project)
        {
            // Since the MiscellaneousFilesWorkspace doesn't do anything lazily, this is a no-op
        }

        private class LanguageInformation
        {
            public LanguageInformation(string languageName, string scriptExtension, ParseOptions parseOptions)
            {
                this.LanguageName = languageName;
                this.ScriptExtension = scriptExtension;
                this.ParseOptions = parseOptions;
            }

            public string LanguageName { get; }
            public string ScriptExtension { get; }
            public ParseOptions ParseOptions { get; }
        }
    }
}
