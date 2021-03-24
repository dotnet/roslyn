// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    internal class ValueTrackedItem
    {
        public ISymbol Symbol { get; }
        public ValueTrackedItem? Parent { get; }

        public Document Document { get; }
        public TextSpan Span { get; }
        public ImmutableArray<ClassifiedSpan> ClassifiedSpans { get; }
        public SourceText SourceText { get; }
        public LineSpan LineSpan { get; }

        private ValueTrackedItem(
            ISymbol symbol,
            SourceText sourceText,
            ImmutableArray<ClassifiedSpan> classifiedSpans,
            TextSpan textSpan,
            Document document,
            LineSpan lineSpan,
            ValueTrackedItem? parent = null)
        {
            Symbol = symbol;
            Parent = parent;

            Span = textSpan;
            ClassifiedSpans = classifiedSpans;
            SourceText = sourceText;
            LineSpan = lineSpan;
            Document = document;
        }

        public override string ToString()
        {
            var subText = SourceText.GetSubText(Span);
            return subText.ToString();
        }

        public static async Task<ValueTrackedItem?> TryCreateAsync(Solution solution, Location location, ISymbol symbol, ValueTrackedItem? parent = null, CancellationToken cancellationToken = default)
        {
            Contract.ThrowIfNull(location.SourceTree);

            var document = solution.GetRequiredDocument(location.SourceTree);
            var excerptService = document.Services.GetService<IDocumentExcerptService>();
            SourceText? sourceText = null;
            ImmutableArray<ClassifiedSpan> classifiedSpans = default;

            if (excerptService != null)
            {
                var result = await excerptService.TryExcerptAsync(document, location.SourceSpan, ExcerptMode.SingleLine, cancellationToken).ConfigureAwait(false);
                if (result.HasValue)
                {
                    var value = result.Value;
                    sourceText = value.Content;
                }
            }

            if (sourceText is null)
            {
                var documentSpan = await ClassifiedSpansAndHighlightSpanFactory.GetClassifiedDocumentSpanAsync(document, location.SourceSpan, cancellationToken).ConfigureAwait(false);
                var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, cancellationToken).ConfigureAwait(false);
                classifiedSpans = classificationResult.ClassifiedSpans;
                sourceText = await location.SourceTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            }

            sourceText.GetLineAndOffset(location.SourceSpan.Start, out var lineStart, out var _);
            sourceText.GetLineAndOffset(location.SourceSpan.End, out var lineEnd, out var _);
            var lineSpan = LineSpan.FromBounds(lineStart, lineEnd);

            return new ValueTrackedItem(
                        symbol,
                        sourceText,
                        classifiedSpans,
                        location.SourceSpan,
                        document,
                        lineSpan,
                        parent: parent);
        }
    }
}
