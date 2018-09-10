// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal sealed partial class ContainedDocument
    {
        public class DocumentServiceProvider : IDocumentServiceProvider
        {
            public static readonly IDocumentServiceProvider Instace = new DocumentServiceProvider();

            private readonly SpanMapper _spanMapper;
            private readonly DocumentExcerpter _excerpter;

            private DocumentServiceProvider()
            {
                _spanMapper = new SpanMapper();
                _excerpter = new DocumentExcerpter();
            }

            public TService GetService<TService>() where TService : class, IDocumentService
            {
                if (_spanMapper is TService spanMapper)
                {
                    return spanMapper;
                }

                if (_excerpter is TService excerpter)
                {
                    return excerpter;
                }

                // ask the default document service provider
                return TextDocumentState.DefaultDocumentServiceProvider.Instance.GetService<TService>();
            }

            private static IProjectionSnapshot GetProjectSnapshot(SourceText sourceText)
            {
                return sourceText.FindCorrespondingEditorTextSnapshot() as IProjectionSnapshot;
            }

            private static void VerifyDocument(Document document)
            {
                // only internal people should use this. so throw when mis-used
                var containedDocument = TryGetContainedDocument(document.Id);
                Contract.ThrowIfNull(containedDocument);
            }

            private class SpanMapper : ISpanMappingService
            {
                public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
                {
                    // REVIEW: for now, we keep document here due to open file case, otherwise, we need to create new SpanMappingService for every char user types.

                    VerifyDocument(document);
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var projectionSnapshot = GetProjectSnapshot(sourceText);
                    if (projectionSnapshot == null)
                    {
                        return default;
                    }

                    var builder = ArrayBuilder<MappedSpanResult>.GetInstance();
                    foreach (var span in spans)
                    {
                        var result = default(MappedSpanResult?);
                        foreach (var primarySpan in projectionSnapshot.MapToSourceSnapshots(span.ToSpan()))
                        {
                            // this is from http://index/?query=MapSecondaryToPrimarySpan&rightProject=Microsoft.VisualStudio.Editor.Implementation&file=VsTextBufferCoordinatorAdapter.cs&line=177
                            // make sure we only consider one that's not split
                            if (primarySpan.Length != span.Length)
                            {
                                continue;
                            }

                            // take the first one.
                            // contained document file path points cshtml this secondary buffer belong to
                            result = new MappedSpanResult(document.FilePath, primarySpan.ToLinePositionSpan(), primarySpan.Span.ToTextSpan());
                            break;
                        }

                        // this is only used internally. we don't expect it to ever fail to map to primary buffer. 
                        // otherwise. caller is using it wrong
                        Contract.ThrowIfFalse(result.HasValue);

                        builder.Add(result.Value);
                    }

                    return builder.ToImmutableAndFree();
                }
            }

            private class DocumentExcerpter : IDocumentExcerptService
            {
                public async Task<ExcerptResult?> TryExcerptAsync(Document document, TextSpan span, ExcerptMode mode, CancellationToken cancellationToken)
                {
                    VerifyDocument(document);

                    // REVIEW: for now, we keep document here due to open file case, otherwise, we need to create new SpanMappingService for every char user types.
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var projectionSnapshot = GetProjectSnapshot(sourceText);
                    if (projectionSnapshot == null)
                    {
                        return null;
                    }

                    var spanOnPrimarySnapshot = MapRoslynSpanToPrimarySpan(projectionSnapshot, span);
                    if (spanOnPrimarySnapshot == null)
                    {
                        return null;
                    }

                    var contentSpanOnPrimarySnapshot = GetContentSpanFromPrimarySpan(mode, spanOnPrimarySnapshot.Value);
                    if (contentSpanOnPrimarySnapshot == null)
                    {
                        // can't figure out span to extract content from
                        return null;
                    }

                    var (content, spanOnContent) = GetContentAndMappedSpan(mode, spanOnPrimarySnapshot.Value, contentSpanOnPrimarySnapshot.Value);
                    if (content == null)
                    {
                        return null;
                    }

                    var classifiedSpansOnContent = await GetClassifiedSpansOnContent(document, projectionSnapshot, contentSpanOnPrimarySnapshot.Value, cancellationToken);

                    // the default implementation has no idea how to classify the primary snapshot
                    return new ExcerptResult(content, spanOnContent, classifiedSpansOnContent, document, span);
                }

                private static async Task<ImmutableArray<ClassifiedSpan>> GetClassifiedSpansOnContent(Document document, IProjectionSnapshot projectionSnapshot, SnapshotSpan contentSpanOnPrimarySnapshot, CancellationToken cancellationToken)
                {
                    // map content span on the primary buffer to second buffer and for ones that can be mapped,
                    // get classification for those portion on secondary buffer and convert span on those to
                    // span on the content and create ClassifiedSpan
                    var contentSpan = contentSpanOnPrimarySnapshot.Span.ToTextSpan();

                    using (var pooledObject = SharedPools.Default<List<ClassifiedSpan>>().GetPooledObject())
                    {
                        var list = pooledObject.Object;

                        foreach (var roslynSpan in projectionSnapshot.MapFromSourceSnapshot(contentSpanOnPrimarySnapshot))
                        {
                            var classifiedSpans = await EditorClassifier.GetClassifiedSpansAsync(document, roslynSpan.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                            if (classifiedSpans.IsDefault)
                            {
                                continue;
                            }

                            foreach (var classifiedSpan in classifiedSpans)
                            {
                                var mappedSpan = MapRoslynSpanToPrimarySpan(projectionSnapshot, classifiedSpan.TextSpan);
                                if (mappedSpan == null)
                                {
                                    continue;
                                }

                                list.Add(new ClassifiedSpan(GetSpanOnContent(mappedSpan.Value.Span.ToTextSpan(), contentSpan), classifiedSpan.ClassificationType));
                            }
                        }

                        // everything is mapped to content which always start from 0
                        // classifier expects there is no gap between classification spans. any empty space
                        // from the above classification call will be filled with "text"
                        var builder = ArrayBuilder<ClassifiedSpan>.GetInstance();

                        var startPosition = GetNonWhitespaceStartPositionOnContent(contentSpanOnPrimarySnapshot);
                        EditorClassifier.FillInClassifiedSpanGaps(startPosition, list, builder);

                        // add html after roslyn content if there is any
                        if (builder.Count == 0)
                        {
                            // no roslyn code. add all as html code
                            builder.Add(new ClassifiedSpan(new TextSpan(0, contentSpan.Length), ClassificationTypeNames.Text));
                        }
                        else
                        {
                            var lastSpan = builder[builder.Count - 1].TextSpan;
                            if (lastSpan.End < contentSpan.Length)
                            {
                                builder.Add(new ClassifiedSpan(new TextSpan(lastSpan.End, contentSpan.Length - lastSpan.End), ClassificationTypeNames.Text));
                            }
                        }

                        return builder.ToImmutableAndFree();
                    }
                }

                private static int GetNonWhitespaceStartPositionOnContent(SnapshotSpan spanOnPrimarySnapshot)
                {
                    for (var i = spanOnPrimarySnapshot.Start.Position; i < spanOnPrimarySnapshot.End.Position; i++)
                    {
                        if (!char.IsWhiteSpace(spanOnPrimarySnapshot.Snapshot[i]))
                        {
                            return i - spanOnPrimarySnapshot.Start.Position;
                        }
                    }

                    return spanOnPrimarySnapshot.Length;
                }

                private static SnapshotSpan? MapRoslynSpanToPrimarySpan(IProjectionSnapshot projectionBuffer, TextSpan span)
                {
                    var primarySpans = projectionBuffer.MapToSourceSnapshots(span.ToSpan());
                    if (primarySpans.Count != 1)
                    {
                        // default version doesn't support where span mapped multiple primary buffer spans
                        return null;
                    }

                    return primarySpans[0];
                }

                private static (SourceText, TextSpan) GetContentAndMappedSpan(ExcerptMode mode, SnapshotSpan primarySpan, SnapshotSpan contentSpan)
                {
                    var line = primarySpan.Start.GetContainingLine();

                    if (mode == ExcerptMode.SingleLine)
                    {
                        return (line.Snapshot.AsText().GetSubText(contentSpan.Span.ToTextSpan()), GetSpanOnContent(primarySpan.Span.ToTextSpan(), contentSpan.Span.ToTextSpan()));
                    }

                    if (mode == ExcerptMode.Tooltip)
                    {
                        return (line.Snapshot.AsText().GetSubText(contentSpan.Span.ToTextSpan()), GetSpanOnContent(primarySpan.Span.ToTextSpan(), contentSpan.Span.ToTextSpan()));
                    }

                    return (default, default);
                }

                private static SnapshotSpan? GetContentSpanFromPrimarySpan(ExcerptMode mode, SnapshotSpan primarySpan)
                {
                    var line = primarySpan.Start.GetContainingLine();

                    if (mode == ExcerptMode.SingleLine)
                    {
                        // the line where primary span is on
                        return line.Extent;
                    }

                    if (mode == ExcerptMode.Tooltip)
                    {
                        // +-3 line of the line where primary span is on
                        const int AdditionalLineCountPerSide = 3;

                        var startLine = line.Snapshot.GetLineFromLineNumber(Math.Max(0, line.LineNumber - AdditionalLineCountPerSide));
                        var endLine = line.Snapshot.GetLineFromLineNumber(Math.Min(line.Snapshot.LineCount - 1, line.LineNumber + AdditionalLineCountPerSide));

                        return new SnapshotSpan(line.Snapshot, Span.FromBounds(startLine.Extent.Start.Position, endLine.Extent.End.Position));
                    }

                    return default;
                }

                private static TextSpan GetSpanOnContent(TextSpan targetSpan, TextSpan excerptSpan)
                {
                    return new TextSpan(targetSpan.Start - excerptSpan.Start, targetSpan.Length);
                }
            }
        }
    }
}
