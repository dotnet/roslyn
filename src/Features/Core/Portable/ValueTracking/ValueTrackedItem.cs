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

namespace Microsoft.CodeAnalysis.ValueTracking
{
    internal class ValueTrackedItem
    {
        public SymbolKey SymbolKey { get; }
        public ValueTrackedItem? Parent { get; }

        public DocumentId DocumentId { get; }
        public TextSpan Span { get; }
        public ImmutableArray<ClassifiedSpan> ClassifiedSpans { get; }
        public SourceText SourceText { get; }
        public Glyph Glyph { get; }

        private ValueTrackedItem(
            SymbolKey symbolKey,
            SourceText sourceText,
            ImmutableArray<ClassifiedSpan> classifiedSpans,
            TextSpan textSpan,
            DocumentId documentId,
            Glyph glyph,
            ValueTrackedItem? parent = null)
        {
            SymbolKey = symbolKey;
            Parent = parent;
            Glyph = glyph;
            Span = textSpan;
            ClassifiedSpans = classifiedSpans;
            SourceText = sourceText;
            DocumentId = documentId;
        }

        public override string ToString()
        {
            var subText = SourceText.GetSubText(Span);
            return subText.ToString();
        }

        public static Task<ValueTrackedItem?> TryCreateAsync(Solution solution, Location location, ISymbol symbol, ValueTrackedItem? parent = null, CancellationToken cancellationToken = default)
        {
            Contract.ThrowIfNull(location.SourceTree);

            var document = solution.GetRequiredDocument(location.SourceTree);
            return TryCreateAsync(document, location.SourceSpan, symbol, parent, cancellationToken);
        }

        public static async Task<ValueTrackedItem?> TryCreateAsync(Document document, TextSpan textSpan, ISymbol symbol, ValueTrackedItem? parent = null, CancellationToken cancellationToken = default)
        {
            var excerptService = document.Services.GetService<IDocumentExcerptService>();
            SourceText? sourceText = null;
            ImmutableArray<ClassifiedSpan> classifiedSpans = default;

            if (excerptService != null)
            {
                var result = await excerptService.TryExcerptAsync(document, textSpan, ExcerptMode.SingleLine, cancellationToken).ConfigureAwait(false);
                if (result.HasValue)
                {
                    var value = result.Value;
                    sourceText = value.Content;
                }
            }

            if (sourceText is null)
            {
                var options = ClassificationOptions.From(document.Project);
                var documentSpan = await ClassifiedSpansAndHighlightSpanFactory.GetClassifiedDocumentSpanAsync(document, textSpan, options, cancellationToken).ConfigureAwait(false);
                var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, options, cancellationToken).ConfigureAwait(false);
                classifiedSpans = classificationResult.ClassifiedSpans;
                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            }

            return new ValueTrackedItem(
                        SymbolKey.Create(symbol, cancellationToken),
                        sourceText,
                        classifiedSpans,
                        textSpan,
                        document.Id,
                        symbol.GetGlyph(),
                        parent: parent);
        }
    }
}
