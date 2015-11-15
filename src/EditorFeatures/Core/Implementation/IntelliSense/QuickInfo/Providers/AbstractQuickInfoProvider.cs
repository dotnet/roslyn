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
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IGlyphService _glyphService;
        private readonly ClassificationTypeMap _typeMap;

        protected AbstractQuickInfoProvider(
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IGlyphService glyphService,
            ClassificationTypeMap typeMap)
        {
            _textBufferFactoryService = textBufferFactoryService;
            _contentTypeRegistryService = contentTypeRegistryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _glyphService = glyphService;
            _typeMap = typeMap;
        }

        public async Task<QuickInfoItem> GetItemAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.GetTouchingToken(position, cancellationToken, findInsideTrivia: true);

            QuickInfoItem state;
            if ((state = await GetQuickInfoItemAsync(document, token, position, cancellationToken).ConfigureAwait(false)) != null)
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
            if (token != default(SyntaxToken) &&
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
            IList<SymbolDisplayPart> mainDescription,
            IDeferredQuickInfoContent documentation,
            IList<SymbolDisplayPart> typeParameterMap,
            IList<SymbolDisplayPart> anonymousTypes,
            IList<SymbolDisplayPart> usageText,
            IList<SymbolDisplayPart> exceptionText)
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
            IList<SymbolDisplayPart> mainDescription,
            IDeferredQuickInfoContent documentation,
            IList<SymbolDisplayPart> typeParameterMap,
            IList<SymbolDisplayPart> anonymousTypes,
            IList<SymbolDisplayPart> usageText,
            IList<SymbolDisplayPart> exceptionText)
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

        protected IDeferredQuickInfoContent CreateClassifiableDeferredContent(IList<SymbolDisplayPart> content)
        {
            return new ClassifiableDeferredContent(
                content, _textBufferFactoryService, _contentTypeRegistryService, _typeMap);
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
    }
}
