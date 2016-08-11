// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class CommonQuickInfoElementProvider : QuickInfoElementProvider
    {
        public override async Task<QuickInfoData> GetQuickInfoElementAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = await tree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            var element = await GetQuickInfoElementAsync(document, token, position, cancellationToken).ConfigureAwait(false);
            if (element != null)
            {
                return new QuickInfoData(token.Span, element);
            }

            if (ShouldCheckPreviousToken(token))
            {
                var previousToken = token.GetPreviousToken();

                if ((element = await GetQuickInfoElementAsync(document, previousToken, position, cancellationToken).ConfigureAwait(false)) != null)
                {
                    return new QuickInfoData(previousToken.Span, element);
                }
            }

            return null;
        }

        protected virtual bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return true;
        }

        private async Task<QuickInfoElement> GetQuickInfoElementAsync(
            Document document,
            SyntaxToken token,
            int position,
            CancellationToken cancellationToken)
        {
            if (token != default(SyntaxToken) &&
                token.Span.IntersectsWith(position))
            {
                return await BuildElementAsync(document, token, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        protected abstract Task<QuickInfoElement> BuildElementAsync(Document document, SyntaxToken token, CancellationToken cancellationToken);

        protected QuickInfoElement CreateSymbolGlyphElement(Glyph glyph)
        {
            return QuickInfoElement.Create(QuickInfoElementKinds.Symbol, tags: GlyphTags.GetTags(glyph));
        }

        protected QuickInfoElement CreateWarningGlyphElement()
        {
            return QuickInfoElement.Create(QuickInfoElementKinds.Warning, tags: ImmutableArray.Create(Completion.CompletionTags.Warning));
        }

        protected QuickInfoElement CreateQuickInfoDisplayElement(
            QuickInfoElement symbolGlyph = null,
            QuickInfoElement warningGlyph = null,
            QuickInfoElement mainDescription = null,
            QuickInfoElement documentation = null,
            QuickInfoElement typeParameterMap = null,
            QuickInfoElement anonymousTypes = null,
            QuickInfoElement usageText = null,
            QuickInfoElement exceptionText = null)
        {
            var elements = new List<QuickInfoElement>();

            if (symbolGlyph != null)
            {
                elements.Add(symbolGlyph);
            }

            if (warningGlyph != null)
            {
                elements.Add(warningGlyph);
            }

            if (mainDescription != null)
            {
                elements.Add(mainDescription);
            }

            if (documentation != null)
            {
                elements.Add(documentation);
            }

            if (typeParameterMap != null)
            {
                elements.Add(typeParameterMap);
            }

            if (anonymousTypes != null)
            {
                elements.Add(anonymousTypes);
            }

            if (usageText != null)
            {
                elements.Add(usageText);
            }

            if (exceptionText != null)
            {
                elements.Add(exceptionText);
            }

            return QuickInfoElement.Create(QuickInfoElementKinds.Group, elements: elements.ToImmutableArray());
        }

#if false
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
            return new SymbolGlyphDeferredContent(Glyph.CompletionWarning, _glyphService);
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
                symbolGlyph: new SymbolGlyphDeferredContent(glyph, _glyphService),
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
            return new SymbolGlyphDeferredContent(symbol.GetGlyph(), _glyphService);
        }

        protected IDeferredQuickInfoContent CreateClassifiableDeferredContent(
            IList<TaggedText> content)
        {
            return new ClassifiableDeferredContent(content, _typeMap);
        }

        protected IDeferredQuickInfoContent CreateDocumentationCommentDeferredContent(
            string documentationComment)
        {
            return new DocumentationCommentDeferredContent(documentationComment, _typeMap);
        }

        protected IDeferredQuickInfoContent CreateElisionBufferDeferredContent(SnapshotSpan span)
        {
            return new ElisionBufferDeferredContent(
                span, _projectionBufferFactoryService, _editorOptionsFactoryService, _textEditorFactoryService);
        }
#endif
    }
}