// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // The parts of a workspace that deal with open documents
    public abstract partial class Workspace
    {
        // open documents
        private readonly Dictionary<ProjectId, ISet<DocumentId>> _projectToOpenDocumentsMap = new Dictionary<ProjectId, ISet<DocumentId>>();

        // text buffer maps
        /// <summary>
        /// Tracks the document ID in the current context for a source text container for an opened text buffer.
        /// </summary>
        /// <remarks>For each entry in this map, there must be a corresponding entry in <see cref="_bufferToAssociatedDocumentsMap"/> where the document ID in current context is one of associated document IDs.</remarks>
        private readonly Dictionary<SourceTextContainer, DocumentId> _bufferToDocumentInCurrentContextMap = new Dictionary<SourceTextContainer, DocumentId>();

        /// <summary>
        /// Tracks all the associated document IDs for a source text container for an opened text buffer.
        /// </summary>
        private readonly Dictionary<SourceTextContainer, OneOrMany<DocumentId>> _bufferToAssociatedDocumentsMap = new Dictionary<SourceTextContainer, OneOrMany<DocumentId>>();

        private readonly Dictionary<DocumentId, TextTracker> _textTrackers = new Dictionary<DocumentId, TextTracker>();

        /// <summary>
        /// True if this workspace supports manually opening and closing documents.
        /// </summary>
        public virtual bool CanOpenDocuments => false;

        /// <summary>
        /// True if this workspace supports manually changing the active context document of a text buffer.
        /// </summary>
        internal virtual bool CanChangeActiveContextDocument => false;

        private static void RemoveIfEmpty<TKey, TValue>(IDictionary<TKey, ISet<TValue>> dictionary, TKey key)
        {
            if (dictionary.TryGetValue(key, out var values))
            {
                if (values.Count == 0)
                {
                    dictionary.Remove(key);
                }
            }
        }

        private void ClearOpenDocuments()
        {
            List<DocumentId> docIds;
            using (_stateLock.DisposableWait())
            {
                docIds = _projectToOpenDocumentsMap.Values.SelectMany(x => x).ToList();
            }

            foreach (var docId in docIds)
            {
                this.ClearOpenDocument(docId, isSolutionClosing: true);
            }
        }

        private void ClearOpenDocuments(ProjectId projectId)
        {
            ISet<DocumentId> openDocs;
            using (_stateLock.DisposableWait())
            {
                _projectToOpenDocumentsMap.TryGetValue(projectId, out openDocs);
            }

            if (openDocs != null)
            {
                // ClearOpenDocument will remove the document from the original set.
                var copyOfOpenDocs = openDocs.ToList();
                foreach (var docId in copyOfOpenDocs)
                {
                    this.ClearOpenDocument(docId);
                }
            }
        }

        protected void ClearOpenDocument(DocumentId documentId, bool isSolutionClosing = false)
        {
            DocumentId currentContextDocumentId;
            using (_stateLock.DisposableWait())
            {
                currentContextDocumentId = this.ClearOpenDocument_NoLock(documentId);
            }

            // If the solution is closing, then setting the updated document context can fail
            // if the host documents are already closed.
            if (!isSolutionClosing && this.CanChangeActiveContextDocument && currentContextDocumentId != null)
            {
                SetDocumentContext(currentContextDocumentId);
            }
        }

        /// <returns>The DocumentId of the current context document for the buffer that was 
        /// previously attached to the given documentId, if any</returns>
        private DocumentId ClearOpenDocument_NoLock(DocumentId documentId)
        {
            _stateLock.AssertHasLock();
            if (_projectToOpenDocumentsMap.TryGetValue(documentId.ProjectId, out var openDocIds) && openDocIds != null)
            {
                openDocIds.Remove(documentId);
            }

            RemoveIfEmpty(_projectToOpenDocumentsMap, documentId.ProjectId);
            // Stop tracking the buffer or update the documentId associated with the buffer.
            if (_textTrackers.TryGetValue(documentId, out var tracker))
            {
                tracker.Disconnect();
                _textTrackers.Remove(documentId);

                var currentContextDocumentId = UpdateCurrentContextMapping_NoLock(tracker.TextContainer, documentId);
                if (currentContextDocumentId != null)
                {
                    return currentContextDocumentId;
                }
                else
                {
                    // No documentIds are attached to this buffer, so stop tracking it.
                    this.UnregisterText(tracker.TextContainer);
                }
            }

            return null;
        }

        /// <summary>
        /// Open the specified document in the host environment.
        /// </summary>
        public virtual void OpenDocument(DocumentId documentId, bool activate = true)
        {
            this.CheckCanOpenDocuments();
        }

        /// <summary>
        /// Close the specified document in the host environment.
        /// </summary>
        public virtual void CloseDocument(DocumentId documentId)
        {
            this.CheckCanOpenDocuments();
        }

        /// <summary>
        /// Open the specified additional document in the host environment.
        /// </summary>
        public virtual void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        {
            this.CheckCanOpenDocuments();
        }

        /// <summary>
        /// Close the specified additional document in the host environment.
        /// </summary>
        public virtual void CloseAdditionalDocument(DocumentId documentId)
        {
            this.CheckCanOpenDocuments();
        }

        protected void CheckCanOpenDocuments()
        {
            if (!this.CanOpenDocuments)
            {
                throw new NotSupportedException(WorkspacesResources.This_workspace_does_not_support_opening_and_closing_documents);
            }
        }

        protected void CheckProjectDoesNotContainOpenDocuments(ProjectId projectId)
        {
            if (ProjectHasOpenDocuments(projectId))
            {
                throw new ArgumentException(string.Format(WorkspacesResources._0_still_contains_open_documents, this.GetProjectName(projectId)));
            }
        }

        private bool ProjectHasOpenDocuments(ProjectId projectId)
        {
            using (_stateLock.DisposableWait())
            {
                return _projectToOpenDocumentsMap.ContainsKey(projectId);
            }
        }

        /// <summary>
        /// Determines if the document is currently open in the host environment.
        /// </summary>
        public virtual bool IsDocumentOpen(DocumentId documentId)
        {
            using (_stateLock.DisposableWait())
            {
                var openDocuments = this.GetProjectOpenDocuments_NoLock(documentId.ProjectId);
                return openDocuments != null && openDocuments.Contains(documentId);
            }
        }

        /// <summary>
        /// Gets a list of the currently opened documents.
        /// </summary>
        public virtual IEnumerable<DocumentId> GetOpenDocumentIds(ProjectId projectId = null)
        {
            using (_stateLock.DisposableWait())
            {
                if (_projectToOpenDocumentsMap.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerable<DocumentId>();
                }

                if (projectId != null)
                {
                    if (_projectToOpenDocumentsMap.TryGetValue(projectId, out var documentIds))
                    {
                        return documentIds;
                    }

                    return SpecializedCollections.EmptyEnumerable<DocumentId>();
                }

                return _projectToOpenDocumentsMap.SelectMany(kvp => kvp.Value).ToImmutableArray();
            }
        }

        /// <summary>
        /// Gets the ids for documents associated with a text container.
        /// Documents are normally associated with a text container when the documents are opened.
        /// </summary>
        public virtual IEnumerable<DocumentId> GetRelatedDocumentIds(SourceTextContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            using (_stateLock.DisposableWait())
            {
                return GetRelatedDocumentIds_NoLock(container);
            }
        }

        private ImmutableArray<DocumentId> GetRelatedDocumentIds_NoLock(SourceTextContainer container)
        {
            if (!_bufferToDocumentInCurrentContextMap.TryGetValue(container, out var documentId))
            {
                // it is not an opened file
                return ImmutableArray<DocumentId>.Empty;
            }

            return this.CurrentSolution.GetRelatedDocumentIds(documentId);
        }

        /// <summary>
        /// Gets the id for the document associated with the given text container in its current context.
        /// Documents are normally associated with a text container when the documents are opened.
        /// </summary>
        public virtual DocumentId GetDocumentIdInCurrentContext(SourceTextContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            using (_stateLock.DisposableWait())
            {
                return GetDocumentIdInCurrentContext_NoLock(container);
            }
        }

        /// <summary>
        /// Finds the <see cref="DocumentId"/> related to the given <see cref="DocumentId"/> that
        /// is in the current context. If the <see cref="DocumentId"/> is currently closed, then 
        /// it is returned directly. If it is open, then this returns the same result that 
        /// <see cref="GetDocumentIdInCurrentContext(SourceTextContainer)"/> would return for the
        /// <see cref="SourceTextContainer"/>. Hosts can override this method to provide more 
        /// customized behaviors (e.g. special handling of documents in Shared Projects).
        /// </summary>
        internal virtual DocumentId GetDocumentIdInCurrentContext(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            using (_stateLock.DisposableWait())
            {
                var container = GetOpenDocumentSourceTextContainer_NoLock(documentId);
                return container != null ? GetDocumentIdInCurrentContext_NoLock(container) : documentId;
            }
        }

        private SourceTextContainer GetOpenDocumentSourceTextContainer_NoLock(DocumentId documentId) =>
            _bufferToAssociatedDocumentsMap.Where(kvp => kvp.Value.Contains(documentId)).Select(kvp => kvp.Key).FirstOrDefault();

        private DocumentId GetDocumentIdInCurrentContext_NoLock(SourceTextContainer container)
        {
            bool foundValue = _bufferToDocumentInCurrentContextMap.TryGetValue(container, out var docId);

            if (foundValue)
            {
                return docId;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal virtual void SetDocumentContext(DocumentId documentId)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        protected void OnDocumentContextUpdated(DocumentId documentId)
        {
            // TODO: remove linear search

            SourceTextContainer container = null;
            using (_stateLock.DisposableWait())
            {
                container = GetOpenDocumentSourceTextContainer_NoLock(documentId);
            }

            if (container != null)
            {
                OnDocumentContextUpdated(documentId, container);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal void OnDocumentContextUpdated(DocumentId documentId, SourceTextContainer container)
        {
            if (_isProjectUnloading.Value)
            {
                // When the project is unloading, the serialization lock is already taken, so there
                // is no need for us to take the lock since everything is already serialized.
                OnDocumentContextUpdated_NoSerializationLock(documentId, container);
            }
            else
            {
                using (_serializationLock.DisposableWait())
                {
                    OnDocumentContextUpdated_NoSerializationLock(documentId, container);
                }
            }
        }

        internal void OnDocumentContextUpdated_NoSerializationLock(DocumentId documentId, SourceTextContainer container)
        {
            DocumentId oldActiveContextDocumentId;

            using (_stateLock.DisposableWait())
            {
                oldActiveContextDocumentId = _bufferToDocumentInCurrentContextMap[container];
                if (documentId == oldActiveContextDocumentId)
                {
                    return;
                }

                UpdateCurrentContextMapping_NoLock(container, documentId, isCurrentContext: true);
            }

            // fire and forget
            this.RaiseDocumentActiveContextChangedEventAsync(container, oldActiveContextDocumentId: oldActiveContextDocumentId, newActiveContextDocumentId: documentId);
        }

        protected void CheckDocumentIsClosed(DocumentId documentId)
        {
            if (this.IsDocumentOpen(documentId))
            {
                throw new ArgumentException(
                    string.Format(WorkspacesResources._0_is_still_open,
                    this.GetDocumentName(documentId)));
            }
        }

        protected void CheckDocumentIsOpen(DocumentId documentId)
        {
            if (!this.IsDocumentOpen(documentId))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_not_open,
                    this.GetDocumentName(documentId)));
            }
        }

        private ISet<DocumentId> GetProjectOpenDocuments_NoLock(ProjectId project)
        {
            _stateLock.AssertHasLock();
            _projectToOpenDocumentsMap.TryGetValue(project, out var openDocs);
            return openDocs;
        }

        protected internal void OnDocumentOpened(
            DocumentId documentId, SourceTextContainer textContainer,
            bool isCurrentContext = true)
        {
            CheckDocumentIsInCurrentSolution(documentId);
            CheckDocumentIsClosed(documentId);

            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                var oldDocument = oldSolution.GetDocument(documentId);
                var oldDocumentState = oldDocument.State;

                AddToOpenDocumentMap(documentId);

                var newText = textContainer.CurrentText;
                Solution currentSolution;
                if (oldDocument.TryGetText(out var oldText) &&
                    oldDocument.TryGetTextVersion(out var version))
                {
                    // Optimize the case where we've already got the previous text and version.
                    var newTextAndVersion = GetProperTextAndVersion(oldText, newText, version, oldDocumentState.FilePath);

                    // keep open document text alive by using PreserveIdentity
                    currentSolution = oldSolution.WithDocumentText(documentId, newTextAndVersion, PreservationMode.PreserveIdentity);
                }
                else
                {
                    // We don't have the old text or version.  And we don't want to retrieve them
                    // just yet (as that would cause blocking in this synchronous method).  So just
                    // make a simple loader to do that for us later when requested.
                    //
                    // keep open document text alive by using PreserveIdentity
                    //
                    // Note: we pass along the newText here so that clients can easily get the text
                    // of an opened document just by calling TryGetText without any blocking.
                    currentSolution = oldSolution.WithDocumentTextLoader(documentId,
                        new ReuseVersionLoader((DocumentState)oldDocument.State, newText), newText, PreservationMode.PreserveIdentity);
                }

                var newSolution = this.SetCurrentSolution(currentSolution);
                SignupForTextChanges(documentId, textContainer, isCurrentContext, (w, id, text, mode) => w.OnDocumentTextChanged(id, text, mode));

                var newDoc = newSolution.GetDocument(documentId);
                this.OnDocumentTextChanged(newDoc);

                // Fire and forget that the workspace is changing.
                RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId: documentId);
                this.RaiseDocumentOpenedEventAsync(newDoc);
            }

            this.RegisterText(textContainer);
        }

        private class ReuseVersionLoader : TextLoader
        {
            // Capture DocumentState instead of Document so that we don't hold onto the old solution.
            private readonly DocumentState _oldDocumentState;
            private readonly SourceText _newText;

            public ReuseVersionLoader(DocumentState oldDocumentState, SourceText newText)
            {
                _oldDocumentState = oldDocumentState;
                _newText = newText;
            }

            public override async Task<TextAndVersion> LoadTextAndVersionAsync(
                Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                var oldText = await _oldDocumentState.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var version = await _oldDocumentState.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

                return GetProperTextAndVersion(oldText, _newText, version, _oldDocumentState.FilePath);
            }

            internal override TextAndVersion LoadTextAndVersionSynchronously(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                var oldText = _oldDocumentState.GetTextSynchronously(cancellationToken);
                var version = _oldDocumentState.GetTextVersionSynchronously(cancellationToken);

                return GetProperTextAndVersion(oldText, _newText, version, _oldDocumentState.FilePath);
            }
        }

        private static TextAndVersion GetProperTextAndVersion(SourceText oldText, SourceText newText, VersionStamp version, string filePath)
        {
            // if the supplied text is the same as the previous text, then also use same version
            // otherwise use new version
            return oldText.ContentEquals(newText)
                ? TextAndVersion.Create(newText, version, filePath)
                : TextAndVersion.Create(newText, version.GetNewerVersion(), filePath);
        }

        private void SignupForTextChanges(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext, Action<Workspace, DocumentId, SourceText, PreservationMode> onChangedHandler)
        {
            var tracker = new TextTracker(this, documentId, textContainer, onChangedHandler);
            _textTrackers.Add(documentId, tracker);
            this.UpdateCurrentContextMapping_NoLock(textContainer, documentId, isCurrentContext);
            tracker.Connect();
        }

        private void AddToOpenDocumentMap(DocumentId documentId)
        {
            using (_stateLock.DisposableWait())
            {
                var openDocuments = GetProjectOpenDocuments_NoLock(documentId.ProjectId);
                if (openDocuments != null)
                {
                    openDocuments.Add(documentId);
                }
                else
                {
                    _projectToOpenDocumentsMap.Add(documentId.ProjectId, new HashSet<DocumentId> { documentId });
                }
            }
        }

        protected internal void OnAdditionalDocumentOpened(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext = true)
        {
            CheckAdditionalDocumentIsInCurrentSolution(documentId);
            CheckDocumentIsClosed(documentId);

            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                var oldDocument = oldSolution.GetAdditionalDocument(documentId);
                var oldText = oldDocument.GetTextSynchronously(CancellationToken.None);

                // keep open document text alive by using PreserveIdentity
                var newText = textContainer.CurrentText;
                Solution currentSolution;

                if (oldText == newText || oldText.ContentEquals(newText))
                {
                    // if the supplied text is the same as the previous text, then also use same version
                    var version = oldDocument.GetTextVersionSynchronously(CancellationToken.None);
                    var newTextAndVersion = TextAndVersion.Create(newText, version, oldDocument.FilePath);
                    currentSolution = oldSolution.WithAdditionalDocumentText(documentId, newTextAndVersion, PreservationMode.PreserveIdentity);
                }
                else
                {
                    currentSolution = oldSolution.WithAdditionalDocumentText(documentId, newText, PreservationMode.PreserveIdentity);
                }

                var newSolution = this.SetCurrentSolution(currentSolution);

                SignupForTextChanges(documentId, textContainer, isCurrentContext, (w, id, text, mode) => w.OnAdditionalDocumentTextChanged(id, text, mode));

                // Fire and forget.
                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AdditionalDocumentChanged, oldSolution, newSolution, documentId: documentId);
            }

            this.RegisterText(textContainer);
        }

        protected internal void OnDocumentClosed(DocumentId documentId, TextLoader reloader, bool updateActiveContext = false)
        {
            this.CheckDocumentIsInCurrentSolution(documentId);
            this.CheckDocumentIsOpen(documentId);

            // When one file from a set of linked or shared documents is closed, we first update
            // our data structures and then call SetDocumentContext to tell the project system 
            // what the new active context is. This can cause reentrancy, so we call 
            // SetDocumentContext after releasing the serializationLock.
            DocumentId currentContextDocumentId;

            using (_serializationLock.DisposableWait())
            {
                // forget any open document info
                currentContextDocumentId = ForgetAnyOpenDocumentInfo(documentId);

                var oldSolution = this.CurrentSolution;
                var oldDocument = oldSolution.GetDocument(documentId);

                this.OnDocumentClosing(documentId);

                var newSolution = oldSolution.WithDocumentTextLoader(documentId, reloader, PreservationMode.PreserveValue);
                newSolution = this.SetCurrentSolution(newSolution);

                var newDoc = newSolution.GetDocument(documentId);
                this.OnDocumentTextChanged(newDoc);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId: documentId); // don't wait for this
                this.RaiseDocumentClosedEventAsync(newDoc); // don't wait for this
            }

            if (updateActiveContext && currentContextDocumentId != null && this.CanChangeActiveContextDocument)
            {
                // Closing this document did not result in the buffer closing, so some 
                // document is now the current context of that buffer. Fire the appropriate
                // events to set that document as the current context of that buffer.

                SetDocumentContext(currentContextDocumentId);
            }
        }

        private DocumentId ForgetAnyOpenDocumentInfo(DocumentId documentId)
        {
            using (_stateLock.DisposableWait())
            {
                return this.ClearOpenDocument_NoLock(documentId);
            }
        }

        protected internal void OnAdditionalDocumentClosed(DocumentId documentId, TextLoader reloader)
        {
            this.CheckAdditionalDocumentIsInCurrentSolution(documentId);

            using (_serializationLock.DisposableWait())
            {
                // forget any open document info
                ForgetAnyOpenDocumentInfo(documentId);

                var oldSolution = this.CurrentSolution;
                var oldDocument = oldSolution.GetAdditionalDocument(documentId);

                var newSolution = oldSolution.WithAdditionalDocumentTextLoader(documentId, reloader, PreservationMode.PreserveValue);
                newSolution = this.SetCurrentSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AdditionalDocumentChanged, oldSolution, newSolution, documentId: documentId); // don't wait for this
            }
        }

        private void UpdateCurrentContextMapping_NoLock(SourceTextContainer textContainer, DocumentId id, bool isCurrentContext)
        {
            if (_bufferToAssociatedDocumentsMap.TryGetValue(textContainer, out OneOrMany<DocumentId> docIds))
            {
                Contract.ThrowIfFalse(_bufferToDocumentInCurrentContextMap.ContainsKey(textContainer));
                if (!docIds.Contains(id))
                {
                    docIds = docIds.Add(id);
                }
            }
            else
            {
                Contract.ThrowIfFalse(!_bufferToDocumentInCurrentContextMap.ContainsKey(textContainer));
                docIds = new OneOrMany<DocumentId>(id);
            }

            if (isCurrentContext || !_bufferToDocumentInCurrentContextMap.ContainsKey(textContainer))
            {
                _bufferToDocumentInCurrentContextMap[textContainer] = id;
            }

            _bufferToAssociatedDocumentsMap[textContainer] = docIds;
        }

        /// <returns>The DocumentId of the current context document attached to the textContainer, if any.</returns>
        private DocumentId UpdateCurrentContextMapping_NoLock(SourceTextContainer textContainer, DocumentId closedDocumentId)
        {
            // Check if we are tracking this textContainer.
            if (!_bufferToAssociatedDocumentsMap.TryGetValue(textContainer, out OneOrMany<DocumentId> docIds))
            {
                Contract.ThrowIfFalse(!_bufferToDocumentInCurrentContextMap.ContainsKey(textContainer));
                return null;
            }

            Contract.ThrowIfFalse(_bufferToDocumentInCurrentContextMap.ContainsKey(textContainer));

            // Remove closedDocumentId
            docIds = docIds.RemoveAll(closedDocumentId);

            // Remove the entry if there are no more documents attached to given textContainer.
            if (docIds.Equals(default(OneOrMany<DocumentId>)))
            {
                _bufferToAssociatedDocumentsMap.Remove(textContainer);
                _bufferToDocumentInCurrentContextMap.Remove(textContainer);
                return null;
            }

            // Update the new list of documents attached to the given textContainer and the current context document, and return the latter.
            _bufferToAssociatedDocumentsMap[textContainer] = docIds;
            _bufferToDocumentInCurrentContextMap[textContainer] = docIds[0];
            return docIds[0];
        }

        private SourceText GetOpenDocumentText(Solution solution, DocumentId documentId)
        {
            CheckDocumentIsOpen(documentId);
            var doc = solution.GetDocument(documentId);
            // text should always be preserved, so TryGetText will succeed.
            doc.TryGetText(out var text);
            return text;
        }

        /// <summary>
        ///  This method is called during OnSolutionReload.  Override this method if you want to manipulate
        ///  the reloaded solution.
        /// </summary>
        protected virtual Solution AdjustReloadedSolution(Solution oldSolution, Solution reloadedSolution)
        {
            var newSolution = reloadedSolution;

            // keep open documents using same text
            foreach (var docId in this.GetOpenDocumentIds())
            {
                if (newSolution.ContainsDocument(docId))
                {
                    newSolution = newSolution.WithDocumentText(docId, this.GetOpenDocumentText(oldSolution, docId), PreservationMode.PreserveIdentity);
                }
            }

            return newSolution;
        }

        protected virtual Project AdjustReloadedProject(Project oldProject, Project reloadedProject)
        {
            var oldSolution = oldProject.Solution;
            var newSolution = reloadedProject.Solution;

            // keep open documents open using same text
            foreach (var docId in this.GetOpenDocumentIds(oldProject.Id))
            {
                if (newSolution.ContainsDocument(docId))
                {
                    newSolution = newSolution.WithDocumentText(docId, this.GetOpenDocumentText(oldSolution, docId), PreservationMode.PreserveIdentity);
                }
            }

            return newSolution.GetProject(oldProject.Id);
        }

        /// <summary>
        /// Update a project as a result of option changes.
        /// 
        /// this is a temporary workaround until editorconfig becomes real part of roslyn solution snapshot.
        /// until then, this will explicitly move current solution forward when such event happened
        /// </summary>
        internal void OnProjectOptionsChanged(ProjectId projectId)
        {
            using (_serializationLock.DisposableWait())
            {
                var oldSolution = CurrentSolution;
                var newSolution = this.SetCurrentSolution(oldSolution.WithProjectOptionsChanged(projectId));

                RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, oldSolution, newSolution, projectId);
            }
        }
    }
}
