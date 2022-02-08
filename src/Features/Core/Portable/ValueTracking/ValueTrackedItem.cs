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
        public SourceText SourceText { get; }
        public Glyph Glyph { get; }

        internal ValueTrackedItem(
            SymbolKey symbolKey,
            SourceText sourceText,
            TextSpan textSpan,
            DocumentId documentId,
            Glyph glyph,
            ValueTrackedItem? parent)
        {
            SymbolKey = symbolKey;
            Parent = parent;
            Glyph = glyph;
            Span = textSpan;
            SourceText = sourceText;
            DocumentId = documentId;
        }

        public override string ToString()
        {
            var subText = SourceText.GetSubText(Span);
            return subText.ToString();
        }

        public static async ValueTask<ValueTrackedItem?> TryCreateAsync(Solution solution, Location location, ISymbol symbol, ValueTrackedItem? parent = null, CancellationToken cancellationToken = default)
        {
            Contract.ThrowIfNull(location.SourceTree);

            var document = solution.GetRequiredDocument(location.SourceTree);

            var excerptService = document.Services.GetService<IDocumentExcerptService>();
            SourceText? sourceText = null;

            if (excerptService != null)
            {
                var result = await excerptService.TryExcerptAsync(document, location.SourceSpan, ExcerptMode.SingleLine, cancellationToken).ConfigureAwait(false);
                sourceText = result?.Content;
            }

            if (sourceText is null)
            {
                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            }

            return new ValueTrackedItem(
                SymbolKey.Create(symbol, cancellationToken),
                sourceText,
                location.SourceSpan,
                document.Id,
                symbol.GetGlyph(),
                parent);
        }
    }
}
