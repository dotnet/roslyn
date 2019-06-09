// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            IThreadingContext threadingContext,
            IBraceMatchingService braceMatcherService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
                : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.BraceHighlighting), notificationService)
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
                return Task.CompletedTask;
            }

            return ProduceTagsAsync(context, document, documentSnapshotSpan.SnapshotSpan.Snapshot, caretPosition.Value);
        }

        internal async Task ProduceTagsAsync(TaggerContext<BraceHighlightTag> context, Document document, ITextSnapshot snapshot, int position)
        {
            using (Logger.LogBlock(FunctionId.Tagger_BraceHighlighting_TagProducer_ProduceTags, context.CancellationToken))
            {
                if (position >= 0 && position <= snapshot.Length)
                {
                    var (bracesLeftOfPosition, bracesRightOfPosition) = await GetAllMatchingBracesAsync(
                        _braceMatcherService, document, position, context.CancellationToken).ConfigureAwait(false);

                    AddBraces(context, snapshot, bracesLeftOfPosition);
                    AddBraces(context, snapshot, bracesRightOfPosition);
                }
            }
        }

        /// <summary>
        /// Given code like   ()^()  (where ^ is the caret position), returns the two pairs of
        /// matching braces on the left and the right of the position.  Note: a brace matching
        /// pair is only returned if the position is on the left-side of hte start brace, or the
        /// right side of end brace.  So, for example, if you have (^()), then only the inner 
        /// braces are returned as the position is not on the right-side of the outer braces.
        /// 
        /// This function also works for multi-character braces i.e.  ([  ])   In this case,
        /// the rule is that the position has to be on the left side of the start brace, or 
        /// inside the start brace (but not at the end).  So,    ^([   ])  will return this
        /// as a brace match, as will  (^[    ]).  But   ([^   ])  will not.
        /// 
        /// The same goes for the braces on the the left of the caret.  i.e.:   ([   ])^
        /// will return the braces on the left, as will   ([   ]^).  But   ([   ^]) will not.
        /// </summary>
        private static async Task<(BraceMatchingResult? leftOfPosition, BraceMatchingResult? rightOfPosition)> GetAllMatchingBracesAsync(
            IBraceMatchingService service,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            // These are the matching spans when checking the token to the right of the position.
            var rightOfPosition = await service.GetMatchingBracesAsync(document, position, cancellationToken).ConfigureAwait(false);

            // The braces to the right of the position should only be added if the position is 
            // actually within the span of the start brace.  Note that this is what we want for
            // single character braces as well as multi char braces.  i.e. if the user has:
            //
            //      ^{ }    // then { and } are matching braces.
            //      {^ }    // then { and } are not matching braces.
            //
            //      ^<@ @>  // then <@ and @> are matching braces.
            //      <^@ @>  // then <@ and @> are matching braces.
            //      <@^ @>  // then <@ and @> are not matching braces.
            if (rightOfPosition.HasValue &&
                !rightOfPosition.Value.LeftSpan.Contains(position))
            {
                // Not a valid match.  
                rightOfPosition = null;
            }

            if (position == 0)
            {
                // We're at the start of the document, can't find braces to the left of the position.
                return (leftOfPosition: null, rightOfPosition);
            }

            // See if we're touching the end of some construct.  i.e.:
            //
            //      { }^
            //      <@ @>^
            //      <@ @^>
            //
            // But not
            //
            //      { ^}
            //      <@ ^@>

            var leftOfPosition = await service.GetMatchingBracesAsync(document, position - 1, cancellationToken).ConfigureAwait(false);

            if (leftOfPosition.HasValue &&
                position <= leftOfPosition.Value.RightSpan.End &&
                position > leftOfPosition.Value.RightSpan.Start)
            {
                // Found a valid pair on the left of us.
                return (leftOfPosition, rightOfPosition);
            }

            // No valid pair of braces on the left of us.
            return (leftOfPosition: null, rightOfPosition);
        }

        private void AddBraces(
            TaggerContext<BraceHighlightTag> context,
            ITextSnapshot snapshot,
            BraceMatchingResult? braces)
        {
            if (braces.HasValue)
            {
                context.AddTag(snapshot.GetTagSpan(braces.Value.LeftSpan.ToSpan(), BraceHighlightTag.StartTag));
                context.AddTag(snapshot.GetTagSpan(braces.Value.RightSpan.ToSpan(), BraceHighlightTag.EndTag));
            }
        }
    }
}
