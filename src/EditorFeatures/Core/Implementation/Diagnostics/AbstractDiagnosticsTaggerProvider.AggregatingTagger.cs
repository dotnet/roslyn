using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
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

            private int refCount;
            private bool _disposed;

            private readonly Dictionary<object, ValueTuple<TaggerProvider, IAccurateTagger<TTag>>> _idToProviderAndTagger = new Dictionary<object, ValueTuple<TaggerProvider, IAccurateTagger<TTag>>>();

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public AggregatingTagger(
                AbstractDiagnosticsTaggerProvider<TTag> owner,
                ITextBuffer subjectBuffer)
            {
                _owner = owner;
                _subjectBuffer = subjectBuffer;

                // Register to hear about diagnostics changing.  When we're notified about new
                // diagnostics (and those diagnostics are for our buffer), we'll ensure that
                // we have an underlying tagger responsible for asynchrounously handling diagnostics
                // from the owner of that diagnostic update.
                _owner._diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
            }

            public void OnTaggerCreated()
            {
                this.AssertIsForeground();
                Debug.Assert(refCount >= 0);
                Debug.Assert(!_disposed);

                refCount++;
            }

            public void Dispose()
            {
                this.AssertIsForeground();
                Debug.Assert(refCount > 0);
                Debug.Assert(!_disposed);

                refCount--;

                if (refCount == 0)
                {
                    _disposed = true;

                    // Stop listening to diagnostic changes from the diagnostic service.
                    _owner._diagnosticService.DiagnosticsUpdated -= OnDiagnosticsUpdated;

                    // Disconnect us from our underlying taggers and make sure they're
                    // released as well.
                    foreach (var kvp in _idToProviderAndTagger)
                    {
                        var tagger = kvp.Value.Item2;

                        tagger.TagsChanged -= OnUnderlyingTaggerTagsChanged;
                        var disposable = tagger as IDisposable;
                        if (disposable != null)
                        {
                            disposable.Dispose();
                        }
                    }

                    _idToProviderAndTagger.Clear();
                    _owner.RemoveTagger(this, _subjectBuffer);
                }
            }

            private void RegisterNotification(Action action)
            {
                var token = _owner._listener.BeginAsyncOperation(GetType().Name + "RegisterNotification");
                _owner._notificationService.RegisterNotification(action, token);
            }

            private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
            {
                // Do some quick checks to avoid doing any further work for diagnostics  we don't
                // care about.
                var ourDocument = _subjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                if (ourDocument == null ||
                    ourDocument.Project.Solution.Workspace != e.Workspace ||
                    ourDocument.Id != e.DocumentId)
                {
                    return;
                }

                // Make sure we can find an editor snapshot for these errors.  Otherwise we won't
                // be able to make ITagSpans for them.  If we can't, just bail out.  This happens
                // when the solution crawler is very far behind.  However, it will have a more
                // up to date document within it that it will eventually process.  Until then
                // we just keep around the stale tags we have.
                //
                // Note: if the Solution or Document is null here, then that means the document
                // was removed.  In that case, we do want to proceed so that we'll produce 0
                // tags and we'll update the editor appropriately.
                SourceText sourceText = null;
                ITextSnapshot editorSnapshot = null;
                if (e.Solution != null)
                {
                    var diagnosticDocument = e.Solution.GetDocument(e.DocumentId);
                    if (diagnosticDocument != null)
                    {
                        if (!diagnosticDocument.TryGetText(out sourceText))
                        {
                            return;
                        }

                        editorSnapshot = sourceText.FindCorrespondingEditorTextSnapshot();
                        if (editorSnapshot == null)
                        {
                            return;
                        }
                    }
                }

                if (this.IsForeground())
                {
                    OnDiagnosticsUpdatedOnForeground(sender, e, sourceText, editorSnapshot);
                }
                else
                {
                    RegisterNotification(() => OnDiagnosticsUpdatedOnForeground(sender, e, sourceText, editorSnapshot));
                }
            }

            private void OnDiagnosticsUpdatedOnForeground(
                object sender, DiagnosticsUpdatedArgs e, SourceText sourceText, ITextSnapshot editorSnapshot)
            {
                this.AssertIsForeground();
                if (_disposed)
                {
                    return;
                }

                // Find the appropriate async tagger for this diagnostics id, and let it know that
                // there were new diagnostics produced for it.
                var id = e.Id;
                ValueTuple<TaggerProvider, IAccurateTagger<TTag>> providerAndTagger;
                if (!_idToProviderAndTagger.TryGetValue(id, out providerAndTagger))
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
                    providerAndTagger = ValueTuple.Create(taggerProvider, tagger);

                    _idToProviderAndTagger[id] = providerAndTagger;

                    // Register for changes from the underlying tagger.  When it tells us about
                    // changes, we'll let anyone know who is registered with us.
                    tagger.TagsChanged += OnUnderlyingTaggerTagsChanged;
                }

                // Let the provier know that there are new diagnostics.  It will then
                // handle all the async processing of those diagnostics.
                providerAndTagger.Item1.OnDiagnosticsUpdated(e, sourceText, editorSnapshot);
            }

            private void OnUnderlyingTaggerTagsChanged(object sender, SnapshotSpanEventArgs args)
            {
                this.AssertIsForeground();
                if (_disposed)
                {
                    return;
                }

                var tagsChanged = this.TagsChanged;
                if (tagsChanged != null)
                {
                    tagsChanged(sender, args);
                }
            }

            public IEnumerable<ITagSpan<TTag>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancel)
            {
                this.AssertIsForeground();
                return _idToProviderAndTagger.Values.SelectMany(t => t.Item2.GetAllTags(spans, cancel)).ToList();
            }

            public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                this.AssertIsForeground();
                return _idToProviderAndTagger.Values.SelectMany(t => t.Item2.GetTags(spans)).ToList();
            }
        }
    }
}