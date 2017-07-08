﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
    internal sealed partial class DocumentProvider : ForegroundThreadAffinitizedObject
    {
        #region Immutable readonly fields/properties that can be accessed from foreground or background threads - do not need locking for access.
        private readonly object _gate = new object();
        private readonly uint _runningDocumentTableEventCookie;
        private readonly IVisualStudioHostProjectContainer _projectContainer;
        private readonly IVsFileChangeEx _fileChangeService;
        private readonly IVsTextManager _textManager;
        private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly VisualStudioDocumentTrackingService _documentTrackingServiceOpt;
        #endregion

        #region Mutable fields accessed from foreground or background threads - need locking for access.
        /// <summary>
        /// The core data structure of this entire class.
        /// </summary>
        private readonly Dictionary<DocumentKey, StandardTextDocument> _documentMap = new Dictionary<DocumentKey, StandardTextDocument>();
        private readonly Dictionary<uint, List<DocumentKey>> _docCookiesToOpenDocumentKeys = new Dictionary<uint, List<DocumentKey>>();
        private readonly Dictionary<uint, ITextBuffer> _docCookiesToNonRoslynDocumentBuffers = new Dictionary<uint, ITextBuffer>();
        private readonly Dictionary<string, DocumentId> _documentIdHints = new Dictionary<string, DocumentId>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<DocumentId, TaskAndTokenSource> _pendingDocumentInitializationTasks = new Dictionary<DocumentId, TaskAndTokenSource>();
        #endregion

        /// <summary>
        /// Creates a document provider.
        /// </summary>
        /// <param name="projectContainer">Project container for the documents.</param>
        /// <param name="serviceProvider">Service provider</param>
        /// <param name="documentTrackingService">An optional <see cref="VisualStudioDocumentTrackingService"/> to track active and visible documents.</param>
        public DocumentProvider(
            IVisualStudioHostProjectContainer projectContainer,
            IServiceProvider serviceProvider,
            VisualStudioDocumentTrackingService documentTrackingService)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

            _projectContainer = projectContainer;
            this._documentTrackingServiceOpt = documentTrackingService;
            this._runningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            this._editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            this._contentTypeRegistryService = componentModel.GetService<IContentTypeRegistryService>();
            _textUndoHistoryRegistry = componentModel.GetService<ITextUndoHistoryRegistry>();
            _textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));

            _fileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));

            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            if (shell == null)
            {
                // This can happen only in tests, bail out.
                return;
            }

            var runningDocumentTableForEvents = (IVsRunningDocumentTable)_runningDocumentTable;
            Marshal.ThrowExceptionForHR(runningDocumentTableForEvents.AdviseRunningDocTableEvents(new RunningDocTableEventsSink(this), out _runningDocumentTableEventCookie));
        }

        /// <summary>
        /// Gets the <see cref="IVisualStudioHostDocument"/> for the file at the given filePath.
        /// If we are on the foreground thread and this document is already open in the editor,
        /// then we also attempt to associate the text buffer with it.
        /// Otherwise, if we are on a background thread, then this text buffer association will happen on a scheduled task
        /// whenever <see cref="NotifyDocumentRegisteredToProjectAndStartToRaiseEvents"/> is invoked for the returned document.
        /// </summary>
        public IVisualStudioHostDocument TryGetDocumentForFile(
            IVisualStudioHostProject hostProject,
            string filePath,
            SourceCodeKind sourceCodeKind,
            Func<ITextBuffer, bool> canUseTextBuffer,
            Func<uint, IReadOnlyList<string>> getFolderNames,
            EventHandler updatedOnDiskHandler = null,
            EventHandler<bool> openedHandler = null,
            EventHandler<bool> closingHandler = null)
        {
            var documentKey = new DocumentKey(hostProject, filePath);
            StandardTextDocument document;

            lock (_gate)
            {
                if (_documentMap.TryGetValue(documentKey, out document))
                {
                    return document;
                }
            }

            ITextBuffer openTextBuffer = null;
            uint foundCookie = VSConstants.VSCOOKIE_NIL;

            if (IsForeground())
            {
                // If we are on the foreground thread and this document is already open in the editor we want to associate the text buffer with it.
                // Otherwise, we are on a background thread, and this text buffer association will happen on a scheduled task
                // whenever NotifyDocumentRegisteredToProjectAndStartToRaiseEvents is invoked for the returned document.
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
                if (_runningDocumentTable.TryGetCookieForInitializedDocument(documentKey.Moniker, out foundCookie))
                {
                    object foundDocData = _runningDocumentTable.GetDocumentData(foundCookie);
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
            }

            lock (_gate)
            {
                // If this is being added through a public call to Workspace.AddDocument (say, ApplyChanges) then we might
                // already have a document ID that we should be using here.
                _documentIdHints.TryGetValue(filePath, out var id);

                document = new StandardTextDocument(
                    this,
                    hostProject,
                    documentKey,
                    getFolderNames,
                    sourceCodeKind,
                    _textUndoHistoryRegistry,
                    _fileChangeService,
                    openTextBuffer,
                    id,
                    updatedOnDiskHandler,
                    openedHandler,
                    closingHandler);

                // Add this to our document map
                _documentMap.Add(documentKey, document);

                if (openTextBuffer != null)
                {
                    AddCookieOpenDocumentPair_NoLock(foundCookie, documentKey);
                }
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
            AssertIsForeground();

            var shimTextBuffer = docData as IVsTextBuffer;

            if (shimTextBuffer != null)
            {
                return _editorAdaptersFactoryService.GetDocumentBuffer(shimTextBuffer);
            }
            else
            {
                return null;
            }
        }

        private void NewBufferOpened(uint docCookie, ITextBuffer textBuffer, DocumentKey documentKey, bool isCurrentContext)
        {
            AssertIsForeground();

            lock (_gate)
            {
                NewBufferOpened_NoLock(docCookie, textBuffer, documentKey, isCurrentContext);
            }
        }

        private void NewBufferOpened_NoLock(uint docCookie, ITextBuffer textBuffer, DocumentKey documentKey, bool isCurrentContext)
        {
            if (_documentMap.TryGetValue(documentKey, out var document))
            {
                document.ProcessOpen(textBuffer, isCurrentContext);
                AddCookieOpenDocumentPair_NoLock(docCookie, documentKey);
            }
        }

        /// <summary>
        /// Notifies the document provider that this document is now registered in a project.
        /// If we are on a foregroud thread, then this is done right away.
        /// Otherwise, we schedule a task on foreground task scheduler.
        /// </summary>
        public void NotifyDocumentRegisteredToProjectAndStartToRaiseEvents(IVisualStudioHostDocument document)
        {
            if (IsForeground())
            {
                NotifyDocumentRegisteredToProjectAndStartToRaiseEvents_Core(document, cancellationToken: CancellationToken.None);
            }
            else
            {
                var cts = new CancellationTokenSource();
                var task = InvokeBelowInputPriority(() => NotifyDocumentRegisteredToProjectAndStartToRaiseEvents_Core(document, cts.Token), cts.Token);
                AddPendingDocumentInitializationTask(document, task, cts);
            }
        }

        private void AddPendingDocumentInitializationTask(IVisualStudioHostDocument document, Task task, CancellationTokenSource cts)
        {
            var taskAndTokenSource = new TaskAndTokenSource() { Task = task, CancellationTokenSource = cts };
            lock (_gate)
            {
                // Add taskAndTokenSource to the pending document initialization tasks.
                // Check for cancellation before adding as the task might already have been completed/cancelled/faulted before we reached here.
                if (!cts.IsCancellationRequested && !task.IsCompleted)
                {
                    _pendingDocumentInitializationTasks.Add(document.Id, taskAndTokenSource);
                }
            }
        }

        private void CancelPendingDocumentInitializationTasks_NoLock(IEnumerable<DocumentKey> documentKeys)
        {
            foreach (var documentKey in documentKeys)
            {
                if (_documentMap.TryGetValue(documentKey, out var document))
                {
                    CancelPendingDocumentInitializationTask_NoLock(document);
                }
            }
        }

        private void CancelPendingDocumentInitializationTask(IVisualStudioHostDocument document)
        {
            lock (_gate)
            {
                CancelPendingDocumentInitializationTask_NoLock(document);
            }
        }

        private void CancelPendingDocumentInitializationTask_NoLock(IVisualStudioHostDocument document)
        {
            // Remove pending initialization task for the document, if any, and dispose the cancellation token source.
            if (_pendingDocumentInitializationTasks.TryGetValue(document.Id, out var taskAndTokenSource))
            {
                taskAndTokenSource.CancellationTokenSource.Cancel();
                taskAndTokenSource.CancellationTokenSource.Dispose();
                _pendingDocumentInitializationTasks.Remove(document.Id);
            }
        }

        private void NotifyDocumentRegisteredToProjectAndStartToRaiseEvents_Core(IVisualStudioHostDocument document, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            cancellationToken.ThrowIfCancellationRequested();

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

            if (_runningDocumentTable.TryGetCookieForInitializedDocument(document.Key.Moniker, out var docCookie))
            {
                TryProcessOpenForDocCookie(docCookie, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            CancelPendingDocumentInitializationTask(document);
        }

        private void TryProcessOpenForDocCookie(uint docCookie, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            lock (_gate)
            {
                cancellationToken.ThrowIfCancellationRequested();

                TryProcessOpenForDocCookie_NoLock(docCookie);
            }
        }

        private void TryProcessOpenForDocCookie_NoLock(uint docCookie)
        {
            string moniker = _runningDocumentTable.GetDocumentMoniker(docCookie);
            _runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out var itemid);

            var shimTextBuffer = _runningDocumentTable.GetDocumentData(docCookie) as IVsTextBuffer;

            if (shimTextBuffer != null)
            {
                var hasAssociatedRoslynDocument = false;
                foreach (var project in _projectContainer.GetProjects())
                {
                    var documentKey = new DocumentKey(project, moniker);

                    if (_documentMap.ContainsKey(documentKey))
                    {
                        hasAssociatedRoslynDocument = true;
                        var textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(shimTextBuffer);

                        // If we already have an ITextBuffer for this document, then we can open it now.
                        // Otherwise, setup an event handler that will do it when the buffer loads.
                        if (textBuffer != null)
                        {
                            // We might already have this docCookie marked as open an older document. This can happen
                            // if we're in the middle of a rename but this class hasn't gotten the notification yet but
                            // another listener for RDT events got it
                            if (_docCookiesToOpenDocumentKeys.ContainsKey(docCookie))
                            {
                                CloseDocuments_NoLock(docCookie, monikerToKeep: moniker);
                            }

                            if (hierarchy == project.Hierarchy)
                            {
                                // This is the current context
                                NewBufferOpened_NoLock(docCookie, textBuffer, documentKey, isCurrentContext: true);
                            }
                            else
                            {
                                // This is a non-current linked context
                                NewBufferOpened_NoLock(docCookie, textBuffer, documentKey, isCurrentContext: false);
                            }
                        }
                        else
                        {
                            TextBufferDataEventsSink.HookupHandler(shimTextBuffer, onDocumentLoadCompleted: () => OnDocumentLoadCompleted(shimTextBuffer, documentKey, moniker));
                        }
                    }
                }

                if (!hasAssociatedRoslynDocument && this._documentTrackingServiceOpt != null && !_docCookiesToNonRoslynDocumentBuffers.ContainsKey(docCookie))
                {
                    // Non-Roslyn document opened.
                    var textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(shimTextBuffer);
                    if (textBuffer != null)
                    {
                        OnNonRoslynBufferOpened_NoLock(textBuffer, docCookie);
                    }
                    else
                    {
                        TextBufferDataEventsSink.HookupHandler(shimTextBuffer, onDocumentLoadCompleted: () => OnDocumentLoadCompleted(shimTextBuffer, documentKeyOpt: null, moniker: moniker));
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

        private void OnNonRoslynBufferOpened_NoLock(ITextBuffer textBuffer, uint docCookie)
        {
            AssertIsForeground();
            Contract.ThrowIfNull(textBuffer);
            Contract.ThrowIfNull(_documentTrackingServiceOpt);

            this._documentTrackingServiceOpt.OnNonRoslynBufferOpened(textBuffer);
            _docCookiesToNonRoslynDocumentBuffers.Add(docCookie, textBuffer);
        }

        private void OnBeforeDocumentWindowShow(IVsWindowFrame frame, uint docCookie, bool firstShow)
        {
            AssertIsForeground();

            var ids = GetDocumentIdsFromDocCookie(docCookie);
            foreach (var id in ids)
            {
                this._documentTrackingServiceOpt?.DocumentFrameShowing(frame, id, firstShow);
            }
        }

        private IList<DocumentId> GetDocumentIdsFromDocCookie(uint docCookie)
        {
            lock (_gate)
            {
                return GetDocumentIdsFromDocCookie_NoLock(docCookie);
            }
        }

        private IList<DocumentId> GetDocumentIdsFromDocCookie_NoLock(uint docCookie)
        {
            AssertIsForeground();
            if (!_docCookiesToOpenDocumentKeys.TryGetValue(docCookie, out var documentKeys))
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
            AssertIsForeground();

            lock (_gate)
            {
                CloseDocuments_NoLock(docCookie, monikerToKeep);
            }
        }

        private void CloseDocuments_NoLock(uint docCookie, string monikerToKeep)
        {
            if (!_docCookiesToOpenDocumentKeys.TryGetValue(docCookie, out var documentKeys))
            {
                // Handle non-Roslyn document close.
                if (this._documentTrackingServiceOpt != null && _docCookiesToNonRoslynDocumentBuffers.TryGetValue(docCookie, out ITextBuffer textBuffer))
                {
                    var moniker = _runningDocumentTable.GetDocumentMoniker(docCookie);
                    if (!StringComparer.OrdinalIgnoreCase.Equals(moniker, monikerToKeep))
                    {
                        this._documentTrackingServiceOpt.OnNonRoslynBufferClosed(textBuffer);
                        _docCookiesToNonRoslynDocumentBuffers.Remove(docCookie);
                    }
                }

                return;
            }

            // We will remove from documentKeys the things we successfully closed,
            // so clone the list so we can mutate while enumerating
            var documentsToClose = documentKeys.Where(key => !StringComparer.OrdinalIgnoreCase.Equals(key.Moniker, monikerToKeep)).ToList();

            // Cancel any pending scheduled tasks to register document opened for the documents we are closing.
            CancelPendingDocumentInitializationTasks_NoLock(documentsToClose);

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
                TryProcessOpenForDocCookie(docCookie, CancellationToken.None);
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

        private void OnHierarchyChanged(uint docCookie, IVsHierarchy pHierOld, uint itemidOld, IVsHierarchy pHierNew, uint itemidNew, bool itemidChanged)
        {
            AssertIsForeground();

            lock (_gate)
            {
                if (_docCookiesToOpenDocumentKeys.TryGetValue(docCookie, out var documentKeys))
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
        }

        private void OnDocumentMonikerChanged(uint docCookie, string oldMoniker, string newMoniker)
        {
            AssertIsForeground();

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
            // between cookies and documents in this class.

            // In the case of miscellaneous files, we're also watching the RDT. If that saw the RDT event
            // before we did, it's possible we've already updated state to handle the rename. Therefore, we
            // should only handle the close if the moniker we had was out of date.
            CloseDocuments(docCookie, monikerToKeep: newMoniker);

            // We might also have new documents that now need to be opened, so handle them too. If the document
            // isn't initialized we will wait until it's actually initialized to trigger the open; we see
            // from the OnAfterAttributeChangeEx notification.
            if (_runningDocumentTable.IsDocumentInitialized(docCookie))
            {
                TryProcessOpenForDocCookie(docCookie, CancellationToken.None);
            }
        }

        private void RenameFileCodeModelInstances(uint docCookie, string oldMoniker, string newMoniker)
        {
            AssertIsForeground();

            List<StandardTextDocument> documents;
            lock (_gate)
            {
                if (!_docCookiesToOpenDocumentKeys.TryGetValue(docCookie, out var documentKeys))
                {
                    return;
                }

                // We will remove from documentKeys the things we successfully closed,
                // so clone the list so we can mutate while enumerating
                documents = documentKeys
                    .Where(key => StringComparer.OrdinalIgnoreCase.Equals(key.Moniker, oldMoniker))
                    .Select(key => _documentMap[key])
                    .ToList();
            }

            foreach (var document in documents)
            {
                var workspace = document.Project.Workspace as VisualStudioWorkspace;
                if (workspace != null)
                {
                    workspace.RenameFileCodeModelInstance(document.Id, newMoniker);
                }
            }
        }

        /// <summary>
        /// Called by a VisualStudioDocument when that document is disposed.
        /// </summary>
        /// <param name="document">The document to stop tracking.</param>
        private void StopTrackingDocument(StandardTextDocument document)
        {
            CancelPendingDocumentInitializationTask(document);

            if (IsForeground())
            {
                StopTrackingDocument_Core(document);
            }
            else
            {
                InvokeBelowInputPriority(() => StopTrackingDocument_Core(document), CancellationToken.None);
            }
        }

        private void StopTrackingDocument_Core(StandardTextDocument document)
        {
            AssertIsForeground();

            lock (_gate)
            {
                StopTrackingDocument_Core_NoLock(document);
            }
        }

        private void StopTrackingDocument_Core_NoLock(StandardTextDocument document)
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

        private void AddCookieOpenDocumentPair_NoLock(uint foundCookie, DocumentKey documentKey)
        {
            if (_docCookiesToOpenDocumentKeys.TryGetValue(foundCookie, out var documentKeys))
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

        private void OnDocumentLoadCompleted(IVsTextBuffer shimTextBuffer, DocumentKey documentKeyOpt, string moniker)
        {
            AssertIsForeground();
            // This is called when IVsTextBufferDataEvents.OnLoadComplete() has been triggered for a
            // newly-created buffer.
            if (!_runningDocumentTable.TryGetCookieForInitializedDocument(moniker, out var docCookie))
            {
                return;
            }

            var textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(shimTextBuffer);
            if (textBuffer == null)
            {
                throw new InvalidOperationException("The IVsTextBuffer has been populated but the underlying ITextBuffer does not exist!");
            }

            if (documentKeyOpt == null)
            {
                // Non-Roslyn document.
                OnNonRoslynBufferOpened_NoLock(textBuffer, docCookie);
            }
            else
            {
                NewBufferOpened(docCookie, textBuffer, documentKeyOpt, IsCurrentContext(documentKeyOpt));
            }
        }

        private bool IsCurrentContext(DocumentKey documentKey)
        {
            AssertIsForeground();
            var document = documentKey.HostProject.GetCurrentDocumentFromPath(documentKey.Moniker);
            return document != null && LinkedFileUtilities.IsCurrentContextHierarchy(document, _runningDocumentTable);
        }

        public IDisposable ProvideDocumentIdHint(string filePath, DocumentId documentId)
        {
            lock (_gate)
            {
                _documentIdHints[filePath] = documentId;
                return new DocumentIdHint(this, filePath);
            }
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
                lock (_documentProvider._gate)
                {
                    _documentProvider._documentIdHints.Remove(_filePath);
                }
            }
        }

        private struct TaskAndTokenSource
        {
            public Task Task { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }
    }
}
