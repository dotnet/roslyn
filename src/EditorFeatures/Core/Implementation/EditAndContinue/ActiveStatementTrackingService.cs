// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
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
        [ExportWorkspaceServiceFactory(typeof(IActiveStatementTrackingService), ServiceLayer.Editor), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory() { }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new ActiveStatementTrackingService(workspaceServices.Workspace);
        }

        private TrackingSession? _session;
        private readonly Workspace _workspace;

        /// <summary>
        /// Raised whenever span tracking starts or ends.
        /// </summary>
        public event Action? TrackingChanged;

        public ActiveStatementTrackingService(Workspace workspace)
        {
            _workspace = workspace;
        }

        public void StartTracking()
        {
            var newSession = new TrackingSession(_workspace, _workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>());
            if (Interlocked.CompareExchange(ref _session, newSession, null) != null)
            {
                newSession.EndTracking();
                Contract.Fail("Can only track active statements for a single edit session.");
            }

            // fire and forget on a background thread:
            _ = newSession.TrackActiveSpansAsync();

            TrackingChanged?.Invoke();
        }

        public void EndTracking()
        {
            var session = Interlocked.Exchange(ref _session, null);
            Contract.ThrowIfNull(session, "Active statement tracking not started.");
            session.EndTracking();

            TrackingChanged?.Invoke();
        }

        public Task<ImmutableArray<TextSpan>> GetSpansAsync(Document document, CancellationToken cancellationToken)
            => _session?.GetSpansAsync(document, cancellationToken) ?? SpecializedTasks.EmptyImmutableArray<TextSpan>();

        public Task<ImmutableArray<ActiveStatementTrackingSpan>> GetAdjustedTrackingSpansAsync(Document document, ITextSnapshot snapshot, CancellationToken cancellationToken)
            => _session?.GetAdjustedTrackingSpansAsync(document, snapshot, cancellationToken) ?? SpecializedTasks.EmptyImmutableArray<ActiveStatementTrackingSpan>();

        // internal for testing
        internal sealed class TrackingSession
        {
            private readonly Workspace _workspace;
            private readonly CancellationTokenSource _cancellationSource;
            private readonly IEditAndContinueWorkspaceService _encService;

            #region lock(_trackingSpans)

            // Spans that are tracking active statements contained in the specified document.
            private readonly Dictionary<DocumentId, ImmutableArray<ActiveStatementTrackingSpan>> _trackingSpans;

            #endregion

            public TrackingSession(Workspace workspace, IEditAndContinueWorkspaceService encService)
            {
                _workspace = workspace;
                _trackingSpans = new Dictionary<DocumentId, ImmutableArray<ActiveStatementTrackingSpan>>();
                _cancellationSource = new CancellationTokenSource();
                _encService = encService;

                _workspace.DocumentOpened += DocumentOpened;
                _workspace.DocumentClosed += DocumentClosed;
            }

            internal Dictionary<DocumentId, ImmutableArray<ActiveStatementTrackingSpan>> Test_GetTrackingSpans()
                => _trackingSpans;

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
            }

            private void DocumentClosed(object? sender, DocumentEventArgs e)
            {
                lock (_trackingSpans)
                {
                    _trackingSpans.Remove(e.Document.Id);
                }
            }

            private void DocumentOpened(object? sender, DocumentEventArgs e)
                => _ = TrackActiveSpansAsync(e.Document, _cancellationSource.Token);

            private async Task TrackActiveSpansAsync(Document document, CancellationToken cancellationToken)
            {
                try
                {
                    if (!TryGetSnapshot(document, out var snapshot))
                    {
                        return;
                    }

                    _ = await GetAdjustedTrackingSpansAsync(document, snapshot, cancellationToken).ConfigureAwait(false);
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

            internal Task TrackActiveSpansAsync()
            {
                try
                {
                    return Task.Run(() => TrackActiveSpansAsync(_cancellationSource.Token), _cancellationSource.Token);
                }
                catch (TaskCanceledException)
                {
                    return Task.CompletedTask;
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

                    var baseActiveStatementSpans = await _encService.GetBaseActiveStatementSpansAsync(openDocumentIds, cancellationToken).ConfigureAwait(false);
                    if (baseActiveStatementSpans.IsDefault)
                    {
                        // Edit session not in progress.
                        return;
                    }

                    Debug.Assert(openDocumentIds.Length == baseActiveStatementSpans.Length);
                    var currentSolution = _workspace.CurrentSolution;

                    lock (_trackingSpans)
                    {
                        for (var i = 0; i < baseActiveStatementSpans.Length; i++)
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

            private static ImmutableArray<ActiveStatementTrackingSpan> CreateTrackingSpans(ITextSnapshot snapshot, ImmutableArray<(LinePositionSpan span, ActiveStatementFlags flags)> activeStatementSpans)
            {
                return activeStatementSpans.SelectAsArray(spanAndFlags =>
                    new ActiveStatementTrackingSpan(snapshot.CreateTrackingSpan(snapshot.GetTextSpan(spanAndFlags.span).ToSpan(), SpanTrackingMode.EdgeExclusive), spanAndFlags.flags));
            }

            private static ImmutableArray<ActiveStatementTrackingSpan> UpdateTrackingSpans(
                ITextSnapshot snapshot,
                ImmutableArray<ActiveStatementTrackingSpan> oldSpans,
                ImmutableArray<(LinePositionSpan, ActiveStatementFlags)> newSpans)
            {
                Debug.Assert(oldSpans.Length == newSpans.Length);

                ArrayBuilder<ActiveStatementTrackingSpan>? lazyBuilder = null;

                for (var i = 0; i < oldSpans.Length; i++)
                {
                    var oldSpan = oldSpans[i];
                    var (newLineSpan, newFlags) = newSpans[i];

                    // flags must be preserved (can't change leaf statement to non-leaf, etc.):
                    Contract.ThrowIfFalse(oldSpan.Flags == newFlags);

                    var newSpan = snapshot.GetTextSpan(newLineSpan).ToSpan();
                    if (oldSpan.Span.GetSpan(snapshot).Span != newSpan)
                    {
                        if (lazyBuilder == null)
                        {
                            lazyBuilder = ArrayBuilder<ActiveStatementTrackingSpan>.GetInstance(oldSpans.Length);
                            lazyBuilder.AddRange(oldSpans);
                        }

                        lazyBuilder[i] = new ActiveStatementTrackingSpan(
                            snapshot.CreateTrackingSpan(newSpan, SpanTrackingMode.EdgeExclusive),
                            newFlags);
                    }
                }

                return lazyBuilder?.ToImmutableAndFree() ?? oldSpans;
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

            public async Task<ImmutableArray<TextSpan>> GetSpansAsync(Document document, CancellationToken cancellationToken)
            {
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                lock (_trackingSpans)
                {
                    if (_trackingSpans.TryGetValue(document.Id, out var documentSpans) && !documentSpans.IsDefaultOrEmpty)
                    {
                        var snapshot = sourceText.FindCorrespondingEditorTextSnapshot();
                        if (snapshot != null && snapshot.TextBuffer == documentSpans.First().Span.TextBuffer)
                        {
                            return documentSpans.SelectAsArray(s => s.Span.GetSpan(snapshot).Span.ToTextSpan());
                        }
                    }
                }

                return ImmutableArray<TextSpan>.Empty;
            }

            /// <summary>
            /// Updates tracking spans with the latest positions of all active statements in the specified document snapshot.
            /// </summary>
            internal async Task<ImmutableArray<ActiveStatementTrackingSpan>> GetAdjustedTrackingSpansAsync(Document document, ITextSnapshot snapshot, CancellationToken cancellationToken)
            {
                try
                {
                    Debug.Assert(TryGetSnapshot(document, out var s) && s == snapshot);

                    var activeStatementSpanProvider = new DocumentActiveStatementSpanProvider(cancellationToken => GetSpansAsync(document, cancellationToken));
                    var activeStatementSpans = await _encService.GetAdjustedActiveStatementSpansAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);

                    lock (_trackingSpans)
                    {
                        var hasExistingSpans = _trackingSpans.TryGetValue(document.Id, out var oldSpans);

                        if (activeStatementSpans.IsDefault)
                        {
                            // Unable to determine the latest positions of active statements for the document snapshot (the document might have syntax errors).
                            // Return the current tracking spans.
                            return oldSpans.NullToEmpty();
                        }

                        return _trackingSpans[document.Id] = hasExistingSpans ?
                            UpdateTrackingSpans(snapshot, oldSpans, activeStatementSpans) :
                            CreateTrackingSpans(snapshot, activeStatementSpans);
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

                return ImmutableArray<ActiveStatementTrackingSpan>.Empty;
            }
        }
    }
}
