// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
{
    internal partial class ReferenceHighlightingTagSource : ProducerPopulatedTagSource<AbstractNavigatableReferenceHighlightingTag, object>
    {
        private const int VoidVersion = -1;

        private readonly ITextView _textView;

        // last solution version we used to update tags
        // * NOTE * here unfortunately, we only hold onto version without caret position since
        //          it is not easy to pass through it. for now, we will void the last version whenever
        //          we see caret changes.
        private int _lastUpdateTagsSolutionVersion = VoidVersion;

        public ReferenceHighlightingTagSource(
            ITextView textView,
            ITextBuffer subjectBuffer,
            ReferenceHighlightingViewTaggerProvider taggerProvider,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
            : base(textView, subjectBuffer, taggerProvider, asyncListener, notificationService)
        {
            _textView = textView;
        }

        protected override SnapshotPoint? GetCaretPoint()
        {
            return _textView.Caret.Position.Point.GetPoint(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType), PositionAffinity.Successor);
        }

        protected override void RecalculateTagsOnChangedCore(TaggerEventArgs e)
        {
            var cancellationToken = this.WorkQueue.CancellationToken;

            VoidLastSolutionVersionIfCaretChanged(e);

            RegisterNotification(() =>
            {
                this.WorkQueue.AssertIsForeground();

                var caret = GetCaretPoint();
                if (!caret.HasValue)
                {
                    ClearTags(cancellationToken);
                    return;
                }

                var spansToTag = TryGetSpansAndDocumentsToTag(e.Kind);
                if (spansToTag != null)
                {
                    // we will eagerly remove tags except for semantic change case.
                    // in semantic change case, we don't actually know whether it will affect highlight we currently
                    // have, so we will wait until we get new tags before removing them.
                    if (e.Kind != PredefinedChangedEventKinds.SemanticsChanged)
                    {
                        ClearTags(spansToTag, cancellationToken);
                    }

                    base.RecalculateTagsOnChangedCore(e);
                }
            }, delay: TaggerConstants.NearImmediateDelay, cancellationToken: cancellationToken);
        }

        private void VoidLastSolutionVersionIfCaretChanged(TaggerEventArgs e)
        {
            if (e.Kind == PredefinedChangedEventKinds.CaretPositionChanged)
            {
                _lastUpdateTagsSolutionVersion = VoidVersion;
            }
        }

        private void ClearTags(CancellationToken cancellationToken)
        {
            this.WorkQueue.AssertIsForeground();
            ClearTags(spansToTag: null, cancellationToken: cancellationToken);
        }

        private void ClearTags(List<DocumentSnapshotSpan> spansToTag, CancellationToken cancellationToken)
        {
            this.WorkQueue.AssertIsForeground();

            if (_textView == null)
            {
                // we are called by base constructor before we are properly setup
                return;
            }

            spansToTag = spansToTag ?? GetSpansAndDocumentsToTag();

            // Save to access CachedTagTrees here because we're on the foreground thread.
            var oldTagsTrees = this.CachedTagTrees;

            this.WorkQueue.EnqueueBackgroundTask(
                c => this.ClearTagsAsync(spansToTag, oldTagsTrees, c), "ClearTags", cancellationToken);
        }

        private List<DocumentSnapshotSpan> TryGetSpansAndDocumentsToTag(string kind)
        {
            this.WorkQueue.AssertIsForeground();

            // TODO: tagger creates so much temporary objects. GetSpansAndDocumentsToTags creates handful of objects per events 
            //       (in this case, on every caret move or text change). at some point of time, we should either re-write tagger framework
            //       or do some audit to reduce memory allocations.
            var spansToTag = GetSpansAndDocumentsToTag();

            if (kind == PredefinedChangedEventKinds.SemanticsChanged || kind == PredefinedChangedEventKinds.TextChanged)
            {
                // check whether we already processed highlight for this document
                // * this can happen if we are called twice for same document due to two different change events caused by
                //   same root change (text edit)
                var spanAndTag = spansToTag.First(s => s.SnapshotSpan.Snapshot.TextBuffer == this.SubjectBuffer);
                var version = spanAndTag.SnapshotSpan.Snapshot.Version.ReiteratedVersionNumber;
                var document = spanAndTag.Document;

                if (version == this.SubjectBuffer.CurrentSnapshot.Version.ReiteratedVersionNumber &&
                    document != null && document.Project.Solution.WorkspaceVersion == _lastUpdateTagsSolutionVersion)
                {
                    return null;
                }
            }

            // we are going to update tags, clear last update tags solution version
            _lastUpdateTagsSolutionVersion = VoidVersion;
            return spansToTag;
        }

        private Task ClearTagsAsync(
            List<DocumentSnapshotSpan> spansToTag, 
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<AbstractNavigatableReferenceHighlightingTag>> oldTagTrees,
            CancellationToken cancellationToken)
        {
            this.WorkQueue.AssertIsBackground();
            cancellationToken.ThrowIfCancellationRequested();

            var tagSpans = SpecializedCollections.EmptyEnumerable<ITagSpan<AbstractNavigatableReferenceHighlightingTag>>();

            var newTagTrees = ConvertToTagTree(oldTagTrees, tagSpans, spansToTag);

            // here we call base.ProcessNewTags so that we can clear tags without setting last solution version
            // clear tags is a special update mechanism where it represents clearing tags not updating tags.

            // we don't care about accumulated text change, so give it null
            base.ProcessNewTagTrees(spansToTag, oldTagTrees: oldTagTrees, newTagTrees: newTagTrees, newState: null, cancellationToken: cancellationToken);

            return SpecializedTasks.EmptyTask;
        }

        protected override void ProcessNewTagTrees(
            IEnumerable<DocumentSnapshotSpan> spansToCompute,
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<AbstractNavigatableReferenceHighlightingTag>> oldTagTrees,
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<AbstractNavigatableReferenceHighlightingTag>> newTags,
            object newState,
            CancellationToken cancellationToken)
        {
            base.ProcessNewTagTrees(spansToCompute, oldTagTrees, newTags, newState, cancellationToken);

            // remember last solution version we updated the tags
            var document = spansToCompute.First().Document;
            if (document != null)
            {
                _lastUpdateTagsSolutionVersion = document.Project.Solution.WorkspaceVersion;
            }
        }
    }
}
