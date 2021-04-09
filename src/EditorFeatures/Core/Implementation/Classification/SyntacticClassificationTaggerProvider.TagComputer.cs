// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SyntacticClassificationTaggerProvider
    {
        /// <summary>
        /// A classifier that operates only on the syntax of the source and not the semantics.  Note:
        /// this class operates in a hybrid sync/async manner.  Specifically, while classification
        /// happens synchronously, it may be synchronous over a parse tree which is out of date.  Then,
        /// asynchronously, we will attempt to get an up to date parse tree for the file. When we do, we
        /// will determine which sections of the file changed and we will use that to notify the editor
        /// about what needs to be reclassified.
        /// </summary>
        internal partial class TagComputer
        {
            // This is how long we will wait after completing a parse before we tell the editor to
            // re-classify the file. We do this, since the edit may have non-local changes, or the user
            // may have made a change that introduced text that we didn't classify because we hadn't
            // parsed it yet, and we want to get back to a known state.
            private const int ReportChangeDelayInMilliseconds = TaggerConstants.ShortDelay;

            private readonly ITextBuffer _subjectBuffer;
            private readonly WorkspaceRegistration _workspaceRegistration;
            private readonly AsynchronousSerialWorkQueue _workQueue;

            // this will cache previous classification information for a span, so that we can avoid
            // digging into same tree again and again to find exactly same answer
            private readonly LastLineCache _lastLineCache;

            // The latest data about the document being classified that we've cached.  objects can 
            // be accessed from both threads, and must be obtained when this lock is held.
            private readonly object _gate = new();
            private ITextSnapshot _lastProcessedSnapshot;
            private Document _lastProcessedDocument;

            /// <summary>
            /// Data stored by the <see cref="IClassificationService"/> that can be computed in the bg which it will
            /// benefit from when it is called back for classifications later.
            /// </summary>
            private object _lastProcessedCachedData;

            private Workspace _workspace;
            private CancellationTokenSource _reportChangeCancellationSource;

            private readonly IAsynchronousOperationListener _listener;
            private readonly IForegroundNotificationService _notificationService;
            private readonly ClassificationTypeMap _typeMap;
            private readonly SyntacticClassificationTaggerProvider _taggerProvider;

            /// <summary>
            /// Timeout before we cancel the work to diff and return whatever we have.
            /// </summary>
            private readonly TimeSpan _diffTimeout;

            private int _taggerReferenceCount;

            public TagComputer(
                SyntacticClassificationTaggerProvider taggerProvider,
                ITextBuffer subjectBuffer,
                IForegroundNotificationService notificationService,
                IAsynchronousOperationListener asyncListener,
                ClassificationTypeMap typeMap,
                TimeSpan diffTimeout)
            {
                _subjectBuffer = subjectBuffer;
                _notificationService = notificationService;
                _listener = asyncListener;
                _typeMap = typeMap;
                _taggerProvider = taggerProvider;
                _diffTimeout = diffTimeout;
                _workQueue = new AsynchronousSerialWorkQueue(taggerProvider._threadingContext, asyncListener);
                _reportChangeCancellationSource = new CancellationTokenSource();

                _lastLineCache = new LastLineCache();

                _workspaceRegistration = Workspace.GetWorkspaceRegistration(subjectBuffer.AsTextContainer());
                _workspaceRegistration.WorkspaceChanged += OnWorkspaceRegistrationChanged;

                if (_workspaceRegistration.Workspace != null)
                {
                    ConnectToWorkspace(_workspaceRegistration.Workspace);
                }
            }

            private void OnWorkspaceRegistrationChanged(object sender, EventArgs e)
            {
                // We both try to connect synchronously, and register for workspace registration events.
                // It's possible (particularly in tests), to connect in the startup path, but then get a
                // previously scheduled, but not yet delivered event.  Don't bother connecting to the
                // same workspace again in that case.
                var newWorkspace = _workspaceRegistration.Workspace;
                if (newWorkspace == _workspace)
                {
                    return;
                }

                DisconnectFromWorkspace();

                if (newWorkspace != null)
                {
                    ConnectToWorkspace(newWorkspace);
                }
            }

            internal void IncrementReferenceCount()
                => _taggerReferenceCount++;

            internal void DecrementReferenceCountAndDisposeIfNecessary()
            {
                _taggerReferenceCount--;

                if (_taggerReferenceCount == 0)
                {
                    DisconnectFromWorkspace();
                    _workspaceRegistration.WorkspaceChanged -= OnWorkspaceRegistrationChanged;
                    _taggerProvider.DisconnectTagComputer(_subjectBuffer);
                }
            }

            private void ResetLastParsedDocument()
            {
                lock (_gate)
                {
                    _lastProcessedDocument = null;
                    _lastProcessedCachedData = null;
                }
            }

            private void ConnectToWorkspace(Workspace workspace)
            {
                ResetLastParsedDocument();

                _workspace = workspace;
                _workspace.WorkspaceChanged += this.OnWorkspaceChanged;
                _workspace.DocumentOpened += this.OnDocumentOpened;
                _workspace.DocumentActiveContextChanged += this.OnDocumentActiveContextChanged;

                var textContainer = _subjectBuffer.AsTextContainer();

                var documentId = _workspace.GetDocumentIdInCurrentContext(textContainer);
                if (documentId != null)
                {
                    var document = workspace.CurrentSolution.GetDocument(documentId);
                    if (document != null)
                    {
                        EnqueueProcessSnapshot(document);
                    }
                }
            }

            public void DisconnectFromWorkspace()
            {
                _reportChangeCancellationSource.Cancel();

                if (_workspace != null)
                {
                    _workspace.WorkspaceChanged -= this.OnWorkspaceChanged;
                    _workspace.DocumentOpened -= this.OnDocumentOpened;
                    _workspace.DocumentActiveContextChanged -= this.OnDocumentActiveContextChanged;

                    _workspace = null;

                    ResetLastParsedDocument();
                }
            }

            private void EnqueueProcessSnapshot(Document newDocument)
            {
                if (newDocument != null)
                {
                    _workQueue.EnqueueBackgroundTask(c => this.EnqueueProcessSnapshotWorkerAsync(newDocument, c), GetType() + ".EnqueueParseSnapshotTask.1", CancellationToken.None);
                }
            }

            private async Task EnqueueProcessSnapshotWorkerAsync(Document currentDocument, CancellationToken cancellationToken)
            {
                // we will enqueue new one soon, cancel pending refresh right away
                _reportChangeCancellationSource.Cancel();

                var currentText = await currentDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var currentSnapshot = currentText.FindCorrespondingEditorTextSnapshot();
                if (currentSnapshot == null)
                {
                    // It's possible that we're seeing a notification for an update that happened
                    // just before the file was opened, and so the document we're given is still the
                    // old one.
                    return;
                }

                var service = TryGetClassificationService(currentSnapshot);
                object currentCachedData = null;
                var changedSpan = currentSnapshot.GetFullSpan();
                if (service != null)
                {
                    var latencyTracker = new RequestLatencyTracker(SyntacticLspLogger.RequestType.SyntacticTagger);
                    using (latencyTracker)
                    {
                        // preemptively allow the classification service to compute and cache data for this file.  For
                        // example, in C# and VB we will parse the file so that are called from tagger from UI thread,
                        // we have the root of the tree ready to go.
                        currentCachedData = await service.GetDataToCacheAsync(currentDocument, cancellationToken).ConfigureAwait(false);

                        // Query the service to determine waht span of the document actually changed and should be
                        // reclassified in the host editor.
                        changedSpan = await GetChangedSpanAsync(currentDocument, currentSnapshot, service, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Once we're past this point, we're mutating our internal state so we cannot cancel past that.
                cancellationToken = CancellationToken.None;

                lock (_gate)
                {
                    _lastProcessedSnapshot = currentSnapshot;
                    _lastProcessedDocument = currentDocument;
                    _lastProcessedCachedData = currentCachedData;
                }

                _reportChangeCancellationSource = new CancellationTokenSource();
                _notificationService.RegisterNotification(() =>
                    {
                        _workQueue.AssertIsForeground();
                        ReportChangedSpan(changedSpan);
                    },
                    ReportChangeDelayInMilliseconds,
                    _listener.BeginAsyncOperation("ReportEntireFileChanged"),
                    _reportChangeCancellationSource.Token);
            }

            private async Task<SnapshotSpan> GetChangedSpanAsync(
                Document currentDocument, ITextSnapshot currentSnapshot,
                IClassificationService service, CancellationToken cancellationToken)
            {
                // We don't need to grab _lastProcessedDocument in a lock.  We don't care which version of the previous
                // doc we grab, just that we grab some prior version.  This is only used 
                var previousDocument = _lastProcessedDocument;
                if (previousDocument != null)
                {
                    var changeRange = await ComputeChangeRangeAsync(
                        previousDocument, currentDocument, service, cancellationToken).ConfigureAwait(false);
                    if (changeRange != null)
                        return currentSnapshot.GetSpan(changeRange.Value.Span.Start, changeRange.Value.NewLength);
                }

                // Couldn't compute a narrower range.  Just the mark the entire file as changed.
                return currentSnapshot.GetFullSpan();
            }

            private async Task<TextChangeRange?> ComputeChangeRangeAsync(
                Document previousDocument, Document currentDocument,
                IClassificationService classificationService, CancellationToken cancellationToken)
            {
                // We want to compute a minimal change, but we don't want this to run for too long.  So do the
                // computation work in the threadpool, but also gate how much time we can spend here so that we can let
                // the editor know about the size of the change asap.
                var computeTaskWithTimeout =
                    classificationService.ComputeSyntacticChangeRangeAsync(previousDocument, currentDocument, cancellationToken)
                                         .WithTimeout(_diffTimeout);
                try
                {
                    return await computeTaskWithTimeout.ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    return null;
                }
            }

            private void ReportChangedSpan(SnapshotSpan changeSpan)
            {
                lock (_gate)
                {
                    var snapshot = _lastProcessedSnapshot;
                    if (snapshot.Version.ReiteratedVersionNumber != changeSpan.Snapshot.Version.ReiteratedVersionNumber)
                    {
                        // wait for next call
                        return;
                    }
                }

                this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(changeSpan));
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                using (Logger.LogBlock(FunctionId.Tagger_SyntacticClassification_TagComputer_GetTags, CancellationToken.None))
                {
                    if (spans.Count > 0 && _workspace != null)
                    {
                        var result = GetTagsWorker(spans);
                        if (result != null)
                            return result;
                    }

                    return SpecializedCollections.EmptyEnumerable<ITagSpan<IClassificationTag>>();
                }
            }

            private IClassificationService TryGetClassificationService(ITextSnapshot snapshot)
            {
                var languageServices = _workspace.Services.GetLanguageServices(snapshot.ContentType);
                if (languageServices == null)
                    return null;

                return languageServices.GetService<IClassificationService>();
            }

            private IEnumerable<ITagSpan<IClassificationTag>> GetTagsWorker(NormalizedSnapshotSpanCollection spans)
            {
                var classificationService = TryGetClassificationService(spans[0].Snapshot);
                if (classificationService == null)
                    return null;

                var classifiedSpans = ClassificationUtilities.GetOrCreateClassifiedSpanList();

                foreach (var span in spans)
                {
                    AddClassifiedSpans(classificationService, span, classifiedSpans);
                }

                return ClassificationUtilities.ConvertAndReturnList(
                    _typeMap, spans[0].Snapshot, classifiedSpans);
            }

            private void AddClassifiedSpans(
                IClassificationService classificationService,
                SnapshotSpan span,
                List<ClassifiedSpan> classifiedSpans)
            {
                // First, get the tree and snapshot that we'll be operating over.  
                // From this point on we'll do all operations over these values.
                ITextSnapshot lastSnapshot;
                Document lastDocument;
                object lastCachedData;

                lock (_gate)
                {
                    lastSnapshot = _lastProcessedSnapshot;
                    lastDocument = _lastProcessedDocument;
                    lastCachedData = _lastProcessedCachedData;
                }

                if (lastDocument == null)
                {
                    // We don't have a syntax tree yet.  Just do a lexical classification of the document.
                    AddClassifiedSpansForTokens(classificationService, span, classifiedSpans);
                    return;
                }

                // We have a tree.  However, the tree may be for an older version of the snapshot.
                // If it is for an older version, then classify that older version and translate
                // the classifications forward.  Otherwise, just classify normally.

                if (lastSnapshot.Version.ReiteratedVersionNumber == span.Snapshot.Version.ReiteratedVersionNumber)
                {
                    AddClassifiedSpansForCurrentTree(
                        classificationService, span, lastDocument, classifiedSpans);
                }
                else
                {
                    // Slightly more complicated.  We have a parse tree, it's just not for the snapshot
                    // we're being asked for.
                    AddClassifiedSpansForPreviousTree(
                        classificationService, span, lastSnapshot, lastDocument, classifiedSpans);
                }

                // Ensure the cached data stays alive for as long as we're calling into the classification service to do
                // the computation.
                GC.KeepAlive(lastCachedData);
            }

            private void AddClassifiedSpansForCurrentTree(
                IClassificationService classificationService,
                SnapshotSpan span,
                Document document,
                List<ClassifiedSpan> classifiedSpans)
            {
                if (!_lastLineCache.TryUseCache(span, out var tempList))
                {
                    tempList = ClassificationUtilities.GetOrCreateClassifiedSpanList();

                    classificationService.AddSyntacticClassificationsAsync(
                        document, span.Span.ToTextSpan(), tempList, CancellationToken.None).Wait(CancellationToken.None);

                    _lastLineCache.Update(span, tempList);
                }

                // simple case.  They're asking for the classifications for a tree that we already have.
                // Just get the results from the tree and return them.

                classifiedSpans.AddRange(tempList);
            }

            private void AddClassifiedSpansForPreviousTree(
                IClassificationService classificationService,
                SnapshotSpan span,
                ITextSnapshot lastSnapshot,
                Document lastDocument,
                List<ClassifiedSpan> classifiedSpans)
            {
                // Slightly more complicated case.  They're asking for the classifications for a
                // different snapshot than what we have a parse tree for.  So we first translate the span
                // that they're asking for so that is maps onto the tree that we have spans for.  We then
                // get the classifications from that tree.  We then take the results and translate them
                // back to the snapshot they want.  Finally, as some of the classifications may have
                // changed, we check for some common cases and touch them up manually so that things
                // look right for the user.

                // Note the handling of SpanTrackingModes here: We do EdgeExclusive while mapping back ,
                // and EdgeInclusive mapping forward. What I've convinced myself is that EdgeExclusive
                // is best when mapping back over a deletion, so that we don't end up classifying all of
                // the deleted code.  In most addition/modification cases, there will be overlap with
                // existing spans, and so we'll end up classifying well.  In the worst case, there is a
                // large addition that doesn't exist when we map back, and so we don't have any
                // classifications for it. That's probably okay, because: 

                // 1. If it's that large, it's likely that in reality there are multiple classification
                // spans within it.

                // 2.We'll eventually call ClassificationsChanged and re-classify that region anyway.

                // When mapping back forward, we use EdgeInclusive so that in the common typing cases we
                // don't end up with half a token colored differently than the other half.

                // See bugs like http://vstfdevdiv:8080/web/wi.aspx?id=6176 for an example of what can
                // happen when this goes wrong.

                // 1) translate the requested span onto the right span for the snapshot that corresponds
                //    to the syntax tree.
                var translatedSpan = span.TranslateTo(lastSnapshot, SpanTrackingMode.EdgeExclusive);
                if (translatedSpan.IsEmpty)
                {
                    // well, there is no information we can get from previous tree, use lexer to
                    // classify given span. soon we will re-classify the region.
                    AddClassifiedSpansForTokens(classificationService, span, classifiedSpans);
                    return;
                }

                var tempList = ClassificationUtilities.GetOrCreateClassifiedSpanList();
                AddClassifiedSpansForCurrentTree(
                    classificationService, translatedSpan, lastDocument, tempList);

                var currentSnapshot = span.Snapshot;
                var currentText = currentSnapshot.AsText();
                foreach (var lastClassifiedSpan in tempList)
                {
                    // 2) Translate those classifications forward so that they correspond to the true
                    //    requested snapshot.
                    var lastSnapshotSpan = lastClassifiedSpan.TextSpan.ToSnapshotSpan(lastSnapshot);
                    var currentSnapshotSpan = lastSnapshotSpan.TranslateTo(currentSnapshot, SpanTrackingMode.EdgeInclusive);

                    var currentClassifiedSpan = new ClassifiedSpan(lastClassifiedSpan.ClassificationType, currentSnapshotSpan.Span.ToTextSpan());

                    // 3) The classifications may be incorrect due to changes in the text.  For example,
                    //    if "clss" becomes "class", then we want to changes the classification from
                    //    'identifier' to 'keyword'.
                    currentClassifiedSpan = classificationService.AdjustStaleClassification(currentText, currentClassifiedSpan);

                    classifiedSpans.Add(currentClassifiedSpan);
                }

                ClassificationUtilities.ReturnClassifiedSpanList(tempList);
            }

            private static void AddClassifiedSpansForTokens(
                IClassificationService classificationService,
                SnapshotSpan span,
                List<ClassifiedSpan> classifiedSpans)
            {
                classificationService.AddLexicalClassifications(
                    span.Snapshot.AsText(), span.Span.ToTextSpan(), classifiedSpans, CancellationToken.None);
            }

            private void OnDocumentActiveContextChanged(object sender, DocumentActiveContextChangedEventArgs args)
            {
                if (_workspace != null && _workspace == args.Solution.Workspace)
                {
                    ProcessIfThisDocument(args.Solution, args.NewActiveContextDocumentId);
                }
            }

            private void OnDocumentOpened(object sender, DocumentEventArgs args)
            {
                if (_workspace != null)
                {
                    ProcessIfThisDocument(args.Document.Project.Solution, args.Document.Id);
                }
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
            {
                // We're getting an event for a workspace we already disconnected from
                if (args.NewSolution.Workspace != _workspace)
                {
                    // we are async so we are getting events from previous workspace we were associated with
                    // just ignore them
                    return;
                }

                switch (args.Kind)
                {
                    case WorkspaceChangeKind.ProjectChanged:
                        {
                            var documentId = _workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer());
                            if (documentId == null || documentId.ProjectId != args.ProjectId)
                            {
                                break;
                            }

                            var oldProject = args.OldSolution.GetProject(args.ProjectId);
                            var newProject = args.NewSolution.GetProject(args.ProjectId);

                            // make sure in case of parse config change, we re-colorize whole document. not just edited section.
                            var configChanged = !object.Equals(oldProject.ParseOptions, newProject.ParseOptions);
                            EnqueueProcessSnapshot(newProject.GetDocument(documentId));
                            break;
                        }

                    case WorkspaceChangeKind.DocumentChanged:
                        {
                            ProcessIfThisDocument(args.NewSolution, args.DocumentId);
                            break;
                        }
                }

                // put a request to update last parsed document.
                // this will make us to enqueue a request per workspace change event per a opened file.
                // if this show up as perf cost, we might need to revisit this design. currently we do this so that our Roslyn Language Service API
                // maintain its consistency.
                var newSolution = args.NewSolution;
                _workQueue.EnqueueBackgroundTask(c => UpdateLastParsedDocumentAsync(newSolution, c), "UpdateLastParsedDocument", CancellationToken.None);
            }

            private async Task UpdateLastParsedDocumentAsync(Solution newSolution, CancellationToken cancellationToken)
            {
                // lastParsedDocument only updated in the same sequential queue so don't need lock to use it
                var lastDocument = Volatile.Read(ref _lastProcessedDocument);
                if (lastDocument == null)
                {
                    return;
                }

                var document = newSolution.GetDocument(lastDocument.Id);
                if (document == null)
                {
                    // document no longer exist. reset it to null, if somebody calls us, we will answer using lexer.
                    ResetLastParsedDocument();
                    return;
                }

                // it is already updated. nothing to do here.
                if (lastDocument == document)
                {
                    return;
                }

                var lastParsedText = await lastDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var lastParsedSnapshot = lastParsedText.FindCorrespondingEditorTextSnapshot();

                var newText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newSnapshot = newText.FindCorrespondingEditorTextSnapshot();
                if (newSnapshot == null)
                {
                    // It's possible that we're seeing a notification for an update that happened
                    // just before the file was opened, and so the document we're given is still the
                    // old one.
                    return;
                }

#if DEBUG
                // this must exist since we are holding it in the field.
                Contract.ThrowIfNull(lastParsedSnapshot);
#endif
                if (lastParsedSnapshot == newSnapshot)
                {
                    // update document to new snapshot with same content
                    lock (_gate)
                    {
                        _lastProcessedDocument = document;
                    }
                }
                else
                {
                    // This workspace change must have also implicitly changed the text of our file. This can happen
                    // if it's a linked file (and we are observing the non-active linked file changing before our own active file)
                    // or some other workspace change (say a SolutionChanged) caused a text edit to happen and we didn't process
                    // it directly. In that case, requeue a parse. This might be a redundant parse in the linked file case
                    // since we might also get a DocumentChanged event for our ID. It's fine.
                    ProcessIfThisDocument(newSolution, document.Id);
                }
            }

            private void ProcessIfThisDocument(Solution newSolution, DocumentId documentId)
            {
                if (_workspace != null)
                {
                    var openDocumentId = _workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer());
                    if (openDocumentId == documentId)
                    {
                        EnqueueProcessSnapshot(newSolution.GetDocument(documentId));
                    }
                }
            }
        }
    }
}
