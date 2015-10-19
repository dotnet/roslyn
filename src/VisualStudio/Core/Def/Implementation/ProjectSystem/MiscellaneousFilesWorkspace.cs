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

        private readonly HashSet<DocumentKey> _filesInProjects = new HashSet<DocumentKey>();

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

        internal void OnFileIncludedInProject(IVisualStudioHostDocument document)
        {
            uint docCookie;
            if (_runningDocumentTable.TryGetCookieForInitializedDocument(document.FilePath, out docCookie))
            {
                TryRemoveDocumentFromMiscellaneousWorkspace(docCookie, document.FilePath);
            }

            _filesInProjects.Add(document.Key);
        }

        internal void OnFileRemovedFromProject(IVisualStudioHostDocument document)
        {
            // Remove the document key from the filesInProjects map first because adding documents
            // to the misc files workspace requires that they not appear in this map.
            _filesInProjects.Remove(document.Key);

            uint docCookie;
            if (_runningDocumentTable.TryGetCookieForInitializedDocument(document.Key.Moniker, out docCookie))
            {
                AddDocumentToMiscellaneousOrMetadataAsSourceWorkspace(docCookie, document.Key.Moniker);
            }
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
                if (TryRemoveDocumentFromMiscellaneousWorkspace(docCookie, pszMkDocumentOld) || TryGetLanguageInformation(pszMkDocumentOld) == null)
                {
                    // Add the new one, if appropriate. 
                    AddDocumentToMiscellaneousOrMetadataAsSourceWorkspace(docCookie, pszMkDocumentNew);
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
                    AddDocumentToMiscellaneousOrMetadataAsSourceWorkspace(docCookie, moniker);
                }
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
                TryRemoveDocumentFromMiscellaneousWorkspace(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
            }

            return VSConstants.S_OK;
        }

        private void AddDocumentToMiscellaneousOrMetadataAsSourceWorkspace(uint docCookie, string moniker)
        {
            var languageInformation = TryGetLanguageInformation(moniker);

            if (languageInformation != null &&
                !_filesInProjects.Any(d => StringComparer.OrdinalIgnoreCase.Equals(d.Moniker, moniker)) &&
                !_docCookiesToHostProject.ContainsKey(docCookie))
            {
                // See if we should push to this to the metadata-to-source workspace instead.
                if (_runningDocumentTable.IsDocumentInitialized(docCookie))
                {
                    var vsTextBuffer = (IVsTextBuffer)_runningDocumentTable.GetDocumentData(docCookie);
                    var textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(vsTextBuffer);

                    if (_fileTrackingMetadataAsSourceService.TryAddDocumentToWorkspace(moniker, textBuffer))
                    {
                        // We already added it, so we will keep it excluded from the misc files workspace
                        return;
                    }
                }

                var parseOptions = languageInformation.ParseOptions;

                if (Path.GetExtension(moniker) == languageInformation.ScriptExtension)
                {
                    parseOptions = parseOptions.WithKind(SourceCodeKind.Script);
                }

                // First, create the project
                var hostProject = new HostProject(this, CurrentSolution.Id, languageInformation.LanguageName, parseOptions, _metadataReferences);

                // Now try to find the document. We accept any text buffer, since we've already verified it's an appropriate file in ShouldIncludeFile.
                var document = _documentProvider.TryGetDocumentForFile(hostProject, (uint)VSConstants.VSITEMID.Nil, moniker, parseOptions.Kind, t => true);

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
        }

        private bool TryRemoveDocumentFromMiscellaneousWorkspace(uint docCookie, string moniker)
        {
            HostProject hostProject;

            if (_fileTrackingMetadataAsSourceService.TryRemoveDocumentFromWorkspace(moniker))
            {
                return true;
            }

            if (_docCookiesToHostProject.TryGetValue(docCookie, out hostProject))
            {
                var document = hostProject.Document;

                OnDocumentClosed(document.Id, document.Loader);
                OnDocumentRemoved(document.Id);
                OnProjectRemoved(hostProject.Id);

                _hostProjects.Remove(hostProject.Id);
                _docCookiesToHostProject.Remove(docCookie);

                return true;
            }

            return false;
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
