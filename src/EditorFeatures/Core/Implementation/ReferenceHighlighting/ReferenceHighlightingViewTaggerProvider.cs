// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(AbstractNavigatableReferenceHighlightingTag))]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal partial class ReferenceHighlightingViewTaggerProvider :
        ForegroundThreadAffinitizedObject,
        IViewTaggerProvider,
        IAsynchronousTaggerDataSource<AbstractNavigatableReferenceHighlightingTag>
    {
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;
        private readonly Lazy<IViewTaggerProvider> _asynchronousTaggerProvider;

        public bool RemoveTagsThatIntersectEdits => true;
        public TaggerDelay? UIUpdateDelay => TaggerDelay.NearImmediate;
        public SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;
        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;
        public IEqualityComparer<AbstractNavigatableReferenceHighlightingTag> TagComparer => null;
        public IEnumerable<Option<bool>> Options => null;
        public IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.ReferenceHighlighting);

        [ImportingConstructor]
        public ReferenceHighlightingViewTaggerProvider(
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _semanticChangeNotificationService = semanticChangeNotificationService;
            _asynchronousTaggerProvider = new Lazy<IViewTaggerProvider>(() =>
                new AsynchronousViewTaggerProviderWithTagSource<AbstractNavigatableReferenceHighlightingTag>(
                    this,
                    new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.ReferenceHighlighting),
                    notificationService,
                    this.CreateTagSource));
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return _asynchronousTaggerProvider.Value.CreateTagger<T>(textView, buffer);
        }

        public ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            // PERF: use a longer delay for OnTextChanged to minimize the impact of GCs while typing
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnCaretPositionChanged(textView, textView.TextBuffer, TaggerDelay.Short),
                TaggerEventSources.OnSemanticChanged(subjectBuffer, TaggerDelay.OnIdle, _semanticChangeNotificationService),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Short),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.ReferenceHighlighting, TaggerDelay.NearImmediate));
        }

        private ProducerPopulatedTagSource<AbstractNavigatableReferenceHighlightingTag> CreateTagSource(
            ITextView textViewOpt, ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            return new ReferenceHighlightingTagSource(textViewOpt, subjectBuffer, this, asyncListener, notificationService);
        }

        public IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return null;
        }

        public Task ProduceTagsAsync(
            IEnumerable<DocumentSnapshotSpan> snapshotSpans,
            SnapshotPoint? caretPosition, 
            Action<ITagSpan<AbstractNavigatableReferenceHighlightingTag>> addTag,
            CancellationToken cancellationToken)
        {
            // NOTE(cyrusn): Normally we'd limit ourselves to producing tags in the span we were
            // asked about.  However, we want to produce all tags here so that the user can actually
            // navigate between all of them using the appropriate tag navigation commands.  If we
            // don't generate all the tags then the user will cycle through an incorrect subset.
            if (caretPosition == null)
            {
                return SpecializedTasks.EmptyTask;
            }

            var position = caretPosition.Value;

            Workspace workspace;
            if (!Workspace.TryGetWorkspace(position.Snapshot.AsText().Container, out workspace))
            {
                return SpecializedTasks.EmptyTask;
            }

            var document = snapshotSpans.First(vt => vt.SnapshotSpan.Snapshot == position.Snapshot).Document;
            if (document == null)
            {
                return SpecializedTasks.EmptyTask;
            }

            return ProduceTagsAsync(snapshotSpans, position, workspace, document, addTag, cancellationToken);
        }

        internal async Task ProduceTagsAsync(
            IEnumerable<DocumentSnapshotSpan> snapshotSpans,
            SnapshotPoint position,
            Workspace workspace,
            Document document,
            Action<ITagSpan<AbstractNavigatableReferenceHighlightingTag>> addTag,
            CancellationToken cancellationToken)
        {
            // Don't produce tags if the feature is not enabled.
            if (!workspace.Options.GetOption(FeatureOnOffOptions.ReferenceHighlighting, document.Project.Language))
            {
                return;
            }

            var solution = document.Project.Solution;

            using (Logger.LogBlock(FunctionId.Tagger_ReferenceHighlighting_TagProducer_ProduceTags, cancellationToken))
            {
                var result = new List<ITagSpan<AbstractNavigatableReferenceHighlightingTag>>();

                if (document != null)
                {
                    var documentHighlightsService = document.Project.LanguageServices.GetService<IDocumentHighlightsService>();
                    if (documentHighlightsService != null)
                    {
                        // We only want to search inside documents that correspond to the snapshots
                        // we're looking at
                        var documentsToSearch = ImmutableHashSet.CreateRange(snapshotSpans.Select(vt => vt.Document).WhereNotNull());
                        var documentHighlightsList = await documentHighlightsService.GetDocumentHighlightsAsync(document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);
                        if (documentHighlightsList != null)
                        {
                            foreach (var documentHighlights in documentHighlightsList)
                            {
                                await AddTagSpansAsync(solution, result, documentHighlights, addTag, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }

        private async Task AddTagSpansAsync(
            Solution solution,
            List<ITagSpan<AbstractNavigatableReferenceHighlightingTag>> tags,
            DocumentHighlights documentHighlights,
            Action<ITagSpan<AbstractNavigatableReferenceHighlightingTag>> addTag,
            CancellationToken cancellationToken)
        {
            var document = documentHighlights.Document;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
            if (textSnapshot == null)
            {
                // There is no longer an editor snapshot for this document, so we can't care about the
                // results.
                return;
            }

            foreach (var span in documentHighlights.HighlightSpans)
            {
                var tag = GetTag(span);
                addTag(new TagSpan<AbstractNavigatableReferenceHighlightingTag>(
                    textSnapshot.GetSpan(Span.FromBounds(span.TextSpan.Start, span.TextSpan.End)), tag));
            }
        }

        private static AbstractNavigatableReferenceHighlightingTag GetTag(HighlightSpan span)
        {
            switch (span.Kind)
            {
                case HighlightSpanKind.WrittenReference:
                    return WrittenReferenceHighlightTag.Instance;

                case HighlightSpanKind.Definition:
                    return DefinitionHighlightTag.Instance;

                case HighlightSpanKind.Reference:
                case HighlightSpanKind.None:
                default:
                    return ReferenceHighlightTag.Instance;
            }
        }

    }
}