// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    internal class HighlighterTagProducer : AbstractSingleDocumentTagProducer<HighlightTag>
    {
        private readonly IHighlightingService _highlightingService;

        public HighlighterTagProducer(IHighlightingService highlightingService)
        {
            _highlightingService = highlightingService;
        }

        public async override Task<IEnumerable<ITagSpan<HighlightTag>>> ProduceTagsAsync(
            Document document,
            SnapshotSpan snapshotSpan,
            int? caretPosition,
            CancellationToken cancellationToken)
        {
            if (document == null)
            {
                return SpecializedCollections.EmptyEnumerable<ITagSpan<HighlightTag>>();
            }

            var options = document.Project.Solution.Workspace.Options;
            if (!options.GetOption(FeatureOnOffOptions.KeywordHighlighting, document.Project.Language))
            {
                return SpecializedCollections.EmptyEnumerable<ITagSpan<HighlightTag>>();
            }

            if (caretPosition.HasValue)
            {
                var position = caretPosition.Value;
                var snapshot = snapshotSpan.Snapshot;

                using (Logger.LogBlock(FunctionId.Tagger_Highlighter_TagProducer_ProduceTags, cancellationToken))
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    var spans = _highlightingService.GetHighlights(root, position, cancellationToken);
                    if (spans.Any())
                    {
                        return spans.Select(span =>
                            new TagSpan<HighlightTag>(span.ToSnapshotSpan(snapshot), HighlightTag.Instance));
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<ITagSpan<HighlightTag>>();
        }
    }
}
