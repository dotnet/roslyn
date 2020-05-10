// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    internal sealed class ActiveStatementTrackingService : IActiveStatementTrackingService
    {
        private TrackingSession? _session;
        private readonly Workspace _workspace;

        public event Action? TrackingSpansChanged;

        public ActiveStatementTrackingService(Workspace workspace)
        {
            _workspace = workspace;
        }

        private void OnTrackingSpansChanged()
            => TrackingSpansChanged?.Invoke();

        public void StartTracking()
        {
            var newSession = new TrackingSession(_workspace, this);
            if (Interlocked.CompareExchange(ref _session, newSession, null) != null)
            {
                newSession.EndTracking();
                Contract.Fail("Can only track active statements for a single edit session.");
            }
        }

        public void EndTracking()
        {
            var session = Interlocked.Exchange(ref _session, null);
            Contract.ThrowIfNull(session, "Active statement tracking not started.");
            session.EndTracking();
        }

        public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
        {
            var session = _session;
            if (session == null)
            {
                span = default;
                return false;
            }

            return session.TryGetSpan(id, source, out span);
        }

        public IEnumerable<ActiveStatementTextSpan> GetSpans(SourceText source)
            => _session?.GetSpans(source) ?? SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();

        public void UpdateActiveStatementSpans(SourceText source, IEnumerable<(ActiveStatementId, ActiveStatementTextSpan)> spans)
            => _session?.UpdateActiveStatementSpans(source, spans);

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
            private readonly Workspace _workspace;
            private readonly CancellationTokenSource _cancellationSource;

            #region lock(_trackingSpans)

            // Spans that are tracking active statements contained in the specified document.
            private readonly Dictionary<DocumentId, ActiveStatementTrackingSpan[]> _trackingSpans;

            #endregion

            public TrackingSession(Workspace workspace, ActiveStatementTrackingService service)
            {
                _service = service;
                _workspace = workspace;
                _trackingSpans = new Dictionary<DocumentId, ActiveStatementTrackingSpan[]>();
                _cancellationSource = new CancellationTokenSource();

                _workspace.DocumentOpened += DocumentOpened;
                _workspace.DocumentClosed += DocumentClosed;

                // fire and forget on a background thread:
                try
                {
                    _ = Task.Run(() => TrackActiveSpansAsync(_cancellationSource.Token), _cancellationSource.Token);
                }
                catch (TaskCanceledException)
                {
                }
            }

            public void EndTracking()
            {
                _cancellationSource.Cancel();
                _cancellationSource.Dispose();

                _workspace.DocumentOpened -= DocumentOpened;
                _workspace.DocumentClosed -= DocumentClosed;

                lock (_trackingSpans)
                {
                    _trackingSpans.Clear();
                }

                _service.OnTrackingSpansChanged();
            }

            private void DocumentClosed(object sender, DocumentEventArgs e)
            {
                lock (_trackingSpans)
                {
                    _trackingSpans.Remove(e.Document.Id);
                }
            }

            private void DocumentOpened(object sender, DocumentEventArgs e)
                => _ = DocumentOpenedAsync(e.Document, _cancellationSource.Token);

            private async Task DocumentOpenedAsync(Document document, CancellationToken cancellationToken)
            {
                try
                {
                    if (!TryGetSnapshot(document, out var snapshot))
                    {
                        return;
                    }

                    var encService = _workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
                    var baseActiveStatementSpans = await encService.GetBaseActiveStatementSpansAsync(ImmutableArray.Create(document.Id), cancellationToken).ConfigureAwait(false);
                    if (baseActiveStatementSpans.IsDefault)
                    {
                        // Edit session not in progress.
                        return;
                    }

                    lock (_trackingSpans)
                    {
                        // Create tracking spans if they have not been created for this open document yet
                        // (avoids race condition with TrackActiveSpansAsync).
                        if (!_trackingSpans.ContainsKey(document.Id))
                        {
                            _trackingSpans.Add(document.Id, CreateTrackingSpans(snapshot, baseActiveStatementSpans.Single()));
                        }
                    }

                    _service.OnTrackingSpansChanged();
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

            private async Task TrackActiveSpansAsync(CancellationToken cancellationToken)
            {
                try
                {
                    var openDocumentIds = _workspace.GetOpenDocumentIds().ToImmutableArray();
                    if (openDocumentIds.Length == 0)
                    {
                        return;
                    }

                    var encService = _workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
                    var baseActiveStatementSpans = await encService.GetBaseActiveStatementSpansAsync(openDocumentIds, cancellationToken).ConfigureAwait(false);
                    if (baseActiveStatementSpans.IsDefault)
                    {
                        // Edit session not in progress.
                        return;
                    }

                    Debug.Assert(openDocumentIds.Length == baseActiveStatementSpans.Length);
                    var currentSolution = _workspace.CurrentSolution;

                    lock (_trackingSpans)
                    {
                        for (int i = 0; i < baseActiveStatementSpans.Length; i++)
                        {
                            var document = currentSolution.GetDocument(openDocumentIds[i]);
                            if (document == null)
                            {
                                // Document has been deleted.
                                continue;
                            }

                            if (!TryGetSnapshot(document, out var snapshot))
                            {
                                // Document is not open in an editor or a corresponding snapshot doesn't exist anymore.
                                continue;
                            }

                            if (!_trackingSpans.ContainsKey(document.Id))
                            {
                                // Create tracking spans if they have not been created for this open document yet
                                // (avoids race condition with DocumentOpen event handler).
                                _trackingSpans.Add(document.Id, CreateTrackingSpans(snapshot, baseActiveStatementSpans[i]));
                            }
                        }
                    }

                    _service.OnTrackingSpansChanged();
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

            private static ActiveStatementTrackingSpan[] CreateTrackingSpans(ITextSnapshot snapshot, ImmutableArray<(LinePositionSpan, ActiveStatementFlags)> activeStatementSpans)
            {
                var result = new ActiveStatementTrackingSpan[activeStatementSpans.Length];
                for (var i = 0; i < result.Length; i++)
                {
                    var (span, flags) = activeStatementSpans[i];
                    result[i] = new ActiveStatementTrackingSpan(snapshot.CreateTrackingSpan(snapshot.GetTextSpan(span).ToSpan(), SpanTrackingMode.EdgeExclusive), flags);
                }

                return result;
            }

            private static bool TryGetSnapshot(Document document, [NotNullWhen(true)] out ITextSnapshot? snapshot)
            {
                if (!document.TryGetText(out var source))
                {
                    snapshot = null;
                    return false;
                }

                snapshot = source.FindCorrespondingEditorTextSnapshot();
                return snapshot != null;
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
                if (document.Project.Solution.Workspace != _workspace)
                {
                    return SpecializedCollections.EmptyEnumerable<ActiveStatementTextSpan>();
                }

                ActiveStatementTrackingSpan[] documentTrackingSpans;
                lock (_trackingSpans)
                {
                    if (!_trackingSpans.TryGetValue(document.Id, out documentTrackingSpans))
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
                var updated = false;
                lock (_trackingSpans)
                {
                    foreach (var (id, span) in spans)
                    {
                        if (_trackingSpans.TryGetValue(id.DocumentId, out var documentSpans))
                        {
                            var snapshot = source.FindCorrespondingEditorTextSnapshot();

                            // Avoid updating spans if the buffer has changed. 
                            // Buffer change is handled by DocumentClosed event.
                            if (snapshot != null && snapshot.TextBuffer == documentSpans[id.Ordinal].Span.TextBuffer)
                            {
                                documentSpans[id.Ordinal] = new ActiveStatementTrackingSpan(snapshot.CreateTrackingSpan(span.Span.ToSpan(), SpanTrackingMode.EdgeExclusive), span.Flags);
                                updated = true;
                            }
                        }
                    }
                }

                if (updated)
                {
                    _service.OnTrackingSpansChanged();
                }
            }
        }
    }
}
