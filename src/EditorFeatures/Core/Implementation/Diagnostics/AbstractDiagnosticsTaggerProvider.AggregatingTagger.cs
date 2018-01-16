// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract partial class AbstractDiagnosticsTaggerProvider<TTag>
    {
        private class AggregatingTagger : ForegroundThreadAffinitizedObject, IAccurateTagger<TTag>, IDisposable
        {
            private readonly AbstractDiagnosticsTaggerProvider<TTag> _owner;
            private readonly ITextBuffer _subjectBuffer;
            private readonly WorkspaceRegistration _workspaceRegistration;
            private readonly CancellationTokenSource _initialDiagnosticsCancellationSource = new CancellationTokenSource();

            private int _refCount;
            private bool _disposed;

            private readonly Dictionary<object, (TaggerProvider provider, IAccurateTagger<TTag> tagger)> _idToProviderAndTagger = new Dictionary<object, (TaggerProvider provider, IAccurateTagger<TTag> tagger)>();

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            // Data that is used from both threads and needs to be protected by _gate.
            private readonly object _gate = new object();

            // Use a chain of tasks to make sure that we process each diagnostic event serially.
            // This also ensures that the first diagnostic event we hear about will be processed
            // after the initial background work to get the first group of diagnostics.
            private Task _taskChain;

            /// <summary>
            /// The current Document and Workspace that our <see cref="_subjectBuffer"/> is 
            /// associated with.  If our buffer becomes associated with another document or
            /// workspace, we will clear out any cached diagnostic information we've collected 
            /// so far as they're no longer valid.
            /// 
            /// Note: we fundamentally have a race condition here.  While we will update this
            /// whenever our ITextBuffer changes which document it is associated with, there
            /// is no guarantee that we will hear about the diagnostics from that document
            /// afterwards.  i.e. we may end up with the following chain of events:
            /// 
            /// 1. Text buffer switches document association.
            /// 2. We hear about diagnostics from that new document (and we ignore them)
            /// 3. We hear about hte association change.
            /// 
            /// This is a problem no matter which thread we process diagnostics on.  The only
            /// way to prevent this would be to have to listen and remember about all diagnostics
            /// for some amount of time, or to have the diagnostic service and workspace coordinate
            /// to ensure that diagnostic events always happened after workspace eveents.
            /// </summary>
            private DocumentId _currentDocumentId;
            private Workspace _workspace;

            /// <summary>
            /// We may get a flood of diagnostic notificaitons on the BG.  In order to not overwhelm
            /// the UI thread with work to do, and to avoid excess allocations, we batch up these
            /// notifications, and attempt to process them all at once on the UI thread every 50ms
            /// or so.
            /// 
            /// This means less notification allocations, and less UI timeslices
            /// needed.  This does mean we may spend a little more time on the UI processing all
            /// those notifications.  However, that time should still be very brief as we do almost
            /// nothing on the UI itself (we just push the data into our providers, and inform the
            /// editor that we have changed).
            /// </summary>
            private Dictionary<object, DiagnosticsUpdatedArgs> idToLatestArgs = s_providerPool.Allocate();

            /// <summary>
            /// The task we use to actuall process all the diagnostic notifications we've gotten.
            /// We only ever have one of these in flight at a time.  If the task is in flight and
            /// we get new diagnostic events, we just add them to <see cref="idToLatestArgs"/>
            /// to be processed when the task finally executes.
            /// </summary>
            private Task _updateTask = null;

            private static readonly ObjectPool<Dictionary<object, DiagnosticsUpdatedArgs>> s_providerPool = new ObjectPool<Dictionary<object, DiagnosticsUpdatedArgs>>(
                () => new Dictionary<object, DiagnosticsUpdatedArgs>());

            public AggregatingTagger(
                AbstractDiagnosticsTaggerProvider<TTag> owner,
                ITextBuffer subjectBuffer)
            {
                _owner = owner;
                _subjectBuffer = subjectBuffer;

                var document = _subjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                _currentDocumentId = document?.Id;

                // Listen for whenever this buffer gets associated with a different workspace/document.
                _workspaceRegistration = Workspace.GetWorkspaceRegistration(_subjectBuffer.AsTextContainer());
                _workspaceRegistration.WorkspaceChanged += OnWorkspaceChanged;
                _workspace = document?.Project.Solution.Workspace;

                if (_workspace != null)
                {
                    _workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;
                }

                // Kick off a background task to collect the initial set of diagnostics.
                var cancellationToken = _initialDiagnosticsCancellationSource.Token;
                var asyncToken = _owner._listener.BeginAsyncOperation(GetType() + ".GetInitialDiagnostics");
                var task = Task.Run(() => GetInitialDiagnosticsInBackground(document, cancellationToken), cancellationToken);
                task.CompletesAsyncOperation(asyncToken);

                _taskChain = task;

                // Register to hear about diagnostics changing.  When we're notified about new
                // diagnostics (and those diagnostics are for our buffer), we'll ensure that
                // we have an underlying tagger responsible for asynchronously handling diagnostics
                // from the owner of that diagnostic update.
                _owner._diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
            }

            private void OnWorkspaceChanged(object sender, EventArgs e)
            {
                this.AssertIsForeground();

                // Disconnect from the old workspace notifications and hook up to the new ones.
                lock (_gate)
                {
                    if (_workspace != null)
                    {
                        _workspace.DocumentActiveContextChanged -= OnDocumentActiveContextChanged;
                    }

                    _workspace = _workspaceRegistration.Workspace;
                    if (_workspace != null)
                    {
                        _workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;
                    }
                }

                // We started tracking another document.  Clear out everything we've stored up so far.
                ResetCurrentDocumentIdIfNecessary();
            }

            private void OnDocumentActiveContextChanged(object sender, DocumentActiveContextChangedEventArgs e)
            {
                this.AssertIsForeground();
                this.ResetCurrentDocumentIdIfNecessary();
            }

            private void ResetCurrentDocumentIdIfNecessary()
            {
                this.AssertIsForeground();

                var document = _subjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();

                // Safe to read _currentDocumentId here, we are the fg thread.
                if (document?.Id == _currentDocumentId &&
                    document?.Project.Solution.Workspace == _workspace)
                {
                    // Nothing changed.
                    return;
                }

                lock (_gate)
                {
                    // Ensure the bg thread sees writes to this field.
                    _currentDocumentId = document?.Id;
                }

                // We started tracking another document.  Clear out everything we've stored up so far.
                RemoveAllCachedDiagnostics();
            }

            private void GetInitialDiagnosticsInBackground(
                Document document, CancellationToken cancellationToken)
            {
                this.AssertIsBackground();
                cancellationToken.ThrowIfCancellationRequested();

                if (document != null)
                {
                    var project = document.Project;
                    var workspace = project.Solution.Workspace;
                    foreach (var updateArgs in _owner._diagnosticService.GetDiagnosticsUpdatedEventArgs(workspace, project.Id, document.Id, cancellationToken))
                    {
                        var diagnostics = AdjustInitialDiagnostics(project.Solution, updateArgs, cancellationToken);
                        if (diagnostics.Length == 0)
                        {
                            continue;
                        }

                        OnDiagnosticsUpdatedOnBackground(
                            DiagnosticsUpdatedArgs.DiagnosticsCreated(
                                updateArgs.Id, updateArgs.Workspace, project.Solution, updateArgs.ProjectId, updateArgs.DocumentId, diagnostics));
                    }
                }
            }

            private ImmutableArray<DiagnosticData> AdjustInitialDiagnostics(
                Solution solution, UpdatedEventArgs args, CancellationToken cancellationToken)
            {
                this.AssertIsBackground();

                // we only reach here if there is the document
                var document = solution.GetDocument(args.DocumentId);
                Contract.ThrowIfNull(document);
                // if there is no source text for this document, we don't populate the initial tags. this behavior is equivalent of existing
                // behavior in OnDiagnosticsUpdated.
                if (!document.TryGetText(out var text))
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                // GetDiagnostics returns whatever cached diagnostics in the service which can be stale ones. for example, build error will be most likely stale
                // diagnostics. so here we make sure we filter out any diagnostics that is not in the text range.
                var builder = ArrayBuilder<DiagnosticData>.GetInstance();
                var fullSpan = new TextSpan(0, text.Length);
                foreach (var diagnostic in _owner._diagnosticService.GetDiagnostics(
                    args.Workspace, args.ProjectId, args.DocumentId, args.Id, includeSuppressedDiagnostics: false, cancellationToken: cancellationToken))
                {
                    if (fullSpan.Contains(diagnostic.GetExistingOrCalculatedTextSpan(text)))
                    {
                        builder.Add(diagnostic);
                    }
                }

                return builder.ToImmutableAndFree();
            }

            public void OnTaggerCreated()
            {
                this.AssertIsForeground();
                Debug.Assert(_refCount >= 0);
                Debug.Assert(!_disposed);

                _refCount++;
            }

            public void Dispose()
            {
                this.AssertIsForeground();
                Debug.Assert(_refCount > 0);
                Debug.Assert(!_disposed);

                _refCount--;

                if (_refCount == 0)
                {
                    _disposed = true;

                    if (_workspace != null)
                    {
                        _workspace.DocumentActiveContextChanged -= OnDocumentActiveContextChanged;
                    }

                    _workspaceRegistration.WorkspaceChanged -= OnWorkspaceChanged;

                    // Stop listening to diagnostic changes from the diagnostic service.
                    _owner._diagnosticService.DiagnosticsUpdated -= OnDiagnosticsUpdated;
                    _initialDiagnosticsCancellationSource.Cancel();

                    // Disconnect us from our underlying taggers and make sure they're
                    // released as well.
                    DisconnectFromAllTaggers();
                    _owner.RemoveTagger(this, _subjectBuffer);
                }
            }

            private void DisconnectFromTagger(IAccurateTagger<TTag> tagger)
            {
                this.AssertIsForeground();

                tagger.TagsChanged -= OnUnderlyingTaggerTagsChanged;
                if (tagger is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            private void DisconnectFromAllTaggers()
            {
                this.AssertIsForeground();

                foreach (var kvp in _idToProviderAndTagger)
                {
                    var tagger = kvp.Value.tagger;

                    DisconnectFromTagger(tagger);
                }

                _idToProviderAndTagger.Clear();
            }

            private void RegisterNotification(Action action)
            {
                var token = _owner._listener.BeginAsyncOperation(GetType().Name + "RegisterNotification");
                _owner._notificationService.RegisterNotification(action, token);
            }

            private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
            {
                lock (_gate)
                {
                    // Chain the events so we process them serially.  This also ensures
                    // that we don't process events while still getting our initial set
                    // of diagnostics.
                    var asyncToken = _owner._listener.BeginAsyncOperation(GetType() + ".OnDiagnosticsUpdated");
                    _taskChain = _taskChain.SafeContinueWith(
                        _ => OnDiagnosticsUpdatedOnBackground(e), TaskScheduler.Default);
                    _taskChain.CompletesAsyncOperation(asyncToken);
                }
            }

            private void OnDiagnosticsUpdatedOnBackground(DiagnosticsUpdatedArgs e)
            {
                this.AssertIsBackground();
                if (_disposed)
                {
                    return;
                }

                if (e.DocumentId == null)
                {
                    // Not a diagnostic event for a document.  Not something we can handle.
                    return;
                }

                lock (_gate)
                {
                    // On the bg, have to read _currentDocumentId in a lock to ensure we see writes
                    // to it.
                    // 
                    // Note: this approach is still is fundamentally racey.  We may be processing 
                    // the diagnostic events for a document that *currently* doesn't match our text 
                    // buffer, but which may *once* we hear about the active context change.  There 
                    // is no guarantee that we hear aobut diagnostic events after the workspace events.
                    if (e.DocumentId != _currentDocumentId)
                    {
                        // Not a notification for the document we're currently tracking.  Ignore this.
                        return;
                    }

                    idToLatestArgs[e.Id] = e;

                    // Check if there's already an in-flight update task.  If so, there's nothing we
                    // need to do.  The in-flight task will pick up the work we enqueued.  Otherwise,
                    // create a task to process this work (and anything else that comes in) for 50ms
                    // from now.
                    if (_updateTask == null)
                    {
                        var token = _owner._listener.BeginAsyncOperation(GetType().Name + "OnDiagnosticsUpdatedOnBackground");

                        _updateTask = Task.Delay(50).ContinueWith(
                            _ => OnDiagnosticsUpdatedOnForeground(),
                            this.ForegroundTaskScheduler).CompletesAsyncOperation(token);
                    }
                }
            }

            /// <summary>
            /// We do all mutation work on the UI thread.  That ensures that all mutation
            /// is processed serially *and* it means we can safely examine things like
            /// subject buffers and open documents without any threat of race conditions.
            /// Note that we do not do any expensive work here.  We just store (or remove)
            /// the data we were given in appropriate buckets.
            /// 
            /// For example, we create a TaggerProvider per unique DiagnosticUpdatedArgs.Id
            /// we get.  So if we see a new id we simply create a tagger for it and pass it
            /// these args to store.  Otherwise we pass these args to the existing tagger.
            /// 
            /// Similarly, clearing out data is just a matter of us clearing our reference
            /// to the data.  
            /// </summary>
            private void OnDiagnosticsUpdatedOnForeground()
            {
                this.AssertIsForeground();

                Dictionary<object, DiagnosticsUpdatedArgs> latestArgs;
                lock (_gate)
                {
                    Debug.Assert(_updateTask != null);

                    // Grab the work we need to do.  Create a fresh dictionary for new work to be
                    // put into.
                    latestArgs = idToLatestArgs;
                    idToLatestArgs = s_providerPool.Allocate();

                    // Clear out the task so that any new work that comes in will cause a new update
                    // task to be created.
                    _updateTask = null;
                }

                try
                {
                    if (_disposed)
                    {
                        return;
                    }

                    // Safe to access _currentDocumentId here.  We are the fg thread.
                    var ourDocument = _workspace.CurrentSolution.GetDocument(_currentDocumentId);
                    if (ourDocument == null)
                    {
                        // We're no longer associated with a workspace document.  Don't show any
                        // diagnostic tags in this buffer.
                        return;
                    }

                    foreach (var kvp in latestArgs)
                    {
                        var updateArgs = kvp.Value;
                        Debug.Assert(kvp.Key == updateArgs.Id);

                        if (updateArgs.DocumentId != ourDocument.Id ||
                            updateArgs.Workspace != ourDocument.Project.Solution.Workspace)
                        {
                            // Notification for some other document.  Just ignore it. This can happen
                            // if the active document we were tracking changed between when we got
                            // the diagnostic notification on the BG and now.
                            continue;
                        }

                        if (updateArgs.Kind == DiagnosticsUpdatedKind.DiagnosticsRemoved)
                        {
                            OnDiagnosticsRemovedOnForeground(updateArgs);
                        }
                        else
                        {
                            OnDiagnosticsCreatedOnForeground(updateArgs);
                        }
                    }
                }
                finally
                {
                    s_providerPool.ClearAndFree(latestArgs);
                }
            }

            private void OnDiagnosticsCreatedOnForeground(DiagnosticsUpdatedArgs e)
            {
                this.AssertIsForeground();

                Debug.Assert(!_disposed);
                Debug.Assert(e.Kind == DiagnosticsUpdatedKind.DiagnosticsCreated);

                var diagnosticDocument = e.Solution.GetDocument(e.DocumentId);

                // We're hearing about diagnostics for our document.  We may be hearing
                // about new diagnostics coming, or existing diagnostics being cleared
                // out.

                // Make sure we can find an editor snapshot for these errors.  Otherwise we won't
                // be able to make ITagSpans for them.  If we can't, just bail out.  This happens
                // when the solution crawler is very far behind.  However, it will have a more
                // up to date document within it that it will eventually process.  Until then
                // we just keep around the stale tags we have.
                //
                // Note: if the Solution or Document is null here, then that means the document
                // was removed.  In that case, we do want to proceed so that we'll produce 0
                // tags and we'll update the editor appropriately.
                        if (!diagnosticDocument.TryGetText(out var sourceText))
                        {
                            return;
                        }

                        var editorSnapshot = sourceText.FindCorrespondingEditorTextSnapshot();
                        if (editorSnapshot == null)
                        {
                            return;
                        }

                        // Make sure the editor we've got associated with these diagnostics is the 
                        // same one we're a tagger for.  It is possible for us to hear about diagnostics
                        // for the *same* Document that are not from the *same* buffer.  For example,
                        // say we have the following chain of events:
                        //
                        //      Document is opened.
                        //      Diagnostics start analyzing.
                        //      Document is closed.
                        //      Document is opened.
                        //      Diagnostics finish and report for document.
                        //
                        // We'll hear about diagnostics for the original Document/Buffer that was 
                        // opened.  But we'll be trying to apply them to this current Document/Buffer.
                        // That won't work since these will be different buffers (and thus, we won't
                        // be able to map the diagnostic spans appropriately).  
                        //
                        // Note: returning here is safe.  Because the file is closed/opened, The 
                        // Diagnostics Service will reanalyze it.  It will then report the new results
                        // which we will hear about and use.
                        if (editorSnapshot.TextBuffer != _subjectBuffer)
                        {
                            return;
                        }

                OnDiagnosticsUpdatedOnForeground(e, sourceText, editorSnapshot);
            }

            private void OnDiagnosticsRemovedOnForeground(DiagnosticsUpdatedArgs e)
            {
                this.AssertIsForeground();

                Debug.Assert(!_disposed);

                // This is a document removal.  Clear out any state we have associated with 
                // this analyzer. 
                var id = e.Id;
                if (!_idToProviderAndTagger.TryGetValue(id, out var providerAndTagger))
                {
                    // Wasn't a diagnostic source we care about.
                    return;
                }

                _idToProviderAndTagger.Remove(id);
                DisconnectFromTagger(providerAndTagger.tagger);

                OnUnderlyingTaggerTagsChanged(this, new SnapshotSpanEventArgs(_subjectBuffer.CurrentSnapshot.GetFullSpan()));
            }

            private void RemoveAllCachedDiagnostics()
            {
                this.AssertIsForeground();

                DisconnectFromAllTaggers();
                OnUnderlyingTaggerTagsChanged(this, new SnapshotSpanEventArgs(_subjectBuffer.CurrentSnapshot.GetFullSpan()));
            }

            private void OnDiagnosticsUpdatedOnForeground(
                DiagnosticsUpdatedArgs e, SourceText sourceText, ITextSnapshot editorSnapshot)
            {
                this.AssertIsForeground();
                Debug.Assert(!_disposed);

                // Find the appropriate async tagger for this diagnostics id, and let it know that
                // there were new diagnostics produced for it.
                var id = e.Id;
                if (!_idToProviderAndTagger.TryGetValue(id, out var providerAndTagger))
                {
                    // We didn't have an existing tagger for this diagnostic id.  If there are no actual 
                    // diagnostics being reported, then don't bother actually doing anything.  This saves
                    // us from creating a lot of objects, and subscribing to tons of events that we don't
                    // actually need (since we don't even have any diagnostics to show!).
                    if (e.Diagnostics.Length == 0)
                    {
                        return;
                    }

                    // Didn't have an existing tagger for this diagnostic id.  Make a new one
                    // and cache it so we can use it in the future.
                    var taggerProvider = new TaggerProvider(_owner);
                    var tagger = taggerProvider.CreateTagger<TTag>(_subjectBuffer);
                    providerAndTagger = (taggerProvider, tagger);

                    _idToProviderAndTagger[id] = providerAndTagger;

                    // Register for changes from the underlying tagger.  When it tells us about
                    // changes, we'll let anyone know who is registered with us.
                    tagger.TagsChanged += OnUnderlyingTaggerTagsChanged;
                }

                // Let the provider know that there are new diagnostics.  It will then
                // handle all the async processing of those diagnostics.
                providerAndTagger.provider.OnDiagnosticsUpdated(e, sourceText, editorSnapshot);
            }

            private void OnUnderlyingTaggerTagsChanged(object sender, SnapshotSpanEventArgs args)
            {
                this.AssertIsForeground();
                if (_disposed)
                {
                    return;
                }

                this.TagsChanged?.Invoke(sender, args);
            }

            public IEnumerable<ITagSpan<TTag>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancel)
            {
                this.AssertIsForeground();
                return _idToProviderAndTagger.Values.SelectMany(t => t.tagger.GetAllTags(spans, cancel)).ToList();
            }

            public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                this.AssertIsForeground();
                return _idToProviderAndTagger.Values.SelectMany(t => t.tagger.GetTags(spans)).ToList();
            }
        }
    }
}
