// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
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

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(BraceHighlightTag))]
    internal class BraceHighlightingViewTaggerProvider : AsynchronousViewTaggerProvider<BraceHighlightTag>
    {
        private readonly IBraceMatchingService _braceMatcherService;

        protected override IEnumerable<Option<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.BraceMatching);

        [ImportingConstructor]
        public BraceHighlightingViewTaggerProvider(
            IBraceMatchingService braceMatcherService,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
                : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.BraceHighlighting), notificationService)
        {
            _braceMatcherService = braceMatcherService;
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnParseOptionChanged(subjectBuffer, TaggerDelay.NearImmediate));
        }

        protected override Task ProduceTagsAsync(TaggerContext<BraceHighlightTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var document = documentSnapshotSpan.Document;
            if (!caretPosition.HasValue || document == null)
            {
                return SpecializedTasks.EmptyTask;
            }

            return ProduceTagsAsync(context, document, documentSnapshotSpan.SnapshotSpan.Snapshot, caretPosition.Value);
        }

        internal async Task ProduceTagsAsync(TaggerContext<BraceHighlightTag> context, Document document, ITextSnapshot snapshot, int position)
        {
            using (Logger.LogBlock(FunctionId.Tagger_BraceHighlighting_TagProducer_ProduceTags, context.CancellationToken))
            {
                await ProduceTagsForBracesAsync(context, document, snapshot, position, rightBrace: false).ConfigureAwait(false);
                await ProduceTagsForBracesAsync(context, document, snapshot, position - 1, rightBrace: true).ConfigureAwait(false);
            }
        }

        private async Task ProduceTagsForBracesAsync(TaggerContext<BraceHighlightTag> context, Document document, ITextSnapshot snapshot, int position, bool rightBrace)
        {
            if (position >= 0 && position <= snapshot.Length)
            {
                var braces = await _braceMatcherService.GetMatchingBracesAsync(document, position, context.CancellationToken).ConfigureAwait(false);
                if (braces.HasValue)
                {
                    if ((!rightBrace && braces.Value.LeftSpan.Start == position) ||
                        (rightBrace && braces.Value.RightSpan.Start == position))
                    {
                        context.AddTag(snapshot.GetTagSpan(braces.Value.LeftSpan.ToSpan(), BraceHighlightTag.StartTag));
                        context.AddTag(snapshot.GetTagSpan(braces.Value.RightSpan.ToSpan(), BraceHighlightTag.EndTag));
                    }
                }
            }
        }
    }
}