// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
{
    internal partial class ReferenceHighlightingViewTaggerProvider
    {
        // Internal for testing purposes.
        internal class TagProducer : ITagProducer<AbstractNavigatableReferenceHighlightingTag>
        {
            public IEqualityComparer<AbstractNavigatableReferenceHighlightingTag> TagComparer
            {
                get
                {
                    return EqualityComparer<AbstractNavigatableReferenceHighlightingTag>.Default;
                }
            }

            public void Dispose()
            {
            }

            public Task<IEnumerable<ITagSpan<AbstractNavigatableReferenceHighlightingTag>>> ProduceTagsAsync(
                IEnumerable<DocumentSnapshotSpan> snapshotSpans, SnapshotPoint? caretPosition, CancellationToken cancellationToken)
            {
                // NOTE(cyrusn): Normally we'd limit ourselves to producing tags in the span we were
                // asked about.  However, we want to produce all tags here so that the user can actually
                // navigate between all of them using the appropriate tag navigation commands.  If we
                // don't generate all the tags then the user will cycle through an incorrect subset.
                if (caretPosition == null)
                {
                    return SpecializedTasks.EmptyEnumerable<ITagSpan<AbstractNavigatableReferenceHighlightingTag>>();
                }

                var position = caretPosition.Value;

                Workspace workspace;
                if (!Workspace.TryGetWorkspace(position.Snapshot.AsText().Container, out workspace))
                {
                    return SpecializedTasks.EmptyEnumerable<ITagSpan<AbstractNavigatableReferenceHighlightingTag>>();
                }

                var document = snapshotSpans.First(vt => vt.SnapshotSpan.Snapshot == position.Snapshot).Document;
                if (document == null)
                {
                    return SpecializedTasks.EmptyEnumerable<ITagSpan<AbstractNavigatableReferenceHighlightingTag>>();
                }

                return ProduceTagsAsync(snapshotSpans, position, workspace, document, cancellationToken);
            }

            internal async Task<IEnumerable<ITagSpan<AbstractNavigatableReferenceHighlightingTag>>> ProduceTagsAsync(
                IEnumerable<DocumentSnapshotSpan> snapshotSpans,
                SnapshotPoint position,
                Workspace workspace,
                Document document,
                CancellationToken cancellationToken)
            {
                // Don't produce tags if the feature is not enabled.
                if (!workspace.Options.GetOption(FeatureOnOffOptions.ReferenceHighlighting, document.Project.Language))
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<AbstractNavigatableReferenceHighlightingTag>>();
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
                                    await AddTagSpansAsync(solution, result, documentHighlights, cancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                    }

                    return result;
                }
            }

            private async Task AddTagSpansAsync(
                Solution solution, List<ITagSpan<AbstractNavigatableReferenceHighlightingTag>> tags, DocumentHighlights documentHighlights, CancellationToken cancellationToken)
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
                    tags.Add(new TagSpan<AbstractNavigatableReferenceHighlightingTag>(
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
}
