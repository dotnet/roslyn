// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(HighlightTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class HighlighterViewTaggerProvider :
        ForegroundThreadAffinitizedObject,
        IViewTaggerProvider,
        IAsynchronousTaggerDataSource<HighlightTag>
    {
        private readonly IHighlightingService _highlightingService;
        private readonly Lazy<IViewTaggerProvider> _asynchronousTaggerProvider;

        public TaggerDelay? UIUpdateDelay => null;
        public IEqualityComparer<HighlightTag> TagComparer => null;
        public bool RemoveTagsThatIntersectEdits => true;
        public SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;
        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;
        public IEnumerable<Option<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.KeywordHighlight);
        public IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        [ImportingConstructor]
        public HighlighterViewTaggerProvider(
            IHighlightingService highlightingService,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _highlightingService = highlightingService;
            _asynchronousTaggerProvider = new Lazy<IViewTaggerProvider>(() =>
                new AsynchronousViewTaggerProviderWithTagSource<HighlightTag>(
                    this,
                    new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.KeywordHighlighting),
                    notificationService,
                    createTagSource: CreateTagSource));
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return _asynchronousTaggerProvider.Value.CreateTagger<T>(textView, buffer);
        }

        public ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle, reportChangedSpans: true),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.KeywordHighlighting, TaggerDelay.NearImmediate));
        }

        private ProducerPopulatedTagSource<HighlightTag> CreateTagSource(
            ITextView textViewOpt, ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener, IForegroundNotificationService notificationService)
        {
            return new HighlightingTagSource(textViewOpt, subjectBuffer, this, asyncListener, notificationService);
        }

        public IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return null;
        }

        public Task ProduceTagsAsync(IEnumerable<DocumentSnapshotSpan> snapshotSpans, SnapshotPoint? caretPosition, Action<ITagSpan<HighlightTag>> addTag, CancellationToken cancellationToken)
        {
            return TaggerUtilities.Delegate(
                snapshotSpans, caretPosition, addTag, ProduceTagsAsync, cancellationToken);
        }

        // Internal for testing purposes
        internal async Task ProduceTagsAsync(
            DocumentSnapshotSpan documentSnapshotSpan,
            int? caretPosition,
            Action<ITagSpan<HighlightTag>> addTag,
            CancellationToken cancellationToken)
        {
            var document = documentSnapshotSpan.Document;
            if (document == null)
            {
                return;
            }

            var options = document.Project.Solution.Workspace.Options;
            if (!options.GetOption(FeatureOnOffOptions.KeywordHighlighting, document.Project.Language))
            {
                return;
            }

            if (caretPosition.HasValue)
            {
                var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
                var position = caretPosition.Value;
                var snapshot = snapshotSpan.Snapshot;

                using (Logger.LogBlock(FunctionId.Tagger_Highlighter_TagProducer_ProduceTags, cancellationToken))
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    var spans = _highlightingService.GetHighlights(root, position, cancellationToken);
                    foreach (var span in spans)
                    {
                        addTag(new TagSpan<HighlightTag>(span.ToSnapshotSpan(snapshot), HighlightTag.Instance));
                    }
                }
            }
        }
    }
}