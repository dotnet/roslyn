// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// This service provides a central place where the workspace/project system layer may create
    /// Document objects that represent items from the project system. These IDocuments are useful
    /// in that they watch the running document table, tracking open/close events, and also file
    /// change events while the file is closed.
    /// </summary>
    internal abstract partial class DocumentProvider
    {
        protected readonly IVsRunningDocumentTable4 RunningDocumentTable;
        protected readonly bool IsRoslynPackageInstalled;
        protected readonly IVsEditorAdaptersFactoryService EditorAdaptersFactoryService;
        protected readonly IContentTypeRegistryService ContentTypeRegistryService;

        private readonly uint _runningDocumentTableEventCookie;

        /// <summary>
        /// The core data structure of this entire class.
        /// </summary>
        private readonly Dictionary<DocumentKey, StandardTextDocument> _documentMap = new Dictionary<DocumentKey, StandardTextDocument>();
        private Dictionary<uint, List<DocumentKey>> _docCookiesToOpenDocumentKeys = new Dictionary<uint, List<DocumentKey>>();

        private readonly Dictionary<string, DocumentId> _documentIdHints = new Dictionary<string, DocumentId>(StringComparer.OrdinalIgnoreCase);

        private readonly IVisualStudioHostProjectContainer _projectContainer;
        private readonly IVsFileChangeEx _fileChangeService;
        private readonly IVsTextManager _textManager;
        private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;

        public DocumentProvider(
            IVisualStudioHostProjectContainer projectContainer,
            IServiceProvider serviceProvider,
            bool signUpForFileChangeNotification)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

            _projectContainer = projectContainer;
            this.RunningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            this.EditorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            this.ContentTypeRegistryService = componentModel.GetService<IContentTypeRegistryService>();
            _textUndoHistoryRegistry = componentModel.GetService<ITextUndoHistoryRegistry>();
            _textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));

            // In the CodeSense scenario we will receive file change notifications from the native
            // Language Services, so we don't want to sign up for them ourselves.
            if (signUpForFileChangeNotification)
            {
                _fileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));
            }

            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            int installed;
            Marshal.ThrowExceptionForHR(shell.IsPackageInstalled(Guids.RoslynPackageId, out installed));
            IsRoslynPackageInstalled = installed != 0;

            var runningDocumentTableForEvents = (IVsRunningDocumentTable)RunningDocumentTable;
            Marshal.ThrowExceptionForHR(runningDocumentTableForEvents.AdviseRunningDocTableEvents(new RunningDocTableEventsSink(this), out _runningDocumentTableEventCookie));
        }

        public IVisualStudioHostDocument TryGetDocumentForFile(
            IVisualStudioHostProject hostProject,
            uint itemId,
            string filePath,
            SourceCodeKind sourceCodeKind,
            bool isGenerated,
            Func<ITextBuffer, bool> canUseTextBuffer)
        {
            var documentKey = new DocumentKey(hostProject, filePath);
            StandardTextDocument document;

            if (_documentMap.TryGetValue(documentKey, out document))
            {
                return document;
            }

            ITextBuffer openTextBuffer = null;
            uint foundCookie = VSConstants.VSCOOKIE_NIL;

            // If this document is already open in the editor we want to associate the text buffer with it.
            // However, determining if a document is already open is a bit complicated. With the introduction
            // of the lazy tabs feature in Dev12, a document may be open (i.e. it has a tab in the shell) but not
            // actually initialized (no data has been loaded for it because its contents have not actually been
            // viewed or used). We only care about documents that are open AND initialized.
            // That means we can't call IVsRunningDocumentTable::FindAndLockDocument to find the document; if the
            // document is open but not initialized, the call will force initialization. This is bad for two
            // reasons:
            //   1.) It circumvents lazy tabs for any document that is part of a VB or C# project.
            //   2.) Initialization may cause a whole host of other code to run synchronously, such as taggers.
            // Instead, we check if the document is already initialized, and avoid asking for the doc data and
            // hierarchy if it is not.
            if (RunningDocumentTable.TryGetCookieForInitializedDocument(documentKey.Moniker, out foundCookie))
            {
                object foundDocData = RunningDocumentTable.GetDocumentData(foundCookie);
                openTextBuffer = TryGetTextBufferFromDocData(foundDocData);
                if (openTextBuffer == null)
                {
                    // We're open but not open as a normal text buffer. This can happen if the
                    // project system (say in ASP.NET cases) is telling us to add a file which
                    // actually isn't a normal text file at all.
                    return null;
                }

                if (!canUseTextBuffer(openTextBuffer))
                {
                    return null;
                }
            }

            // If this is being added through a public call to Workspace.AddDocument (say, ApplyChanges) then we might
            // already have a document ID that we should be using here.
            DocumentId id = null;
            _documentIdHints.TryGetValue(filePath, out id);

            document = new StandardTextDocument(
                this,
                hostProject,
                documentKey,
                itemId,
                sourceCodeKind,
                _textUndoHistoryRegistry,
                _fileChangeService,
                openTextBuffer,
                id,
                isGenerated);

            // Add this to our document map
            _documentMap.Add(documentKey, document);

            if (openTextBuffer != null)
            {
                AddCookieOpenDocumentPair(foundCookie, documentKey);
            }

            return document;
        }

        /// <summary>
        /// Tries to return an ITextBuffer representing the document from the document's DocData.
        /// </summary>
        /// <param name="docData">The DocData from the running document table.</param>
        /// <returns>The ITextBuffer. If one could not be found, this returns null.</returns>
        private ITextBuffer TryGetTextBufferFromDocData(object docData)
        {
            var shimTextBuffer = docData as IVsTextBuffer;

            if (shimTextBuffer != null)
            {
                return EditorAdaptersFactoryService.GetDocumentBuffer(shimTextBuffer);
            }
            else
            {
                return null;
            }
        }

        private void NewBufferOpened(uint docCookie, ITextBuffer textBuffer, DocumentKey documentKey, bool isCurrentContext)
        {
            StandardTextDocument document;

            if (_documentMap.TryGetValue(documentKey, out document))
            {
                document.ProcessOpen(textBuffer, isCurrentContext);
                AddCookieOpenDocumentPair(docCookie, documentKey);
            }
        }

        /// <summary>
        /// Notifies the document provider that this document is now registered in a project.
        /// </summary>
        public void NotifyDocumentRegisteredToProject(IVisualStudioHostDocument document)
        {
            // Ignore any other unknown kinds of documents
            var standardDocument = document as StandardTextDocument;
            if (standardDocument == null)
            {
                return;
            }

            // If it's already open, then we have nothing more to do here.
            if (standardDocument.IsOpen)
            {
                return;
            }

            uint docCookie;
            if (RunningDocumentTable.TryGetCookieForInitializedDocument(document.Key.Moniker, out docCookie))
            {
                TryProcessOpenForDocCookie(docCookie);
            }
        }

        private void TryProcessOpenForDocCookie(uint docCookie)
        {
            string moniker = RunningDocumentTable.GetDocumentMoniker(docCookie);

            IVsHierarchy hierarchy;
            uint itemid;
            RunningDocumentTable.GetDocumentHierarchyItem(docCookie, out hierarchy, out itemid);

            var shimTextBuffer = RunningDocumentTable.GetDocumentData(docCookie) as IVsTextBuffer;

            if (shimTextBuffer != null)
            {
                foreach (var project in _projectContainer.GetProjects())
                {
                    var documentKey = new DocumentKey(project, moniker);

                    if (_documentMap.ContainsKey(documentKey))
                    {
                        var textBuffer = EditorAdaptersFactoryService.GetDocumentBuffer(shimTextBuffer);

                        // If we already have an ITextBuffer for this document, then we can open it now.
                        // Otherwise, setup an event handler that will do it when the buffer loads.
                        if (textBuffer != null)
                        {
                            // We might already have this docCookie marked as open an older document. This can happen
                            // if we're in the middle of a rename but this class hasn't gotten the notification yet but
                            // another listener for RDT events got it
                            if (_docCookiesToOpenDocumentKeys.ContainsKey(docCookie))
                            {
                                CloseDocuments(docCookie, monikerToKeep: moniker);
                            }

                            if (hierarchy == project.Hierarchy)
                            {
                                // This is the current context
                                NewBufferOpened(docCookie, textBuffer, documentKey, isCurrentContext: true);
                            }
                            else
                            {
                                // This is a non-current linked context
                                NewBufferOpened(docCookie, textBuffer, documentKey, isCurrentContext: false);
                            }
                        }
                        else
                        {
                            TextBufferDataEventsSink.HookupHandler(this, shimTextBuffer, documentKey);
                        }
                    }
                }
            }
            else
            {
                // This is opening some other designer or property page. If it's tied to our IVsHierarchy, we should
                // let the workspace know
                foreach (var project in _projectContainer.GetProjects())
                {
                    if (hierarchy == project.Hierarchy)
                    {
                        _projectContainer.NotifyNonDocumentOpenedForProject(project);
                    }
                }
            }
        }

        private void OnBeforeDocumentWindowShow(IVsWindowFrame frame, uint docCookie, bool firstShow)
        {
            var ids = GetDocumentIdsFromDocCookie(docCookie);
            foreach (var id in ids)
            {
                OnBeforeDocumentWindowShow(frame, id, firstShow);
            }

            if (ids.Count == 0)
            {
                // deal with non roslyn text file opened in the editor
                OnBeforeNonRoslynDocumentWindowShow(frame, firstShow);
            }
        }

        protected virtual void OnBeforeDocumentWindowShow(IVsWindowFrame frame, DocumentId id, bool firstShow)
        {
        }

        protected virtual void OnBeforeNonRoslynDocumentWindowShow(IVsWindowFrame frame, bool firstShow)
        {
        }

        private IList<DocumentId> GetDocumentIdsFromDocCookie(uint docCookie)
        {
            List<DocumentKey> documentKeys;
            if (!_docCookiesToOpenDocumentKeys.TryGetValue(docCookie, out documentKeys))
            {
                return SpecializedCollections.EmptyList<DocumentId>();
            }

            IList<DocumentId> documentIds = new List<DocumentId>();

            foreach (var documentKey in documentKeys)
            {
                documentIds.Add(_documentMap[documentKey].Id);
            }

            return documentIds;
        }

        /// <summary>
        /// Closes all documents that match the cookie and predicate.
        /// </summary>
        /// <param name="docCookie">The cookie that we should close documents for.</param>
        /// <param name="monikerToKeep">The moniker that we should _not_ close documents for. When a rename is happening,
        /// we might have documents with both the old and new moniker attached to the same docCookie. In those cases
        /// we only want to close anything that doesn't match the new name. Can be null to close everything.</param>
        private void CloseDocuments(uint docCookie, string monikerToKeep)
        {
            List<DocumentKey> documentKeys;
            if (!_docCookiesToOpenDocumentKeys.TryGetValue(docCookie, out documentKeys))
            {
                return;
            }

            // We will remove from documentKeys the things we successfully closed,
            // so clone the list so we can mutate while enumerating
            var documentsToClose = documentKeys.Where(key => !StringComparer.OrdinalIgnoreCase.Equals(key.Moniker, monikerToKeep)).ToList();

            // For a given set of open linked or shared files, we may be closing one of the
            // documents (e.g. excluding a linked file from one of its owning projects or
            // unloading one of the head projects for a shared project) or the entire set of
            // documents (e.g. closing the tab of a shared document). If the entire set of
            // documents is closing, then we should avoid the process of updating the active
            // context document between the closing of individual documents in the set. In the
            // case of closing the tab of a shared document, this avoids updating the shared 
            // item context hierarchy for the entire shared project to head project owning the
            // last documentKey in this list.
            var updateActiveContext = documentsToClose.Count == 1;

            foreach (var documentKey in documentsToClose)
            {
                var document = _documentMap[documentKey];
                document.ProcessClose(updateActiveContext);
                Contract.ThrowIfFalse(documentKeys.Remove(documentKey));
            }

            // If we removed all the keys, then remove the list entirely
            if (documentKeys.Count == 0)
            {
                _docCookiesToOpenDocumentKeys.Remove(docCookie);
            }
        }

        private void OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            if ((grfAttribs & (uint)__VSRDTATTRIB3.RDTA_DocumentInitialized) != 0)
            {
                TryProcessOpenForDocCookie(docCookie);
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_MkDocument) != 0)
            {
                OnDocumentMonikerChanged(docCookie, pszMkDocumentOld, pszMkDocumentNew);
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_Hierarchy) != 0)
            {
                bool itemidChanged = (grfAttribs & (uint)__VSRDTATTRIB.RDTA_ItemID) != 0;
                OnHierarchyChanged(docCookie, pHierOld, itemidOld, pHierNew, itemidNew, itemidChanged);
            }
        }

        protected virtual void OnHierarchyChanged(uint docCookie, IVsHierarchy pHierOld, uint itemidOld, IVsHierarchy pHierNew, uint itemidNew, bool itemidChanged)
        {
            List<DocumentKey> documentKeys;
            if (_docCookiesToOpenDocumentKeys.TryGetValue(docCookie, out documentKeys))
            {
                foreach (var documentKey in documentKeys)
                {
                    var document = _documentMap[documentKey];
                    var currDocHier = document.Project.Hierarchy;

                    if (currDocHier == pHierNew)
                    {
                        documentKey.HostProject.Workspace.OnDocumentContextUpdated(document.Id, document.GetOpenTextContainer());
                    }
                }
            }
        }

        protected virtual void OnDocumentMonikerChanged(uint docCookie, string oldMoniker, string newMoniker)
        {
            // If the moniker change only involves casing differences then the project system will
            // not remove & add the file again with the new name, so we should not clear any state.
            // Leaving the old casing in the DocumentKey is safe because DocumentKey equality 
            // checks ignore the casing of the moniker.
            if (oldMoniker.Equals(newMoniker, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // While we don't natively track source file renames in the Visual Studio workspace, we do
            // need to track them for Code Model's FileCodeModel instances. The lifetime of a FileCodeModel
            // needs to be resilient through the source file add/remove that happens during a file rename.
            RenameFileCodeModelInstances(docCookie, oldMoniker, newMoniker);

            // Since our moniker changed, any old DocumentKeys are invalid. DocumentKeys are immutable,
            // and HostDocuments implicitly have an immutable filename. Therefore, we choose to close 
            // all files associated with this docCookie, so they are no longer associated with an RDT document that
            // no longer matches their filenames. This removes all tracking information and associations
            // between cookies and documents in this class. When the project system comes along and adds the
            // files with the new name, they will ask the DocumentProvider for the documents under the
            // new name. At that point, since we've removed all tracking, these will appear as new files
            // and we'll hand out new HostDocuments that are properly tracking the already open files.

            // In the case of miscellaneous files, we're also watching the RDT. If that saw the RDT event
            // before we did, it's possible we've already updated state to handle the rename. Therefore, we
            // should only handle the close if the moniker we had was out of date.
            CloseDocuments(docCookie, monikerToKeep: newMoniker);
        }

        private void RenameFileCodeModelInstances(uint docCookie, string oldMoniker, string newMoniker)
        {
            List<DocumentKey> documentKeys;
            if (_docCookiesToOpenDocumentKeys.TryGetValue(docCookie, out documentKeys))
            {
                // We will remove from documentKeys the things we successfully closed,
                // so clone the list so we can mutate while enumerating
                var documents = documentKeys
                    .Where(key => StringComparer.OrdinalIgnoreCase.Equals(key.Moniker, oldMoniker))
                    .Select(key => _documentMap[key])
                    .ToList();

                foreach (var document in documents)
                {
                    var workspace = document.Project.Workspace as VisualStudioWorkspace;
                    if (workspace != null)
                    {
                        workspace.RenameFileCodeModelInstance(document.Id, newMoniker);
                    }
                }
            }
        }

        /// <summary>
        /// Called by a VisualStudioDocument when that document is disposed.
        /// </summary>
        /// <param name="document">The document to stop tracking.</param>
        private void StopTrackingDocument(StandardTextDocument document)
        {
            if (document.IsOpen)
            {
                // TODO: This was previously faster, need a bidirectional 1-to-many map

                foreach (var cookie in _docCookiesToOpenDocumentKeys.Keys)
                {
                    var documentKeys = _docCookiesToOpenDocumentKeys[cookie];

                    if (documentKeys.Contains(document.Key))
                    {
                        documentKeys.Remove(document.Key);
                        if (documentKeys.IsEmpty())
                        {
                            _docCookiesToOpenDocumentKeys.Remove(cookie);
                        }

                        break;
                    }
                }
            }

            _documentMap.Remove(document.Key);
        }

        private void AddCookieOpenDocumentPair(uint foundCookie, DocumentKey documentKey)
        {
            List<DocumentKey> documentKeys;
            if (_docCookiesToOpenDocumentKeys.TryGetValue(foundCookie, out documentKeys))
            {
                if (!documentKeys.Contains(documentKey))
                {
                    documentKeys.Add(documentKey);
                }
            }
            else
            {
                _docCookiesToOpenDocumentKeys.Add(foundCookie, new List<DocumentKey> { documentKey });
            }
        }

        private void DocumentLoadCompleted(IVsTextBuffer shimTextBuffer, DocumentKey documentKey)
        {
            // This is called when IVsTextBufferDataEvents.OnLoadComplete() has been triggered for a
            // newly-created buffer.

            uint docCookie;
            if (!RunningDocumentTable.TryGetCookieForInitializedDocument(documentKey.Moniker, out docCookie))
            {
                return;
            }

            var textBuffer = EditorAdaptersFactoryService.GetDocumentBuffer(shimTextBuffer);
            if (textBuffer == null)
            {
                throw new InvalidOperationException("The IVsTextBuffer has been populated but the underlying ITextBuffer does not exist!");
            }

            NewBufferOpened(docCookie, textBuffer, documentKey, IsCurrentContext(docCookie, documentKey));
        }

        private bool IsCurrentContext(uint docCookie, DocumentKey documentKey)
        {
            IVsHierarchy hierarchy;
            uint itemid;
            RunningDocumentTable.GetDocumentHierarchyItem(docCookie, out hierarchy, out itemid);

            // If it belongs to a Shared Code or ASP.NET 5 project, then find the correct host project
            var hostProject = LinkedFileUtilities.GetContextHostProject(hierarchy, _projectContainer);

            return documentKey.HostProject == hostProject;
        }

        public IDisposable ProvideDocumentIdHint(string filePath, DocumentId documentId)
        {
            _documentIdHints[filePath] = documentId;

            return new DocumentIdHint(this, filePath);
        }

        /// <summary>
        /// A small IDisposable object that's returned from ProvideDocumentIdHint.
        /// </summary>
        private class DocumentIdHint : IDisposable
        {
            private readonly DocumentProvider _documentProvider;
            private readonly string _filePath;

            public DocumentIdHint(DocumentProvider documentProvider, string filePath)
            {
                _documentProvider = documentProvider;
                _filePath = filePath;
            }

            public void Dispose()
            {
                _documentProvider._documentIdHints.Remove(_filePath);
            }
        }
    }
}
