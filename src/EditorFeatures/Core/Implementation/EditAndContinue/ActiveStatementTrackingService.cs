// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    /// <summary>
    /// Tracks active statements for the debugger during an edit session.
    /// </summary>
    /// <remarks>
    /// An active statement is a source statement that occurs in a stack trace of a thread.
    /// Active statements are visualized via a gray marker in the text editor.
    /// </remarks>
    [Export(typeof(IActiveStatementTrackingService))]
    internal sealed class ActiveStatementTrackingService : IActiveStatementTrackingService
    {
        private TrackingSession _session;

        internal ActiveStatementTrackingService()
        {
        }

        public event Action<bool> TrackingSpansChanged;

        private void OnTrackingSpansChanged(bool leafChanged)
        {
            TrackingSpansChanged?.Invoke(leafChanged);
        }

        public void StartTracking(EditSession editSession)
        {
            if (Interlocked.CompareExchange(ref _session, new TrackingSession(this, editSession), null) != null)
            {
                Debug.Assert(false, "Can only track active statements for a single edit session.");
            }
        }

        public void EndTracking()
        {
            TrackingSession session = Interlocked.Exchange(ref _session, null);
            Debug.Assert(session != null, "Active statement tracking not started.");

            session.EndTracking();
        }

        public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
        {
            TrackingSession session = _session;
            if (session == null)
            {
                span = default;
                return false;
            }

            return session.TryGetSpan(id, source, out span);
        }

        public IEnumerable<ActiveStatementTextSpan> GetSpans(SourceText source)
        {
            TrackingSession session = _session;
            if (session == null)
            {
                return SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();
            }

            return session.GetSpans(source);
        }

        public void UpdateActiveStatementSpans(SourceText source, IEnumerable<KeyValuePair<ActiveStatementId, TextSpan>> spans)
        {
            TrackingSession session = _session;
            if (session != null)
            {
                session.UpdateActiveStatementSpans(source, spans);
            }
        }

        private sealed class TrackingSession
        {
            private readonly ActiveStatementTrackingService _service;
            private readonly EditSession _editSession;

            #region lock(TrackingSpans)

            // Spans that are tracking active statements contained in the specified document,
            // or null if we lost track of them due to document being closed and reopened.
            private readonly Dictionary<DocumentId, ITrackingSpan[]> _trackingSpans;

            #endregion

            public TrackingSession(ActiveStatementTrackingService service, EditSession editSession)
            {
                Debug.Assert(service != null);
                Debug.Assert(editSession != null);

                _service = service;
                _editSession = editSession;
                _trackingSpans = new Dictionary<DocumentId, ITrackingSpan[]>();

                editSession.BaseSolution.Workspace.DocumentOpened += DocumentOpened;
                TrackActiveSpans();

                service.OnTrackingSpansChanged(leafChanged: true);
            }

            public void EndTracking()
            {
                _editSession.BaseSolution.Workspace.DocumentOpened -= DocumentOpened;

                lock (_trackingSpans)
                {
                    _trackingSpans.Clear();
                }

                _service.OnTrackingSpansChanged(leafChanged: true);
            }

            private void DocumentOpened(object sender, DocumentEventArgs e)
            {
                if (_editSession.BaseActiveStatements.TryGetValue(e.Document.Id, out var activeStatements) &&
                    TryGetSnapshot(e.Document, out var snapshot))
                {
                    lock (_trackingSpans)
                    {
                        TrackActiveSpansNoLock(e.Document, snapshot, activeStatements);
                    }

                    bool leafChanged = activeStatements.Contains(a => (a.Flags & ActiveStatementFlags.LeafFrame) != 0);
                    _service.OnTrackingSpansChanged(leafChanged);
                }
            }

            private static bool TryGetSnapshot(Document document, out ITextSnapshot snapshot)
            {
                if (!document.TryGetText(out var source))
                {
                    snapshot = null;
                    return false;
                }

                snapshot = source.FindCorrespondingEditorTextSnapshot();
                return snapshot != null;
            }

            private void TrackActiveSpans()
            {
                lock (_trackingSpans)
                {
                    foreach (var entry in _editSession.BaseActiveStatements)
                    {
                        var documentId = entry.Key;
                        Document document = _editSession.BaseSolution.GetDocument(documentId);
                        if (TryGetSnapshot(document, out var snapshot))
                        {
                            TrackActiveSpansNoLock(document, snapshot, entry.Value);
                        }
                    }
                }
            }

            private void TrackActiveSpansNoLock(
                Document document,
                ITextSnapshot snapshot,
                ImmutableArray<ActiveStatementSpan> documentActiveSpans)
            {
                if (!_trackingSpans.TryGetValue(document.Id, out var documentTrackingSpans))
                {
                    SetTrackingSpansNoLock(document.Id, CreateTrackingSpans(snapshot, documentActiveSpans));
                }
                else if (documentTrackingSpans != null)
                {
                    Debug.Assert(documentTrackingSpans.Length > 0);

                    if (documentTrackingSpans[0].TextBuffer != snapshot.TextBuffer)
                    {
                        // The underlying text buffer has changed - this means that our tracking spans 
                        // are no longer useful, we need to refresh them. Refresh happens asynchronously 
                        // as we calculate document delta.
                        SetTrackingSpansNoLock(document.Id, null);
                        RefreshTrackingSpansAsync(document, snapshot);
                    }
                }
            }

            private void RefreshTrackingSpansAsync(Document document, ITextSnapshot snapshot)
            {
                _editSession.GetDocumentAnalysis(document).GetValueAsync(_editSession.Cancellation.Token).SafeContinueWith(task =>
                {
                    // Do nothing if the statements aren't available (in presence of compilation errors).
                    if (!task.Result.ActiveStatements.IsDefault)
                    {
                        RefreshTrackingSpans(document.Id, snapshot, task.Result.ActiveStatements);
                    }
                }, _editSession.Cancellation.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }

            private void RefreshTrackingSpans(DocumentId documentId, ITextSnapshot snapshot, ImmutableArray<LinePositionSpan> documentActiveSpans)
            {
                bool updated = false;
                lock (_trackingSpans)
                {
                    if (_trackingSpans.TryGetValue(documentId, out var documentTrackingSpans) && documentTrackingSpans == null)
                    {
                        SetTrackingSpansNoLock(documentId, CreateTrackingSpans(snapshot, documentActiveSpans));
                        updated = true;
                    }
                }

                if (updated)
                {
                    _service.OnTrackingSpansChanged(leafChanged: true);
                }
            }

            private void SetTrackingSpansNoLock(DocumentId documentId, ITrackingSpan[] spans)
            {
                Debug.Assert(spans == null || spans.Length == _editSession.BaseActiveStatements[documentId].Length);
                _trackingSpans[documentId] = spans;
            }

            private static ITrackingSpan[] CreateTrackingSpans(ITextSnapshot snapshot, ImmutableArray<ActiveStatementSpan> documentActiveSpans)
            {
                var result = new ITrackingSpan[documentActiveSpans.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    var span = snapshot.GetTextSpan(documentActiveSpans[i].Span).ToSpan();
                    result[i] = CreateTrackingSpan(snapshot, span);
                }

                return result;
            }

            private ITrackingSpan[] CreateTrackingSpans(ITextSnapshot snapshot, ImmutableArray<LinePositionSpan> documentActiveSpans)
            {
                var result = new ITrackingSpan[documentActiveSpans.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    var span = snapshot.GetTextSpan(documentActiveSpans[i]).ToSpan();
                    result[i] = CreateTrackingSpan(snapshot, span);
                }

                return result;
            }

            private static ITrackingSpan CreateTrackingSpan(ITextSnapshot snapshot, Span span)
            {
                return snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            }

            public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
            {
                lock (_trackingSpans)
                {
                    if (_trackingSpans.TryGetValue(id.DocumentId, out var documentSpans) && documentSpans != null)
                    {
                        var trackingSpan = documentSpans[id.Ordinal];
                        var snapshot = source.FindCorrespondingEditorTextSnapshot();

                        if (snapshot != null && snapshot.TextBuffer == trackingSpan.TextBuffer)
                        {
                            span = trackingSpan.GetSpan(snapshot).Span.ToTextSpan();
                            return true;
                        }
                    }
                }

                span = default;
                return false;
            }

            public IEnumerable<ActiveStatementTextSpan> GetSpans(SourceText source)
            {
                var document = source.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();
                }

                // We might be asked for spans in a different workspace than 
                // the one we maintain tracking spans for (for example, a preview).
                if (document.Project.Solution.Workspace != _editSession.BaseSolution.Workspace)
                {
                    return SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();
                }

                ITrackingSpan[] documentTrackingSpans;
                lock (_trackingSpans)
                {
                    if (!_trackingSpans.TryGetValue(document.Id, out documentTrackingSpans) || documentTrackingSpans == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();
                    }
                }

                Debug.Assert(documentTrackingSpans.Length > 0);
                var snapshot = source.FindCorrespondingEditorTextSnapshot();

                // The document might have been reopened with a new text buffer
                // and we haven't created tracking spans for the new text buffer yet.
                if (snapshot == null || snapshot.TextBuffer != documentTrackingSpans[0].TextBuffer)
                {
                    return SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();
                }

                var baseStatements = _editSession.BaseActiveStatements[document.Id];

                Debug.Assert(documentTrackingSpans.Length == baseStatements.Length);

                var result = new ActiveStatementTextSpan[documentTrackingSpans.Length];
                for (int i = 0; i < documentTrackingSpans.Length; i++)
                {
                    Debug.Assert(documentTrackingSpans[i].TextBuffer == snapshot.TextBuffer);

                    result[i] = new ActiveStatementTextSpan(
                        baseStatements[i].Flags,
                        documentTrackingSpans[i].GetSpan(snapshot).Span.ToTextSpan());
                }

                return result;
            }

            public void UpdateActiveStatementSpans(SourceText source, IEnumerable<KeyValuePair<ActiveStatementId, TextSpan>> spans)
            {
                bool leafUpdated = false;
                bool updated = false;
                lock (_trackingSpans)
                {
                    foreach (var span in spans)
                    {
                        ActiveStatementId id = span.Key;
                        if (_trackingSpans.TryGetValue(id.DocumentId, out var documentSpans) && documentSpans != null)
                        {
                            var snapshot = source.FindCorrespondingEditorTextSnapshot();

                            // Avoid updating spans if the buffer has changed. 
                            // Buffer change is handled by DocumentOpened event.
                            if (snapshot != null && snapshot.TextBuffer == documentSpans[id.Ordinal].TextBuffer)
                            {
                                documentSpans[id.Ordinal] = snapshot.CreateTrackingSpan(span.Value.ToSpan(), SpanTrackingMode.EdgeExclusive);

                                if (!leafUpdated)
                                {
                                    var baseStatements = _editSession.BaseActiveStatements[id.DocumentId];
                                    leafUpdated = (baseStatements[id.Ordinal].Flags & ActiveStatementFlags.LeafFrame) != 0;
                                }

                                updated = true;
                            }
                        }
                    }
                }

                if (updated)
                {
                    _service.OnTrackingSpansChanged(leafUpdated);
                }
            }
        }
    }
}
