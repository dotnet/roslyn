// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    internal partial class BraceHighlightingTagProducer :
        AbstractSingleDocumentTagProducer<BraceHighlightTag>
    {
        private static readonly IEnumerable<ITagSpan<BraceHighlightTag>> s_noTags
            = SpecializedCollections.EmptyEnumerable<ITagSpan<BraceHighlightTag>>();

        private readonly IBraceMatchingService _braceMatcherService;

        public BraceHighlightingTagProducer(
            IBraceMatchingService braceMatcherService)
        {
            _braceMatcherService = braceMatcherService;
        }

        public override Task<IEnumerable<ITagSpan<BraceHighlightTag>>> ProduceTagsAsync(
            Document document, SnapshotSpan snapshotSpan, int? caretPosition, CancellationToken cancellationToken)
        {
            var snapshot = snapshotSpan.Snapshot;
            if (!caretPosition.HasValue || document == null)
            {
                return SpecializedTasks.EmptyEnumerable<ITagSpan<BraceHighlightTag>>();
            }

            return ProduceTagsAsync(document, snapshotSpan.Snapshot, caretPosition.Value, cancellationToken);
        }

        internal async Task<IEnumerable<ITagSpan<BraceHighlightTag>>> ProduceTagsAsync(
            Document document,
            ITextSnapshot snapshot,
            int position,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Tagger_BraceHighlighting_TagProducer_ProduceTags, cancellationToken))
            {
                var tags = new List<ITagSpan<BraceHighlightTag>>();

                await ProduceTagsForBracesAsync(document, snapshot, position, rightBrace: false, tags: tags, cancellationToken: cancellationToken).ConfigureAwait(false);
                await ProduceTagsForBracesAsync(document, snapshot, position - 1, rightBrace: true, tags: tags, cancellationToken: cancellationToken).ConfigureAwait(false);

                return tags;
            }
        }

        private async Task ProduceTagsForBracesAsync(
            Document document,
            ITextSnapshot snapshot,
            int position,
            bool rightBrace,
            IList<ITagSpan<BraceHighlightTag>> tags,
            CancellationToken cancellationToken)
        {
            if (position >= 0 && position < snapshot.Length)
            {
                var braces = await _braceMatcherService.GetMatchingBracesAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (braces.HasValue)
                {
                    if ((!rightBrace && braces.Value.LeftSpan.Start == position) ||
                        (rightBrace && braces.Value.RightSpan.Start == position))
                    {
                        tags.Add(snapshot.GetTagSpan(braces.Value.LeftSpan.ToSpan(), BraceHighlightTag.StartTag));
                        tags.Add(snapshot.GetTagSpan(braces.Value.RightSpan.ToSpan(), BraceHighlightTag.EndTag));
                    }
                }
            }
        }
    }
}
