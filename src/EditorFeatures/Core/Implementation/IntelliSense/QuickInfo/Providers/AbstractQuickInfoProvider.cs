// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal abstract partial class AbstractQuickInfoProvider : IQuickInfoProvider
    {
        public async Task<QuickInfoItem> GetItemAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = await tree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            var state = await GetQuickInfoItemAsync(document, token, position, cancellationToken).ConfigureAwait(false);
            if (state != null)
            {
                return state;
            }

            if (ShouldCheckPreviousToken(token))
            {
                var previousToken = token.GetPreviousToken();

                if ((state = await GetQuickInfoItemAsync(document, previousToken, position, cancellationToken).ConfigureAwait(false)) != null)
                {
                    return state;
                }
            }

            return null;
        }

        protected virtual bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return true;
        }

        private async Task<QuickInfoItem> GetQuickInfoItemAsync(
            Document document,
            SyntaxToken token,
            int position,
            CancellationToken cancellationToken)
        {
            if (token != default &&
                token.Span.IntersectsWith(position))
            {
                var deferredContent = await BuildContentAsync(document, token, cancellationToken).ConfigureAwait(false);
                if (deferredContent != null)
                {
                    return new QuickInfoItem(token.Span, deferredContent);
                }
            }

            return null;
        }

        protected abstract Task<IDeferredQuickInfoContent> BuildContentAsync(Document document, SyntaxToken token, CancellationToken cancellationToken);

        protected IDeferredQuickInfoContent CreateQuickInfoDisplayDeferredContent(
            ISymbol symbol,
            bool showWarningGlyph,
            bool showSymbolGlyph,
            IList<TaggedText> mainDescription,
            IDeferredQuickInfoContent documentation,
            IList<TaggedText> typeParameterMap,
            IList<TaggedText> anonymousTypes,
            IList<TaggedText> usageText,
            IList<TaggedText> exceptionText)
        {
            return new QuickInfoDisplayDeferredContent(
                symbolGlyph: showSymbolGlyph ? CreateGlyphDeferredContent(symbol) : null,
                warningGlyph: showWarningGlyph ? CreateWarningGlyph() : null,
                mainDescription: CreateClassifiableDeferredContent(mainDescription),
                documentation: documentation,
                typeParameterMap: CreateClassifiableDeferredContent(typeParameterMap),
                anonymousTypes: CreateClassifiableDeferredContent(anonymousTypes),
                usageText: CreateClassifiableDeferredContent(usageText),
                exceptionText: CreateClassifiableDeferredContent(exceptionText));
        }

        private IDeferredQuickInfoContent CreateWarningGlyph()
        {
            return new SymbolGlyphDeferredContent(Glyph.CompletionWarning);
        }

        protected IDeferredQuickInfoContent CreateQuickInfoDisplayDeferredContent(
            Glyph glyph,
            IList<TaggedText> mainDescription,
            IDeferredQuickInfoContent documentation,
            IList<TaggedText> typeParameterMap,
            IList<TaggedText> anonymousTypes,
            IList<TaggedText> usageText,
            IList<TaggedText> exceptionText)
        {
            return new QuickInfoDisplayDeferredContent(
                symbolGlyph: new SymbolGlyphDeferredContent(glyph),
                warningGlyph: null,
                mainDescription: CreateClassifiableDeferredContent(mainDescription),
                documentation: documentation,
                typeParameterMap: CreateClassifiableDeferredContent(typeParameterMap),
                anonymousTypes: CreateClassifiableDeferredContent(anonymousTypes),
                usageText: CreateClassifiableDeferredContent(usageText),
                exceptionText: CreateClassifiableDeferredContent(exceptionText));
        }

        protected IDeferredQuickInfoContent CreateGlyphDeferredContent(ISymbol symbol)
        {
            return new SymbolGlyphDeferredContent(symbol.GetGlyph());
        }

        protected IDeferredQuickInfoContent CreateClassifiableDeferredContent(
            IList<TaggedText> content)
        {
            return new ClassifiableDeferredContent(content);
        }

        protected IDeferredQuickInfoContent CreateDocumentationCommentDeferredContent(
            string documentationComment)
        {
            return new DocumentationCommentDeferredContent(documentationComment);
        }

        protected IDeferredQuickInfoContent CreateProjectionBufferDeferredContent(SnapshotSpan span)
        {
            return new ProjectionBufferDeferredContent(span);
        }
    }
}
