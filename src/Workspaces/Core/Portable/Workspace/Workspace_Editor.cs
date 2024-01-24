// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // The parts of a workspace that deal with open documents
    public abstract partial class Workspace
    {
        // open documents
        private readonly Dictionary<ProjectId, HashSet<DocumentId>> _projectToOpenDocumentsMap = new();

        // text buffer maps
        /// <summary>
        /// Tracks the document ID in the current context for a source text container for an opened text buffer.
        /// </summary>
        /// <remarks>For each entry in this map, there must be a corresponding entry in <see cref="_bufferToAssociatedDocumentsMap"/> where the document ID in current context is one of associated document IDs.</remarks>
        private readonly Dictionary<SourceTextContainer, DocumentId> _bufferToDocumentInCurrentContextMap = new();

        /// <summary>
        /// Tracks all the associated document IDs for a source text container for an opened text buffer.
        /// </summary>
        private readonly Dictionary<SourceTextContainer, OneOrMany<DocumentId>> _bufferToAssociatedDocumentsMap = new();

        private readonly Dictionary<DocumentId, TextTracker> _textTrackers = new();
        private readonly Dictionary<DocumentId, SourceTextContainer> _documentToAssociatedBufferMap = new();
        private readonly Dictionary<DocumentId, SourceGeneratedDocumentIdentity> _openSourceGeneratedDocumentIdentities = new();

        /// <summary>
        /// True if this workspace supports manually opening and closing documents.
        /// </summary>
        public virtual bool CanOpenDocuments => false;

        /// <summary>
        /// True if this workspace supports manually changing the active context document of a text buffer by calling <see cref="SetDocumentContext(DocumentId)" />.
        /// </summary>
        internal virtual bool CanChangeActiveContextDocument => false;

        internal void ClearOpenDocuments()
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
            HashSet<DocumentId>? openDocs;
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
                if (_documentToAssociatedBufferMap.TryGetValue(documentId, out var textContainer))
                {
                    _documentToAssociatedBufferMap.Remove(documentId);

                    if (_textTrackers.TryGetValue(documentId, out var tracker))
                    {
                        tracker.Disconnect();
                        _textTrackers.Remove(documentId);
                    }

                    var currentContextDocumentId = RemoveDocumentFromCurrentContextMapping_NoLock(textContainer, documentId);
                    if (currentContextDocumentId == null)
                    {
                        // No documentIds are attached to this buffer, so stop tracking it.
                        this.UnregisterText(textContainer);
                    }
                }
            }
        }

        [Obsolete("The isSolutionClosing parameter is now obsolete. Please call the overload without that parameter.")]
        protected void ClearOpenDocument(DocumentId documentId, bool isSolutionClosing)
            => ClearOpenDocument(documentId);

        /// <summary>
        /// Open the specified document in the host environment.
        /// </summary>
        public virtual void OpenDocument(DocumentId documentId, bool activate = true)
            => this.CheckCanOpenDocuments();

        /// <summary>
        /// Close the specified document in the host environment.
        /// </summary>
        public virtual void CloseDocument(DocumentId documentId)
            => this.CheckCanOpenDocuments();

        /// <summary>
        /// Open the specified additional document in the host environment.
        /// </summary>
        public virtual void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
            => this.CheckCanOpenDocuments();

        /// <summary>
        /// Close the specified additional document in the host environment.
        /// </summary>
        public virtual void CloseAdditionalDocument(DocumentId documentId)
            => this.CheckCanOpenDocuments();

        /// <summary>
        /// Open the specified analyzer config document in the host environment.
        /// </summary>
        public virtual void OpenAnalyzerConfigDocument(DocumentId documentId, bool activate = true)
            => this.CheckCanOpenDocuments();

        /// <summary>
        /// Close the specified analyzer config document in the host environment.
        /// </summary>
        public virtual void CloseAnalyzerConfigDocument(DocumentId documentId)
            => this.CheckCanOpenDocuments();

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
        public virtual IEnumerable<DocumentId> GetOpenDocumentIds(ProjectId? projectId = null)
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

                return _projectToOpenDocumentsMap.SelectManyAsArray(kvp => kvp.Value);
            }
        }

        /// <summary>
        /// Gets the ids for documents in the <see cref="CurrentSolution"/> snapshot associated with the given <paramref name="container"/>.
        /// Documents are normally associated with a text container when the documents are opened.
        /// </summary>
        public virtual IEnumerable<DocumentId> GetRelatedDocumentIds(SourceTextContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var documentId = GetDocumentIdInCurrentContext(container);
            if (documentId == null)
            {
                return ImmutableArray<DocumentId>.Empty;
            }

            return CurrentSolution.GetRelatedDocumentIds(documentId);
        }

        /// <summary>
        /// Gets the id for the document associated with the given text container in its current context.
        /// Documents are normally associated with a text container when the documents are opened.
        /// </summary>
        public virtual DocumentId? GetDocumentIdInCurrentContext(SourceTextContainer container)
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

        private DocumentId? GetDocumentIdInCurrentContext_NoLock(SourceTextContainer container)
            => _bufferToDocumentInCurrentContextMap.TryGetValue(container, out var documentId) ? documentId : null;

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
                if (container != null)
                {
                    var currentContextId = GetDocumentIdInCurrentContext_NoLock(container);
                    Contract.ThrowIfNull(currentContextId, "The document is open, so we should have had some context ID.");
                    return currentContextId;
                }

                return documentId;
            }
        }

        private SourceTextContainer? GetOpenDocumentSourceTextContainer_NoLock(DocumentId documentId)
        {
            // TODO: remove linear search
            return _bufferToAssociatedDocumentsMap.Where(kvp => kvp.Value.Contains(documentId)).Select(kvp => kvp.Key).FirstOrDefault();
        }

        internal bool TryGetOpenSourceGeneratedDocumentIdentity(DocumentId id, out SourceGeneratedDocumentIdentity documentIdentity)
        {
            using (_serializationLock.DisposableWait())
            {
                return _openSourceGeneratedDocumentIdentities.TryGetValue(id, out documentIdentity);
            }
        }

        /// <summary>
        /// Call this method to tell the host environment to change the current active context to this document. Only supported if
        /// <see cref="CanChangeActiveContextDocument"/> returns true.
        /// </summary>
        internal virtual void SetDocumentContext(DocumentId documentId)
            => throw new NotSupportedException();

        /// <summary>
        /// Call this method when a document has been made the active context in the host environment.
        /// </summary>
        protected internal void OnDocumentContextUpdated(DocumentId documentId)
        {
            using (_serializationLock.DisposableWait())
            {
                DocumentId oldActiveContextDocumentId;
                SourceTextContainer? container;

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

        protected internal void OnDocumentOpened(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext = true)
            => OnDocumentOpened(documentId, textContainer, isCurrentContext, requireDocumentPresentAndClosed: true);

        internal virtual ValueTask TryOnDocumentOpenedAsync(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext, CancellationToken cancellationToken)
        {
            OnDocumentOpened(documentId, textContainer, isCurrentContext, requireDocumentPresentAndClosed: false);
            return ValueTaskFactory.CompletedTask;
        }

        internal void OnDocumentOpened(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext, bool requireDocumentPresentAndClosed)
        {
            SetCurrentSolution(
                data: (@this: this, documentId, textContainer, isCurrentContext, requireDocumentPresentAndClosed),
                static (oldSolution, data) =>
                {
                    var (@this, documentId, textContainer, _, requireDocumentPresentAndClosed) = data;

                    var oldDocument = oldSolution.GetRequiredDocument(documentId);
                    if (oldDocument is null)
                    {
                        // Didn't have a document.  Throw if required.  Bail out gracefully if not.
                        if (requireDocumentPresentAndClosed)
                        {
                            throw new ArgumentException(string.Format(
                                WorkspacesResources._0_is_not_part_of_the_workspace,
                                @this.GetDocumentName(documentId)));
                        }
                        else
                        {
                            return oldSolution;
                        }
                    }

                    if (@this.IsDocumentOpen(documentId))
                    {
                        // Document was already open.  Throw if required.  Bail out gracefully if not.
                        if (requireDocumentPresentAndClosed)
                            @this.CheckDocumentIsClosed(documentId);
                        else
                            return oldSolution;
                    }

                    var oldDocumentState = oldDocument.State;

                    var newText = textContainer.CurrentText;
                    if (oldDocument.TryGetText(out var oldText) &&
                        oldDocument.TryGetTextVersion(out var version))
                    {
                        // Optimize the case where we've already got the previous text and version.
                        var newTextAndVersion = GetProperTextAndVersion(oldText, newText, version, oldDocumentState.FilePath);

                        // keep open document text alive by using PreserveIdentity
                        return oldSolution.WithDocumentText(documentId, newTextAndVersion, PreservationMode.PreserveIdentity);
                    }
                    else
                    {
                        // We don't have the old text or version.  Rather than trying to reuse a version that we still have, let's just assume the file has changed.
                        // keep open document text alive by using PreserveIdentity
                        return oldSolution.WithDocumentText(documentId, newText, PreservationMode.PreserveValue);
                    }
                },
                onAfterUpdate: static (oldSolution, newSolution, data) =>
                {
                    var (@this, documentId, textContainer, isCurrentContext, requireDocumentPresentAndClosed) = data;

                    @this.AddToOpenDocumentMap(documentId);
                    @this.SignupForTextChanges(documentId, textContainer, isCurrentContext, (w, id, text, mode) => w.OnDocumentTextChanged(id, text, mode));

                    var newDoc = newSolution.GetRequiredDocument(documentId);
                    @this.OnDocumentTextChanged(newDoc);

                    // Fire and forget that the workspace is changing.
                    @this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId: documentId);

                    // We fire 2 events on source document opened.
                    @this.RaiseDocumentOpenedEventAsync(newDoc);
                    @this.RaiseTextDocumentOpenedEventAsync(newDoc);
                });

            // TODO: why is this here, and not in onAfterUpdate?
            this.RegisterText(textContainer);
        }

        /// <summary>
        /// Registers a SourceTextContainer to a source generated document. Unlike <see
        /// cref="OnDocumentOpened(DocumentId, SourceTextContainer, bool)" />, this doesn't result in the workspace
        /// being updated any time the contents of the container is changed; instead this ensures that features going
        /// from the text container to the buffer back to a document get a usable document.
        /// </summary>
        // TODO: switch this protected once we have confidence in API shape
        internal void OnSourceGeneratedDocumentOpened(
            SourceTextContainer textContainer,
            SourceGeneratedDocument document)
        {
            using (_serializationLock.DisposableWait())
            {
                var documentId = document.Identity.DocumentId;
                CheckDocumentIsClosed(documentId);
                AddToOpenDocumentMap(documentId);

                _documentToAssociatedBufferMap.Add(documentId, textContainer);
                _openSourceGeneratedDocumentIdentities.Add(documentId, document.Identity);

                UpdateCurrentContextMapping_NoLock(textContainer, documentId, isCurrentContext: true);

                // Fire and forget that the workspace is changing.
                // We raise 2 events for source document opened.
                var token = _taskQueue.Listener.BeginAsyncOperation(nameof(OnSourceGeneratedDocumentOpened));
                _ = RaiseDocumentOpenedEventAsync(document).CompletesAsyncOperation(token);
                token = _taskQueue.Listener.BeginAsyncOperation(TextDocumentOpenedEventName);
                _ = RaiseTextDocumentOpenedEventAsync(document).CompletesAsyncOperation(token);
            }

            this.RegisterText(textContainer);
        }

        internal void OnSourceGeneratedDocumentClosed(SourceGeneratedDocument document)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckDocumentIsOpen(document.Id);

                Contract.ThrowIfFalse(_openSourceGeneratedDocumentIdentities.Remove(document.Id));
                ClearOpenDocument(document.Id);

                // Fire and forget that the workspace is changing.
                // We raise 2 events for source document closed.
                var token = _taskQueue.Listener.BeginAsyncOperation(nameof(OnSourceGeneratedDocumentClosed));
                _ = RaiseDocumentClosedEventAsync(document).CompletesAsyncOperation(token);
                token = _taskQueue.Listener.BeginAsyncOperation(TextDocumentClosedEventName);
                _ = RaiseTextDocumentClosedEventAsync(document).CompletesAsyncOperation(token);
            }
        }

        private static TextAndVersion GetProperTextAndVersion(SourceText oldText, SourceText newText, VersionStamp version, string? filePath)
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
            _documentToAssociatedBufferMap.Add(documentId, textContainer);
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
                CheckAdditionalDocumentIsInSolution,
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
                CheckAnalyzerConfigDocumentIsInSolution,
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
            Action<Solution, DocumentId> checkTextDocumentIsInSolution,
            Func<Solution, DocumentId, SourceText, PreservationMode, Solution> withDocumentText,
            Func<Solution, DocumentId, TextAndVersion, PreservationMode, Solution> withDocumentTextAndVersion,
            Action<Workspace, DocumentId, SourceText, PreservationMode> onDocumentTextChanged)
        {
            SetCurrentSolution(
                data: (@this: this, documentId, textContainer, isCurrentContext, workspaceChangeKind, checkTextDocumentIsInSolution, withDocumentText, withDocumentTextAndVersion, onDocumentTextChanged),
                static (oldSolution, data) =>
                {
                    var documentId = data.documentId;

                    data.checkTextDocumentIsInSolution(oldSolution, documentId);
                    data.@this.CheckDocumentIsClosed(documentId);

                    var oldDocument = oldSolution.GetRequiredTextDocument(documentId);
                    Debug.Assert(oldDocument.Kind is TextDocumentKind.AdditionalDocument or TextDocumentKind.AnalyzerConfigDocument);

                    var oldText = oldDocument.GetTextSynchronously(CancellationToken.None);

                    // keep open document text alive by using PreserveIdentity
                    var newText = data.textContainer.CurrentText;

                    if (oldText == newText || oldText.ContentEquals(newText))
                    {
                        // if the supplied text is the same as the previous text, then also use same version
                        var version = oldDocument.GetTextVersionSynchronously(CancellationToken.None);
                        var newTextAndVersion = TextAndVersion.Create(newText, version, oldDocument.FilePath);
                        return data.withDocumentTextAndVersion(oldSolution, documentId, newTextAndVersion, PreservationMode.PreserveIdentity);
                    }
                    else
                    {
                        return data.withDocumentText(oldSolution, documentId, newText, PreservationMode.PreserveIdentity);
                    }
                },
                onAfterUpdate: static (oldSolution, newSolution, data) =>
                {
                    var documentId = data.documentId;

                    data.@this.AddToOpenDocumentMap(documentId);
                    data.@this.SignupForTextChanges(documentId, data.textContainer, data.isCurrentContext, data.onDocumentTextChanged);

                    // Fire and forget.
                    data.@this.RaiseWorkspaceChangedEventAsync(data.workspaceChangeKind, oldSolution, newSolution, documentId: documentId);

                    // Fire and forget.
                    var newDoc = newSolution.GetRequiredTextDocument(documentId);
                    data.@this.RaiseTextDocumentOpenedEventAsync(newDoc);
                });

            this.RegisterText(textContainer);
        }

        /// <summary>
        /// Tries to close the document identified by <paramref name="documentId"/>.  This is only needed by
        /// implementations of ILspWorkspace to indicate that the workspace should try to transition to the closed state
        /// for this document, but can bail out gracefully if they don't know about it (for example if they haven't
        /// heard about the file from the project system).  Subclasses should determine what file contents they should
        /// transition to if the file is within the workspace.
        /// </summary>
        /// <param name="documentId"></param>
        internal virtual ValueTask TryOnDocumentClosedAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

