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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal sealed partial class ContainedDocument
    {
        // this is to support old venus/razor case before dev16. 
        // all new razor (asp.NET core after dev16) should use thier own implementation not ours
        public class DocumentServiceProvider : IDocumentServiceProvider
        {
            private readonly SpanMapper _spanMapper;
            private readonly DocumentExcerpter _excerpter;

            public DocumentServiceProvider(ITextBuffer primaryBuffer)
            {
                _spanMapper = new SpanMapper(primaryBuffer);
                _excerpter = new DocumentExcerpter(primaryBuffer);
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
                return DefaultTextDocumentServiceProvider.Instance.GetService<TService>();
            }

            private static ITextSnapshot GetRoslynSnapshot(SourceText sourceText)
            {
                return sourceText.FindCorrespondingEditorTextSnapshot();
            }

            private class SpanMapper : ISpanMappingService
            {
                private readonly ITextBuffer _primaryBuffer;

                public SpanMapper(ITextBuffer primaryBuffer)
                {
                    _primaryBuffer = primaryBuffer;
                }

                public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
                {
                    // REVIEW: for now, we keep document here due to open file case, otherwise, we need to create new SpanMappingService for every char user types.
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    // _primary buffer (in this case razor html files) is not in roslyn snapshot, so mapping from roslyn snapshot to razor document
                    // always just map to current snapshot which have potential to have a race since content could have changed while we are doing this.
                    // but for ones that uses this implementation, it always had that possiblity. so this doesn't change that aspect due to this.
                    var primarySnapshot = (IProjectionSnapshot)_primaryBuffer.CurrentSnapshot;
                    var roslynSnapshot = GetRoslynSnapshot(sourceText);
                    if (roslynSnapshot == null)
                    {
                        return default;
                    }

                    var builder = ArrayBuilder<MappedSpanResult>.GetInstance();
                    foreach (var span in spans)
                    {
                        var result = default(MappedSpanResult?);
                        foreach (var primarySpan in primarySnapshot.MapFromSourceSnapshot(span.ToSnapshotSpan(roslynSnapshot)))
                        {
                            // this is from http://index/?query=MapSecondaryToPrimarySpan&rightProject=Microsoft.VisualStudio.Editor.Implementation&file=VsTextBufferCoordinatorAdapter.cs&line=177
                            // make sure we only consider one that's not split
                            if (primarySpan.Length != span.Length)
                            {
                                continue;
                            }

                            // take the first one.
                            // contained document file path points cshtml this secondary buffer belong to
                            var primarySnapshotSpan = new SnapshotSpan(primarySnapshot, primarySpan);
                            result = new MappedSpanResult(document.FilePath, primarySnapshotSpan.ToLinePositionSpan(), primarySpan.ToTextSpan());
                            break;
                        }

                        builder.Add(result ?? default);
                    }

                    return builder.ToImmutableAndFree();
                }
            }

            private class DocumentExcerpter : IDocumentExcerptService
            {
                private readonly ITextBuffer _primaryBuffer;

                public DocumentExcerpter(ITextBuffer primaryBuffer)
                {
                    _primaryBuffer = primaryBuffer;
                }

                public async Task<ExcerptResult?> TryExcerptAsync(Document document, TextSpan span, ExcerptMode mode, CancellationToken cancellationToken)
                {
                    // REVIEW: for now, we keep document here due to open file case, otherwise, we need to create new DocumentExcerpter for every char user types.
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var primarySnapshot = (IProjectionSnapshot)_primaryBuffer.CurrentSnapshot;

                    var roslynSnapshot = GetRoslynSnapshot(sourceText);
                    if (roslynSnapshot == null)
                    {
                        return null;
                    }

                    var spanOnPrimarySnapshot = MapRoslynSpanToPrimarySpan(primarySnapshot, roslynSnapshot, span);
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

                    var classifiedSpansOnContent = await GetClassifiedSpansOnContent(document, roslynSnapshot, contentSpanOnPrimarySnapshot.Value, cancellationToken).ConfigureAwait(false);

                    // the default implementation has no idea how to classify the primary snapshot
                    return new ExcerptResult(content, spanOnContent, classifiedSpansOnContent, document, span);
                }

                private static async Task<ImmutableArray<ClassifiedSpan>> GetClassifiedSpansOnContent(
                    Document document, ITextSnapshot roslynSnapshot, SnapshotSpan contentSpanOnPrimarySnapshot, CancellationToken cancellationToken)
                {
                    var primarySnapshot = (IProjectionSnapshot)contentSpanOnPrimarySnapshot.Snapshot;

                    // map content span on the primary buffer to second buffer and for ones that can be mapped,
                    // get classification for those portion on secondary buffer and convert span on those to
                    // span on the content and create ClassifiedSpan
                    var contentSpan = contentSpanOnPrimarySnapshot.Span.ToTextSpan();

                    // anything based on content is starting from 0
                    var startPositionOnContentSpan = GetNonWhitespaceStartPositionOnContent(contentSpanOnPrimarySnapshot);

                    using var pooledObject = SharedPools.Default<List<ClassifiedSpan>>().GetPooledObject();
                    var list = pooledObject.Object;

                    foreach (var roslynSpan in primarySnapshot.MapToSourceSnapshots(contentSpanOnPrimarySnapshot.Span))
                    {
                        if (roslynSnapshot.TextBuffer != roslynSpan.Snapshot.TextBuffer)
                        {
                            // not mapped to right buffer. ignore
                            continue;
                        }

                        // we don't have gurantee that pirmary snapshot is from same snapshot as roslyn snapshot. make sure
                        // we map it to right snapshot
                        var fixedUpSpan = roslynSpan.TranslateTo(roslynSnapshot, SpanTrackingMode.EdgeExclusive);
                        var classifiedSpans = await ClassifierHelper.GetClassifiedSpansAsync(document, fixedUpSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                        if (classifiedSpans.IsDefault)
                        {
                            continue;
                        }

                        foreach (var classifiedSpan in classifiedSpans)
                        {
                            var mappedSpan = MapRoslynSpanToPrimarySpan(primarySnapshot, roslynSnapshot, classifiedSpan.TextSpan);
                            if (mappedSpan == null)
                            {
                                continue;
                            }

                            var spanOnContentSpan = GetSpanOnContent(mappedSpan.Value.Span.ToTextSpan(), contentSpan);
                            if (spanOnContentSpan.Start < startPositionOnContentSpan)
                            {
                                // skip span before start position.
                                continue;
                            }

                            list.Add(new ClassifiedSpan(spanOnContentSpan, classifiedSpan.ClassificationType));
                        }
                    }

                    // classifier expects there is no gap between classification spans. any empty space
                    // from the above classification call will be filled with "text"
                    //
                    // the EditorClassifier call above fills all the gaps for the span it is called with, but we are combining
                    // multiple spans with html code, so we need to fill those gaps
                    var builder = ArrayBuilder<ClassifiedSpan>.GetInstance();
                    ClassifierHelper.FillInClassifiedSpanGaps(startPositionOnContentSpan, list, builder);

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

                private static SnapshotSpan? MapRoslynSpanToPrimarySpan(IProjectionSnapshot primarySnapshot, ITextSnapshot roslynSnapshot, TextSpan span)
                {
                    var primarySpans = primarySnapshot.MapFromSourceSnapshot(span.ToSnapshotSpan(roslynSnapshot));
                    if (primarySpans.Count != 1)
                    {
                        // default version doesn't support where span mapped multiple primary buffer spans
                        return null;
                    }

                    return new SnapshotSpan(primarySnapshot, primarySpans[0]);
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
