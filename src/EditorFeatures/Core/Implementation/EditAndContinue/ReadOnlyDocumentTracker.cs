// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal sealed class ReadOnlyDocumentTracker : ForegroundThreadAffinitizedObject, IDisposable
    {
        private readonly IEditAndContinueWorkspaceService _encService;
        private readonly Workspace _workspace;

        // null after the object is disposed
        private Dictionary<DocumentId, IReadOnlyRegion> _readOnlyRegions;

        // invoked on UI thread
        private readonly Action<DocumentId, SessionReadOnlyReason, ProjectReadOnlyReason> _onReadOnlyDocumentEditAttempt;

        public ReadOnlyDocumentTracker(IEditAndContinueWorkspaceService encService, Action<DocumentId, SessionReadOnlyReason, ProjectReadOnlyReason> onReadOnlyDocumentEditAttempt)
            : base(assertIsForeground: true)
        {
            Debug.Assert(encService.DebuggingSession != null);

            _encService = encService;
            _readOnlyRegions = new Dictionary<DocumentId, IReadOnlyRegion>();
            _workspace = encService.DebuggingSession.InitialSolution.Workspace;
            _onReadOnlyDocumentEditAttempt = onReadOnlyDocumentEditAttempt;

            _workspace.DocumentClosed += OnDocumentClosed;
            _workspace.DocumentOpened += OnDocumentOpened;

            foreach (var documentId in _workspace.GetOpenDocumentIds())
            {
                TrackDocument(documentId);
            }
        }

        public Workspace Workspace
        {
            get { return _workspace; }
        }

        private void OnDocumentOpened(object sender, DocumentEventArgs e)
        {
            InvokeBelowInputPriority(() => TrackDocument(e.Document.Id));
        }

        private void OnDocumentClosed(object sender, DocumentEventArgs e)
        {
            // The buffer is gone by now, so we don't need to remove the read-only region from it, just clean up our dictionary.
            InvokeBelowInputPriority(() =>
            {
                if (_readOnlyRegions != null)
                {
                    _readOnlyRegions.Remove(e.Document.Id);
                }
            });
        }

        private void TrackDocument(DocumentId documentId)
        {
            AssertIsForeground();

            if (_readOnlyRegions == null || _readOnlyRegions.ContainsKey(documentId))
            {
                return;
            }

            var textBuffer = GetTextBuffer(_workspace, documentId);
            using (var readOnlyEdit = textBuffer.CreateReadOnlyRegionEdit())
            {
                _readOnlyRegions.Add(documentId, readOnlyEdit.CreateDynamicReadOnlyRegion(Span.FromBounds(0, readOnlyEdit.Snapshot.Length), SpanTrackingMode.EdgeInclusive, EdgeInsertionMode.Deny,
                    isEdit => IsRegionReadOnly(documentId, isEdit)));

                readOnlyEdit.Apply();
            }
        }

        private bool IsRegionReadOnly(DocumentId documentId, bool isEdit)
        {
            AssertIsForeground();

            SessionReadOnlyReason sessionReason;
            ProjectReadOnlyReason projectReason;
            bool isReadOnly = _encService.IsProjectReadOnly(documentId.ProjectId, out sessionReason, out projectReason);

            if (isEdit && isReadOnly)
            {
                _onReadOnlyDocumentEditAttempt?.Invoke(documentId, sessionReason, projectReason);
            }

            return isReadOnly;
        }

        public void Dispose()
        {
            AssertIsForeground();

            _workspace.DocumentClosed -= OnDocumentClosed;
            _workspace.DocumentOpened -= OnDocumentOpened;

            // event handlers may be queued after the disposal - they should be a no-op
            foreach (var documentAndRegion in _readOnlyRegions)
            {
                RemoveReadOnlyRegionFromBuffer(documentAndRegion.Key, documentAndRegion.Value);
            }

            _readOnlyRegions = null;
        }

        private void RemoveReadOnlyRegionFromBuffer(DocumentId documentId, IReadOnlyRegion region)
        {
            AssertIsForeground();

            var textBuffer = GetTextBuffer(_workspace, documentId);
            using (var readOnlyEdit = textBuffer.CreateReadOnlyRegionEdit())
            {
                readOnlyEdit.RemoveReadOnlyRegion(region);
                readOnlyEdit.Apply();
            }
        }

        private static ITextBuffer GetTextBuffer(Workspace workspace, DocumentId documentId)
        {
            var doc = workspace.CurrentSolution.GetDocument(documentId);
            SourceText text;
            doc.TryGetText(out text);
            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            return snapshot.TextBuffer;
        }
    }
}