#pragma warning disable IDE0060 // Remove unused parameter 'updateActiveContext' - shipped public API.
        protected internal void OnDocumentClosed(DocumentId documentId, TextLoader reloader, bool updateActiveContext = false)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            OnDocumentClosedEx(documentId, reloader, requireDocumentPresentAndOpen: true);
        }

        private protected void OnDocumentClosedEx(DocumentId documentId, TextLoader reloader, bool requireDocumentPresentAndOpen)
        {
            // The try/catch here is to find additional telemetry for https://devdiv.visualstudio.com/DevDiv/_queries/query/71ee8553-7220-4b2a-98cf-20edab701fd1/,
            // where we have one theory that OnDocumentClosed is running but failing somewhere in the middle and thus failing to get to the RaiseDocumentClosedEventAsync() line. 
            // We are choosing ReportWithoutCrashAndPropagate because this is a public API that has callers outside VS and also non-VisualStudioWorkspace callers inside VS, and
            // we don't want to be crashing underneath them if they were already handling exceptions or (worse) was using those exceptions for expected code flow.
            try
            {
                this.SetCurrentSolution(
                    data: (@this: this, documentId, reloader, requireDocumentPresentAndOpen),
                    static (oldSolution, data) =>
                    {
                        var (@this, documentId, reloader, requireDocumentPresentAndOpen) = data;

                        var document = oldSolution.GetDocument(documentId);
                        if (document is null)
                        {
                            // Didn't have a document.  Throw if required.  Bail out gracefully if not.
                            if (requireDocumentPresentAndOpen)
                            {
                                throw new ArgumentException(string.Format(
                                    WorkspacesResources._0_is_not_part_of_the_workspace,
                                    @this.GetDocumentName(documentId)));
                            }
                            else
                            {
                                return oldSolution;
                            }
                        }

                        if (!@this.IsDocumentOpen(documentId))
                        {
                            // Document wasn't open.  Throw if required.  Bail out gracefull if not.
                            if (requireDocumentPresentAndOpen)
                                @this.CheckDocumentIsOpen(documentId);
                            else
                                return oldSolution;
                        }

                        return oldSolution.WithDocumentTextLoader(documentId, reloader, PreservationMode.PreserveValue);
                    },
                    onBeforeUpdate: static (oldSolution, newSolution, data) =>
                    {
                        var (@this, documentId, _, _) = data;

                        // forget any open document info
                        @this.ClearOpenDocument(documentId);

                        @this.OnDocumentClosing(documentId);
                    },
                    onAfterUpdate: static (oldSolution, newSolution, data) =>
                    {
                        var (@this, documentId, _, _) = data;

                        var newDoc = newSolution.GetRequiredDocument(documentId);
                        @this.OnDocumentTextChanged(newDoc);

                        @this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId: documentId); // don't wait for this

                        // We fire and forget 2 events on source document closed.
                        @this.RaiseDocumentClosedEventAsync(newDoc);
                        @this.RaiseTextDocumentClosedEventAsync(newDoc);
                    });
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.General))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        protected internal void OnAdditionalDocumentClosed(DocumentId documentId, TextLoader reloader)
        {
            OnAdditionalOrAnalyzerConfigDocumentClosed(
                documentId,
                reloader,
                WorkspaceChangeKind.AdditionalDocumentChanged,
                CheckAdditionalDocumentIsInSolution,
                withTextDocumentTextLoader: (oldSolution, documentId, textLoader, mode) => oldSolution.WithAdditionalDocumentTextLoader(documentId, textLoader, mode));
        }

        protected internal void OnAnalyzerConfigDocumentClosed(DocumentId documentId, TextLoader reloader)
        {
            OnAdditionalOrAnalyzerConfigDocumentClosed(
                documentId,
                reloader,
                WorkspaceChangeKind.AnalyzerConfigDocumentChanged,
                CheckAnalyzerConfigDocumentIsInSolution,
                withTextDocumentTextLoader: (oldSolution, documentId, textLoader, mode) => oldSolution.WithAnalyzerConfigDocumentTextLoader(documentId, textLoader, mode));
        }

        // NOTE: We are only sharing this code between additional documents and analyzer config documents,
        // which are essentially plain text documents. Regular source documents need special handling
        // and hence have a different implementation.
        private void OnAdditionalOrAnalyzerConfigDocumentClosed(
            DocumentId documentId,
            TextLoader reloader,
            WorkspaceChangeKind workspaceChangeKind,
            Action<Solution, DocumentId> checkTextDocumentIsInSolution,
            Func<Solution, DocumentId, TextLoader, PreservationMode, Solution> withTextDocumentTextLoader)
        {
            this.SetCurrentSolution(
                data: (@this: this, documentId, reloader, workspaceChangeKind, checkTextDocumentIsInSolution, withTextDocumentTextLoader),
                static (oldSolution, data) =>
                {
                    var documentId = data.documentId;
                    data.checkTextDocumentIsInSolution(oldSolution, documentId);
                    data.@this.CheckDocumentIsOpen(documentId);

                    Debug.Assert(oldSolution.GetRequiredTextDocument(documentId).Kind is TextDocumentKind.AdditionalDocument or TextDocumentKind.AnalyzerConfigDocument);

                    return data.withTextDocumentTextLoader(oldSolution, documentId, data.reloader, PreservationMode.PreserveValue);
                },
                onBeforeUpdate: static (oldSolution, newSolution, data) =>
                {
                    // forget any open document info
                    data.@this.ClearOpenDocument(data.documentId);
                },
                onAfterUpdate: static (oldSolution, newSolution, data) =>
                {
                    data.@this.RaiseWorkspaceChangedEventAsync(
                        data.workspaceChangeKind, oldSolution, newSolution, documentId: data.documentId); // don't wait for this

                    var newDoc = newSolution.GetRequiredTextDocument(data.documentId);
                    data.@this.RaiseTextDocumentClosedEventAsync(newDoc); // don't wait for this
                });
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
        private DocumentId? RemoveDocumentFromCurrentContextMapping_NoLock(SourceTextContainer textContainer, DocumentId closedDocumentId)
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
            if (docIds.IsEmpty)
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
            var doc = solution.GetRequiredTextDocument(documentId);
            // text should always be preserved, so TryGetText will succeed.
            Contract.ThrowIfFalse(doc.TryGetText(out var text));
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

            return newSolution.GetRequiredProject(oldProject.Id);
        }
    }
}
