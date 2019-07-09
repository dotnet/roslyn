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
using Microsoft.CodeAnalysis.ErrorReporting;
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
        private TrackingSession _sessionOpt;

        [ImportingConstructor]
        public ActiveStatementTrackingService()
        {
        }

        public event Action<bool> TrackingSpansChanged;

        private void OnTrackingSpansChanged(bool leafChanged)
        {
            TrackingSpansChanged?.Invoke(leafChanged);
        }

        public void StartTracking(EditSession editSession)
        {
            var newSession = new TrackingSession(this, editSession);
            if (Interlocked.CompareExchange(ref _sessionOpt, newSession, null) != null)
            {
                newSession.EndTracking();
                Contract.Fail("Can only track active statements for a single edit session.");
            }
        }

        public void EndTracking()
        {
            var session = Interlocked.Exchange(ref _sessionOpt, null);
            Contract.ThrowIfNull(session, "Active statement tracking not started.");
            session.EndTracking();
        }

        public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
        {
            var session = _sessionOpt;
            if (session == null)
            {
                span = default;
                return false;
            }

            return session.TryGetSpan(id, source, out span);
        }

        public IEnumerable<ActiveStatementTextSpan> GetSpans(SourceText source)
        {
            return _sessionOpt?.GetSpans(source) ?? SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();
        }

        public void UpdateActiveStatementSpans(SourceText source, IEnumerable<(ActiveStatementId, ActiveStatementTextSpan)> spans)
        {
            _sessionOpt?.UpdateActiveStatementSpans(source, spans);
        }

        private sealed class TrackingSession
        {
            private struct ActiveStatementTrackingSpan
            {
                public readonly ITrackingSpan Span;
                public readonly ActiveStatementFlags Flags;

                public ActiveStatementTrackingSpan(ITrackingSpan trackingSpan, ActiveStatementFlags flags)
                {
                    Span = trackingSpan;
                    Flags = flags;
                }
            }

            private readonly ActiveStatementTrackingService _service;
            private readonly EditSession _editSession;

            #region lock(_trackingSpans)

            // Spans that are tracking active statements contained in the specified document,
            // or null if we lost track of them due to document being closed and reopened.
            private readonly Dictionary<DocumentId, ActiveStatementTrackingSpan[]> _trackingSpans;

            #endregion

            public TrackingSession(ActiveStatementTrackingService service, EditSession editSession)
            {
                Debug.Assert(service != null);
                Debug.Assert(editSession != null);

                _service = service;
                _editSession = editSession;
                _trackingSpans = new Dictionary<DocumentId, ActiveStatementTrackingSpan[]>();

                editSession.BaseSolution.Workspace.DocumentOpened += DocumentOpened;

                // fire and forget on a background thread:
                try
                {
                    Task.Run(TrackActiveSpansAsync, _editSession.Cancellation.Token);
                }
                catch (TaskCanceledException)
                {
                }
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
                _ = DocumentOpenedAsync(e.Document);
            }

            private async Task DocumentOpenedAsync(Document document)
            {
                try
                {
                    var baseActiveStatements = await _editSession.BaseActiveStatements.GetValueAsync(_editSession.Cancellation.Token).ConfigureAwait(false);

                    if (baseActiveStatements.DocumentMap.TryGetValue(document.Id, out var documentActiveStatements) &&
                        TryGetSnapshot(document, out var snapshot))
                    {
                        lock (_trackingSpans)
                        {
                            TrackActiveSpansNoLock(document, snapshot, documentActiveStatements);
                        }

                        var leafChanged = documentActiveStatements.Contains(s => s.IsLeaf);
                        _service.OnTrackingSpansChanged(leafChanged);
                    }
                }
                catch (OperationCanceledException)
                {
                    // nop
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                    // nop
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

            private async Task TrackActiveSpansAsync()
            {
                try
                {
                    var baseActiveStatements = await _editSession.BaseActiveStatements.GetValueAsync(_editSession.Cancellation.Token).ConfigureAwait(false);

                    lock (_trackingSpans)
                    {
                        foreach (var (documentId, documentActiveStatements) in baseActiveStatements.DocumentMap)
                        {
                            var document = _editSession.BaseSolution.GetDocument(documentId);
                            if (TryGetSnapshot(document, out var snapshot))
                            {
                                TrackActiveSpansNoLock(document, snapshot, documentActiveStatements);
                            }
                        }
                    }

                    _service.OnTrackingSpansChanged(leafChanged: true);
                }
                catch (OperationCanceledException)
                {
                    // nop
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                    // nop
                }
            }

            private void TrackActiveSpansNoLock(
                Document document,
                ITextSnapshot snapshot,
                ImmutableArray<ActiveStatement> documentActiveStatements)
            {
                if (!_trackingSpans.TryGetValue(document.Id, out var documentTrackingSpans))
                {
                    SetTrackingSpansNoLock(document.Id, CreateTrackingSpans(snapshot, documentActiveStatements));
                }
                else if (documentTrackingSpans != null)
                {
                    Debug.Assert(documentTrackingSpans.Length > 0);

                    if (documentTrackingSpans[0].Span.TextBuffer != snapshot.TextBuffer)
                    {
                        // The underlying text buffer has changed - this means that our tracking spans 
                        // are no longer useful, we need to refresh them. Refresh happens asynchronously 
                        // as we calculate document delta.
                        SetTrackingSpansNoLock(document.Id, null);

                        // fire and forget:
                        _ = RefreshTrackingSpansAsync(document, snapshot);
                    }
                }
            }

            private async Task RefreshTrackingSpansAsync(Document document, ITextSnapshot snapshot)
            {
                try
                {
                    var documentAnalysis = await _editSession.GetDocumentAnalysis(document).GetValueAsync(_editSession.Cancellation.Token).ConfigureAwait(false);

                    // Do nothing if the statements aren't available (in presence of compilation errors).
                    if (!documentAnalysis.ActiveStatements.IsDefault)
                    {
                        RefreshTrackingSpans(document.Id, snapshot, documentAnalysis.ActiveStatements);
                    }
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    // nop
                }
            }

            private void RefreshTrackingSpans(DocumentId documentId, ITextSnapshot snapshot, ImmutableArray<ActiveStatement> documentActiveStatements)
            {
                var updated = false;
                lock (_trackingSpans)
                {
                    if (_trackingSpans.TryGetValue(documentId, out var documentTrackingSpans) && documentTrackingSpans == null)
                    {
                        SetTrackingSpansNoLock(documentId, CreateTrackingSpans(snapshot, documentActiveStatements));
                        updated = true;
                    }
                }

                if (updated)
                {
                    _service.OnTrackingSpansChanged(leafChanged: true);
                }
            }

            private void SetTrackingSpansNoLock(DocumentId documentId, ActiveStatementTrackingSpan[] spansOpt)
            {
                _trackingSpans[documentId] = spansOpt;
            }

            private static ActiveStatementTrackingSpan[] CreateTrackingSpans(ITextSnapshot snapshot, ImmutableArray<ActiveStatement> documentActiveStatements)
            {
                var result = new ActiveStatementTrackingSpan[documentActiveStatements.Length];
                for (var i = 0; i < result.Length; i++)
                {
                    var span = snapshot.GetTextSpan(documentActiveStatements[i].Span).ToSpan();
                    result[i] = CreateTrackingSpan(snapshot, span, documentActiveStatements[i].Flags);
                }

                return result;
            }

            private static ActiveStatementTrackingSpan CreateTrackingSpan(ITextSnapshot snapshot, Span span, ActiveStatementFlags flags)
            {
                return new ActiveStatementTrackingSpan(snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive), flags);
            }

            public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
            {
                lock (_trackingSpans)
                {
                    if (_trackingSpans.TryGetValue(id.DocumentId, out var documentSpans) && documentSpans != null)
                    {
                        var trackingSpan = documentSpans[id.Ordinal].Span;
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

                ActiveStatementTrackingSpan[] documentTrackingSpans;
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
                if (snapshot == null || snapshot.TextBuffer != documentTrackingSpans[0].Span.TextBuffer)
                {
                    return SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();
                }

                var result = new ActiveStatementTextSpan[documentTrackingSpans.Length];
                for (var i = 0; i < documentTrackingSpans.Length; i++)
                {
                    Debug.Assert(documentTrackingSpans[i].Span.TextBuffer == snapshot.TextBuffer);

                    result[i] = new ActiveStatementTextSpan(
                        documentTrackingSpans[i].Flags,
                        documentTrackingSpans[i].Span.GetSpan(snapshot).Span.ToTextSpan());
                }

                return result;
            }

            public void UpdateActiveStatementSpans(SourceText source, IEnumerable<(ActiveStatementId, ActiveStatementTextSpan)> spans)
            {
                var leafUpdated = false;
                var updated = false;
                lock (_trackingSpans)
                {
                    foreach (var (id, span) in spans)
                    {
                        if (_trackingSpans.TryGetValue(id.DocumentId, out var documentSpans) && documentSpans != null)
                        {
                            var snapshot = source.FindCorrespondingEditorTextSnapshot();

                            // Avoid updating spans if the buffer has changed. 
                            // Buffer change is handled by DocumentOpened event.
                            if (snapshot != null && snapshot.TextBuffer == documentSpans[id.Ordinal].Span.TextBuffer)
                            {
                                documentSpans[id.Ordinal] = new ActiveStatementTrackingSpan(snapshot.CreateTrackingSpan(span.Span.ToSpan(), SpanTrackingMode.EdgeExclusive), span.Flags);

                                if (!leafUpdated)
                                {
                                    leafUpdated = span.IsLeaf;
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
