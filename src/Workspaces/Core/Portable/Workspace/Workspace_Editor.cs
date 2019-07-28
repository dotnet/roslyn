// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // The parts of a workspace that deal with open documents
    public abstract partial class Workspace
    {
        // open documents
        private readonly Dictionary<ProjectId, HashSet<DocumentId>> _projectToOpenDocumentsMap = new Dictionary<ProjectId, HashSet<DocumentId>>();

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
        /// True if this workspace supports manually changing the active context document of a text buffer by calling <see cref="SetDocumentContext(DocumentId)" />.
        /// </summary>
        internal virtual bool CanChangeActiveContextDocument => false;

        private void ClearOpenDocuments()
        {
            List<DocumentId> docIds;
            using (_stateLock.DisposableWait())
            {
                docIds = _projectToOpenDocumentsMap.Values.SelectMany(x => x).ToList();
            }

            foreach (var docId in docIds)
            {
                this.ClearOpenDocument(docId);
            }
        }

        private void ClearOpenDocuments(ProjectId projectId)
        {
            HashSet<DocumentId> openDocs;
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

        protected void ClearOpenDocument(DocumentId documentId)
        {
            using (_stateLock.DisposableWait())
            {
                _projectToOpenDocumentsMap.MultiRemove(documentId.ProjectId, documentId);

                // Stop tracking the buffer or update the documentId associated with the buffer.
                if (_textTrackers.TryGetValue(documentId, out var tracker))
                {
                    tracker.Disconnect();
                    _textTrackers.Remove(documentId);

                    var currentContextDocumentId = UpdateCurrentContextMapping_NoLock(tracker.TextContainer, documentId);
                    if (currentContextDocumentId == null)
                    {
                        // No documentIds are attached to this buffer, so stop tracking it.
                        this.UnregisterText(tracker.TextContainer);
                    }
                }
            }
        }

        [Obsolete("The isSolutionClosing parameter is now obsolete. Please call the overload without that parameter.")]
        protected void ClearOpenDocument(DocumentId documentId, bool isSolutionClosing)
        {
            ClearOpenDocument(documentId);
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

        /// <summary>
        /// Open the specified analyzer config document in the host environment.
        /// </summary>
        public virtual void OpenAnalyzerConfigDocument(DocumentId documentId, bool activate = true)
        {
            this.CheckCanOpenDocuments();
        }

        /// <summary>
        /// Close the specified analyzer config document in the host environment.
        /// </summary>
        public virtual void CloseAnalyzerConfigDocument(DocumentId documentId)
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
                return _projectToOpenDocumentsMap.TryGetValue(documentId.ProjectId, out var openDocuments) &&
                       openDocuments.Contains(documentId);
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
        /// <see cref="SourceTextContainer"/>.
        /// </summary>
        internal DocumentId GetDocumentIdInCurrentContext(DocumentId documentId)
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

        private SourceTextContainer GetOpenDocumentSourceTextContainer_NoLock(DocumentId documentId)
        {
            // TODO: remove linear search
            return _bufferToAssociatedDocumentsMap.Where(kvp => kvp.Value.Contains(documentId)).Select(kvp => kvp.Key).FirstOrDefault();
        }

        private DocumentId GetDocumentIdInCurrentContext_NoLock(SourceTextContainer container)
        {
            var foundValue = _bufferToDocumentInCurrentContextMap.TryGetValue(container, out var docId);

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
        /// Call this method to tell the host environment to change the current active context to this document. Only supported if
        /// <see cref="CanChangeActiveContextDocument"/> returns true.
        /// </summary>
        internal virtual void SetDocumentContext(DocumentId documentId)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Call this method when a document has been made the active context in the host environment.
        /// </summary>
        protected internal void OnDocumentContextUpdated(DocumentId documentId)
        {
            using (_serializationLock.DisposableWait())
            {
                DocumentId oldActiveContextDocumentId;
                SourceTextContainer container;

                using (_stateLock.DisposableWait())
                {
                    container = GetOpenDocumentSourceTextContainer_NoLock(documentId);

                    if (container == null)
                    {
                        return;
                    }

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

        protected internal void OnDocumentOpened(
            DocumentId documentId, SourceTextContainer textContainer,
            bool isCurrentContext = true)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckDocumentIsInCurrentSolution(documentId);
                CheckDocumentIsClosed(documentId);

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
                _projectToOpenDocumentsMap.MultiAdd(documentId.ProjectId, documentId);
            }
        }

        protected internal void OnAdditionalDocumentOpened(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext = true)
        {
            OnAdditionalOrAnalyzerConfigDocumentOpened(
                documentId,
                textContainer,
                isCurrentContext,
                WorkspaceChangeKind.AdditionalDocumentChanged,
                CheckAdditionalDocumentIsInCurrentSolution,
                withDocumentText: (oldSolution, documentId, newText, mode) => oldSolution.WithAdditionalDocumentText(documentId, newText, mode),
                withDocumentTextAndVersion: (oldSolution, documentId, newTextAndVersion, mode) => oldSolution.WithAdditionalDocumentText(documentId, newTextAndVersion, mode),
                onDocumentTextChanged: (w, id, text, mode) => w.OnAdditionalDocumentTextChanged(id, text, mode));
        }

        protected internal void OnAnalyzerConfigDocumentOpened(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext = true)
        {
            OnAdditionalOrAnalyzerConfigDocumentOpened(
                documentId,
                textContainer,
                isCurrentContext,
                WorkspaceChangeKind.AnalyzerConfigDocumentChanged,
                CheckAnalyzerConfigDocumentIsInCurrentSolution,
                withDocumentText: (oldSolution, documentId, newText, mode) => oldSolution.WithAnalyzerConfigDocumentText(documentId, newText, mode),
                withDocumentTextAndVersion: (oldSolution, documentId, newTextAndVersion, mode) => oldSolution.WithAnalyzerConfigDocumentText(documentId, newTextAndVersion, mode),
                onDocumentTextChanged: (w, id, text, mode) => w.OnAnalyzerConfigDocumentTextChanged(id, text, mode));
        }

        // NOTE: We are only sharing this code between additional documents and analyzer config documents,
        // which are essentially plain text documents. Regular source documents need special handling
        // and hence have a different implementation.
        private void OnAdditionalOrAnalyzerConfigDocumentOpened(
            DocumentId documentId,
            SourceTextContainer textContainer,
            bool isCurrentContext,
            WorkspaceChangeKind workspaceChangeKind,
            Action<DocumentId> checkTextDocumentIsInCurrentSolution,
            Func<Solution, DocumentId, SourceText, PreservationMode, Solution> withDocumentText,
            Func<Solution, DocumentId, TextAndVersion, PreservationMode, Solution> withDocumentTextAndVersion,
            Action<Workspace, DocumentId, SourceText, PreservationMode> onDocumentTextChanged)
        {
            using (_serializationLock.DisposableWait())
            {
                checkTextDocumentIsInCurrentSolution(documentId);
                CheckDocumentIsClosed(documentId);

                var oldSolution = this.CurrentSolution;
                var oldDocument = oldSolution.GetTextDocument(documentId);
                Debug.Assert(oldDocument.Kind == TextDocumentKind.AdditionalDocument || oldDocument.Kind == TextDocumentKind.AnalyzerConfigDocument);

                var oldText = oldDocument.GetTextSynchronously(CancellationToken.None);

                AddToOpenDocumentMap(documentId);

                // keep open document text alive by using PreserveIdentity
                var newText = textContainer.CurrentText;
                Solution currentSolution;

                if (oldText == newText || oldText.ContentEquals(newText))
                {
                    // if the supplied text is the same as the previous text, then also use same version
                    var version = oldDocument.GetTextVersionSynchronously(CancellationToken.None);
                    var newTextAndVersion = TextAndVersion.Create(newText, version, oldDocument.FilePath);
                    currentSolution = withDocumentTextAndVersion(oldSolution, documentId, newTextAndVersion, PreservationMode.PreserveIdentity);
                }
                else
                {
                    currentSolution = withDocumentText(oldSolution, documentId, newText, PreservationMode.PreserveIdentity);
                }

                var newSolution = this.SetCurrentSolution(currentSolution);

                SignupForTextChanges(documentId, textContainer, isCurrentContext, onDocumentTextChanged);

                // Fire and forget.
                this.RaiseWorkspaceChangedEventAsync(workspaceChangeKind, oldSolution, newSolution, documentId: documentId);
            }

            this.RegisterText(textContainer);
        }

        protected internal void OnDocumentClosed(DocumentId documentId, TextLoader reloader, bool updateActiveContext = false)
        {
            // The try/catch here is to find additional telemetry for https://devdiv.visualstudio.com/DevDiv/_queries/query/71ee8553-7220-4b2a-98cf-20edab701fd1/,
            // where we have one theory that OnDocumentClosed is running but failing somewhere in the middle and thus failing to get to the RaiseDocumentClosedEventAsync() line. 
            // We are choosing ReportWithoutCrashAndPropagate because this is a public API that has callers outside VS and also non-VisualStudioWorkspace callers inside VS, and
            // we don't want to be crashing underneath them if they were already handling exceptions or (worse) was using those exceptions for expected code flow.
            try
            {
                using (_serializationLock.DisposableWait())
                {
                    this.CheckDocumentIsInCurrentSolution(documentId);
                    this.CheckDocumentIsOpen(documentId);

                    // forget any open document info
                    ClearOpenDocument(documentId);

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
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected internal void OnAdditionalDocumentClosed(DocumentId documentId, TextLoader reloader)
        {
            OnAdditionalOrAnalyzerConfigDocumentClosed(
                documentId,
                reloader,
                WorkspaceChangeKind.AdditionalDocumentChanged,
                CheckAdditionalDocumentIsInCurrentSolution,
                withTextDocumentTextLoader: (oldSolution, documentId, textLoader, mode) => oldSolution.WithAdditionalDocumentTextLoader(documentId, textLoader, mode));
        }

        protected internal void OnAnalyzerConfigDocumentClosed(DocumentId documentId, TextLoader reloader)
        {
            OnAdditionalOrAnalyzerConfigDocumentClosed(
                documentId,
                reloader,
                WorkspaceChangeKind.AnalyzerConfigDocumentChanged,
                CheckAnalyzerConfigDocumentIsInCurrentSolution,
                withTextDocumentTextLoader: (oldSolution, documentId, textLoader, mode) => oldSolution.WithAnalyzerConfigDocumentTextLoader(documentId, textLoader, mode));
        }

        // NOTE: We are only sharing this code between additional documents and analyzer config documents,
        // which are essentially plain text documents. Regular source documents need special handling
        // and hence have a different implementation.
        private void OnAdditionalOrAnalyzerConfigDocumentClosed(
            DocumentId documentId,
            TextLoader reloader,
            WorkspaceChangeKind workspaceChangeKind,
            Action<DocumentId> checkTextDocumentIsInCurrentSolution,
            Func<Solution, DocumentId, TextLoader, PreservationMode, Solution> withTextDocumentTextLoader)
        {
            using (_serializationLock.DisposableWait())
            {
                checkTextDocumentIsInCurrentSolution(documentId);
                this.CheckDocumentIsOpen(documentId);

                // forget any open document info
                ClearOpenDocument(documentId);

                var oldSolution = this.CurrentSolution;
                var oldDocument = oldSolution.GetTextDocument(documentId);
                Debug.Assert(oldDocument.Kind == TextDocumentKind.AdditionalDocument || oldDocument.Kind == TextDocumentKind.AnalyzerConfigDocument);

                var newSolution = withTextDocumentTextLoader(oldSolution, documentId, reloader, PreservationMode.PreserveValue);
                newSolution = this.SetCurrentSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(workspaceChangeKind, oldSolution, newSolution, documentId: documentId); // don't wait for this
            }
        }

        private void UpdateCurrentContextMapping_NoLock(SourceTextContainer textContainer, DocumentId id, bool isCurrentContext)
        {
            if (_bufferToAssociatedDocumentsMap.TryGetValue(textContainer, out var docIds))
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
            if (!_bufferToAssociatedDocumentsMap.TryGetValue(textContainer, out var docIds))
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
            var doc = solution.GetTextDocument(documentId);
            // text should always be preserved, so TryGetText will succeed.
            var success = doc.TryGetText(out var text);
            Debug.Assert(success);
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
                var document = newSolution.GetTextDocument(docId);
                if (document != null)
                {
                    newSolution = document.WithText(this.GetOpenDocumentText(oldSolution, docId)).Project.Solution;
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
                var document = newSolution.GetTextDocument(docId);
                if (document != null)
                {
                    newSolution = document.WithText(this.GetOpenDocumentText(oldSolution, docId)).Project.Solution;
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
