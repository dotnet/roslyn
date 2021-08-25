﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public async ValueTask StartTrackingAsync(Solution solution, IActiveStatementSpanProvider spanProvider, CancellationToken cancellationToken)
        {
            var newSession = new TrackingSession(_workspace, spanProvider);
            if (Interlocked.CompareExchange(ref _session, newSession, null) != null)
            {
                newSession.EndTracking();
                Contract.Fail("Can only track active statements for a single edit session.");
            }

            await newSession.TrackActiveSpansAsync(solution, cancellationToken).ConfigureAwait(false);

            TrackingChanged?.Invoke();
        }

        public void EndTracking()
        {
            var session = Interlocked.Exchange(ref _session, null);
            Contract.ThrowIfNull(session, "Active statement tracking not started.");
            session.EndTracking();

            TrackingChanged?.Invoke();
        }

        public ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(Solution solution, DocumentId? documentId, string filePath, CancellationToken cancellationToken)
            => _session?.GetSpansAsync(solution, documentId, filePath, cancellationToken) ?? new(ImmutableArray<ActiveStatementSpan>.Empty);

        public ValueTask<ImmutableArray<ActiveStatementTrackingSpan>> GetAdjustedTrackingSpansAsync(TextDocument document, ITextSnapshot snapshot, CancellationToken cancellationToken)
            => _session?.GetAdjustedTrackingSpansAsync(document, snapshot, cancellationToken) ?? new(ImmutableArray<ActiveStatementTrackingSpan>.Empty);

        // internal for testing
        internal sealed class TrackingSession
        {
            private readonly Workspace _workspace;
            private readonly CancellationTokenSource _cancellationSource = new();
            private readonly IActiveStatementSpanProvider _spanProvider;
            private readonly ICompileTimeSolutionProvider _compileTimeSolutionProvider;

            #region lock(_trackingSpans)

            /// <summary>
            /// Spans that are tracking active statements contained in the document of given file path.
            /// For each document the array contains spans for all active statements present in the file
            /// (even if they have been deleted, in which case the spans are empty).
            /// </summary>
            private readonly Dictionary<string, ImmutableArray<ActiveStatementTrackingSpan>> _trackingSpans = new();

            #endregion

            public TrackingSession(Workspace workspace, IActiveStatementSpanProvider spanProvider)
            {
                _workspace = workspace;
                _spanProvider = spanProvider;
                _compileTimeSolutionProvider = workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>();

                _workspace.DocumentOpened += DocumentOpened;
                _workspace.DocumentClosed += DocumentClosed;
            }

            internal Dictionary<string, ImmutableArray<ActiveStatementTrackingSpan>> Test_GetTrackingSpans()
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
                if (e.Document.FilePath != null)
                {
                    lock (_trackingSpans)
                    {
                        _trackingSpans.Remove(e.Document.FilePath);
                    }
                }
            }

            private void DocumentOpened(object? sender, DocumentEventArgs e)
                => _ = TrackActiveSpansAsync(e.Document, _cancellationSource.Token);

            private async Task TrackActiveSpansAsync(Document designTimeDocument, CancellationToken cancellationToken)
            {
                try
                {
                    if (!designTimeDocument.DocumentState.SupportsEditAndContinue())
                    {
                        return;
                    }

                    var compileTimeSolution = _compileTimeSolutionProvider.GetCompileTimeSolution(designTimeDocument.Project.Solution);
                    var compileTimeDocument = await compileTimeSolution.GetDocumentAsync(designTimeDocument.Id, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                    if (compileTimeDocument == null || !TryGetSnapshot(compileTimeDocument, out var snapshot))
                    {
                        return;
                    }

                    _ = await GetAdjustedTrackingSpansAsync(compileTimeDocument, snapshot, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // nop
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e))
                {
                    // nop
                }
            }

            internal async Task TrackActiveSpansAsync(Solution solution, CancellationToken cancellationToken)
            {
                try
                {
                    var openDocumentIds = _workspace.GetOpenDocumentIds().ToImmutableArray();
                    if (openDocumentIds.Length == 0)
                    {
                        return;
                    }

                    var baseActiveStatementSpans = await _spanProvider.GetBaseActiveStatementSpansAsync(solution, openDocumentIds, cancellationToken).ConfigureAwait(false);
                    if (baseActiveStatementSpans.IsDefault)
                    {
                        // Edit session not in progress.
                        return;
                    }

                    Debug.Assert(openDocumentIds.Length == baseActiveStatementSpans.Length);
                    using var _ = ArrayBuilder<TextDocument?>.GetInstance(out var documents);

                    foreach (var id in openDocumentIds)
                    {
                        // active statements may be in any document kind (#line may in theory map to analyzer config as well, no need to exclude it):
                        documents.Add(await solution.GetTextDocumentAsync(id, cancellationToken).ConfigureAwait(false));
                    }

                    lock (_trackingSpans)
                    {
                        for (var i = 0; i < baseActiveStatementSpans.Length; i++)
                        {
                            var document = documents[i];
                            if (document?.FilePath == null)
                            {
                                // Document has been deleted, doesn't have a path or is an open design-time document (which does not exist in the compile-time solution)
                                continue;
                            }

                            if (!TryGetSnapshot(document, out var snapshot))
                            {
                                // Document is not open in an editor or a corresponding snapshot doesn't exist anymore.
                                continue;
                            }

                            if (!_trackingSpans.ContainsKey(document.FilePath))
                            {
                                // Create tracking spans if they have not been created for this open document yet
                                // (avoids race condition with DocumentOpen event handler).
                                _trackingSpans.Add(document.FilePath, CreateTrackingSpans(snapshot, baseActiveStatementSpans[i]));
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // nop
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e))
                {
                    // nop
                }
            }

            private static ImmutableArray<ActiveStatementTrackingSpan> CreateTrackingSpans(ITextSnapshot snapshot, ImmutableArray<ActiveStatementSpan> activeStatementSpans)
                => activeStatementSpans.SelectAsArray((span, snapshot) => ActiveStatementTrackingSpan.Create(snapshot, span), snapshot);

            private static ImmutableArray<ActiveStatementTrackingSpan> UpdateTrackingSpans(
                ITextSnapshot snapshot,
                ImmutableArray<ActiveStatementTrackingSpan> oldSpans,
                ImmutableArray<ActiveStatementSpan> newSpans)
            {
                Debug.Assert(oldSpans.Length == newSpans.Length);

                ArrayBuilder<ActiveStatementTrackingSpan>? lazyBuilder = null;

                for (var i = 0; i < oldSpans.Length; i++)
                {
                    var oldSpan = oldSpans[i];
                    var newSpan = newSpans[i];

                    Contract.ThrowIfFalse(oldSpan.Flags == newSpan.Flags);
                    Contract.ThrowIfFalse(oldSpan.Ordinal == newSpan.Ordinal);

                    var newTextSpan = snapshot.GetTextSpan(newSpan.LineSpan).ToSpan();
                    if (oldSpan.Span.GetSpan(snapshot).Span != newTextSpan)
                    {
                        if (lazyBuilder == null)
                        {
                            lazyBuilder = ArrayBuilder<ActiveStatementTrackingSpan>.GetInstance(oldSpans.Length);
                            lazyBuilder.AddRange(oldSpans);
                        }

                        lazyBuilder[i] = new ActiveStatementTrackingSpan(
                            snapshot.CreateTrackingSpan(newTextSpan, SpanTrackingMode.EdgeExclusive),
                            newSpan.Ordinal,
                            newSpan.Flags,
                            newSpan.UnmappedDocumentId);
                    }
                }

                return lazyBuilder?.ToImmutableAndFree() ?? oldSpans;
            }

            private static bool TryGetSnapshot(TextDocument document, [NotNullWhen(true)] out ITextSnapshot? snapshot)
            {
                if (!document.TryGetText(out var source))
                {
                    snapshot = null;
                    return false;
                }

                snapshot = source.FindCorrespondingEditorTextSnapshot();
                return snapshot != null;
            }

            /// <summary>
            /// Returns location of the tracking spans in the specified <see cref="Document"/> snapshot (#line target document).
            /// </summary>
            /// <returns>Empty array if tracking spans are not available for the document.</returns>
            public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(Solution solution, DocumentId? documentId, string filePath, CancellationToken cancellationToken)
            {
                documentId ??= solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();

                var document = await solution.GetTextDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                if (document == null)
                {
                    return ImmutableArray<ActiveStatementSpan>.Empty;
                }

                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                lock (_trackingSpans)
                {
                    if (_trackingSpans.TryGetValue(filePath, out var documentSpans) && !documentSpans.IsDefaultOrEmpty)
                    {
                        var snapshot = sourceText.FindCorrespondingEditorTextSnapshot();
                        if (snapshot != null && snapshot.TextBuffer == documentSpans.First().Span.TextBuffer)
                        {
                            return documentSpans.SelectAsArray(s => new ActiveStatementSpan(s.Ordinal, s.Span.GetSpan(snapshot).ToLinePositionSpan(), s.Flags, s.UnmappedDocumentId));
                        }
                    }
                }

                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            /// <summary>
            /// Updates tracking spans with the latest positions of all active statements in the specified document snapshot (#line target document) and returns them.
            /// </summary>
            internal async ValueTask<ImmutableArray<ActiveStatementTrackingSpan>> GetAdjustedTrackingSpansAsync(TextDocument document, ITextSnapshot snapshot, CancellationToken cancellationToken)
            {
                try
                {
                    if (document.FilePath == null)
                    {
                        return ImmutableArray<ActiveStatementTrackingSpan>.Empty;
                    }

                    Debug.Assert(TryGetSnapshot(document, out var s) && s == snapshot);

                    var solution = document.Project.Solution;

                    var activeStatementSpans = await _spanProvider.GetAdjustedActiveStatementSpansAsync(
                        document,
                        (documentId, filePath, cancellationToken) => GetSpansAsync(solution, documentId, filePath, cancellationToken),
                        cancellationToken).ConfigureAwait(false);

                    Contract.ThrowIfTrue(activeStatementSpans.IsDefault);

                    lock (_trackingSpans)
                    {
                        var hasExistingSpans = _trackingSpans.TryGetValue(document.FilePath, out var oldSpans);

                        if (activeStatementSpans.IsEmpty)
                        {
                            // Unable to determine the latest positions of active statements for the document snapshot (the document is out-of-sync).
                            // Return the current tracking spans.
                            return oldSpans.NullToEmpty();
                        }

                        return _trackingSpans[document.FilePath] = hasExistingSpans ?
                            UpdateTrackingSpans(snapshot, oldSpans, activeStatementSpans) :
                            CreateTrackingSpans(snapshot, activeStatementSpans);
                    }
                }
                catch (OperationCanceledException)
                {
                    // nop
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e))
                {
                    // nop
                }

                return ImmutableArray<ActiveStatementTrackingSpan>.Empty;
            }
        }
    }
}
