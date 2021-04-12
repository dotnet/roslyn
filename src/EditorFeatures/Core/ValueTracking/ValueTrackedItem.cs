// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;

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

        private ValueTrackedItem(
            ISymbol symbol,
            SourceText sourceText,
            ImmutableArray<ClassifiedSpan> classifiedSpans,
            TextSpan textSpan,
            Document document,
            ValueTrackedItem? parent = null)
        {
            Symbol = symbol;
            Parent = parent;

            Span = textSpan;
            ClassifiedSpans = classifiedSpans;
            SourceText = sourceText;
            Document = document;
        }

        public override string ToString()
        {
            var subText = SourceText.GetSubText(Span);
            return subText.ToString();
        }

        public sealed class TruncatedClassifiedSpans
        {
            public ImmutableArray<ClassifiedSpan> Spans { get; }
            public int Start => IsEmpty ? 0 : Spans[0].TextSpan.Start;
            public int Length => IsEmpty ? 0 : Spans.Sum(c => c.TextSpan.Length);
            public bool IsEmpty => Spans.IsDefaultOrEmpty;

            public TruncatedClassifiedSpans(
                ImmutableArray<ClassifiedSpan> spans)
            {
                Spans = spans;
            }

            public static readonly TruncatedClassifiedSpans Empty = new TruncatedClassifiedSpans(default);
        }

        /// <summary>
        /// Gets the truncated version of the spans if the text length is greater than the max length provided. Returns true
        /// if the out params represent some subset of <see cref="ClassifiedSpans"/>
        /// </summary>
        /// <param name="maxLength">The maximum text length before truncation happens</param>
        /// <param name="beginning">If not empty, the chunk of spans that should always be shown at the beginning</param>
        /// <param name="middle">If not empty, the chunk of spans that should be shown and may be surrounded by indicators of truncation</param>
        /// <param name="end">If not empty, the chunk of spans that should always be shown at the end</param>
        /// <remarks>
        /// For trivial cases, beginning and end will not be used. This means the display can be "... [text] ...".
        /// However, for some cases that loses important context for the line.
        /// <para>
        /// Example: <code>FullName = GetFirstName() + GetMiddleName() + GetLastName();</code>
        /// Assume "GetMiddleName()" is the important symbol to be shown for this item, we want to show
        /// something like this:
        ///     "FullName = ... + GetMiddleName() + ...".
        /// This keeps more context for the user to see. It's up to overrides of ValueTrackedItem to handle splitting the spans as needed. 
        /// </para>
        /// </remarks>
        public virtual bool GetTruncatedClassifiedSpans(int maxLength, out TruncatedClassifiedSpans beginning, out TruncatedClassifiedSpans middle, out TruncatedClassifiedSpans end, out int totalLength)
        {
            beginning = TruncatedClassifiedSpans.Empty;
            middle = TruncatedClassifiedSpans.Empty;
            end = TruncatedClassifiedSpans.Empty;

            if (ClassifiedSpans.IsDefaultOrEmpty)
            {
                totalLength = 0;
                return false;
            }

            totalLength = ClassifiedSpans.Sum(c => c.TextSpan.Length);
            if (totalLength <= maxLength || Span.Length >= maxLength)
            {
                middle = new TruncatedClassifiedSpans(ClassifiedSpans);
                return false;
            }

            var builder = ImmutableArray.CreateBuilder<ClassifiedSpan>();
            var beginningOfClassification = ClassifiedSpans[0].TextSpan.Start;
            var minInclude = Span.Start - ((maxLength - Span.Length) / 2);
            var maxInclude = Span.End + ((maxLength - Span.Length) / 2);

            var position = beginningOfClassification;

            foreach (var part in ClassifiedSpans)
            {
                if (position >= minInclude && position <= maxInclude)
                {
                    builder.Add(part);
                }
                else if (position > maxInclude)
                {
                    break;
                }

                position += part.TextSpan.Length;
            }

            middle = new TruncatedClassifiedSpans(
                builder.ToImmutable());

            return true;
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

            return new ValueTrackedItem(
                        symbol,
                        sourceText,
                        classifiedSpans,
                        location.SourceSpan,
                        document,
                        parent: parent);
        }
    }
}
