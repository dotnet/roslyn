// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class QuickInfoDisplayDeferredContent : IDeferredQuickInfoContent
    {
        private readonly IDeferredQuickInfoContent _symbolGlyph;
        private readonly IDeferredQuickInfoContent _mainDescription;
        private readonly IDeferredQuickInfoContent _documentation;
        private readonly IDeferredQuickInfoContent _typeParameterMap;
        private readonly IDeferredQuickInfoContent _anonymousTypes;
        private readonly IDeferredQuickInfoContent _usageText;
        private readonly IDeferredQuickInfoContent _exceptionText;
        private readonly IDeferredQuickInfoContent _warningGlyph;

        public QuickInfoDisplayDeferredContent(
            IDeferredQuickInfoContent symbolGlyph,
            IDeferredQuickInfoContent warningGlyph,
            IDeferredQuickInfoContent mainDescription,
            IDeferredQuickInfoContent documentation,
            IDeferredQuickInfoContent typeParameterMap,
            IDeferredQuickInfoContent anonymousTypes,
            IDeferredQuickInfoContent usageText,
            IDeferredQuickInfoContent exceptionText)
        {
            _symbolGlyph = symbolGlyph;
            _warningGlyph = warningGlyph;
            _mainDescription = mainDescription;
            _documentation = documentation;
            _typeParameterMap = typeParameterMap;
            _anonymousTypes = anonymousTypes;
            _usageText = usageText;
            _exceptionText = exceptionText;
        }

        public FrameworkElement Create()
        {
            // Workaround: Dev 12 spring update added the "GlyphCompletionWarning" group for the 
            // linked files warning glyph. However, that code hasn't yet merged into VSPro_Platform 
            // from VSClient_1. For now, we'll wrap the calls to IGlyphService in a try catch, 
            // which will be removed when the relevant code has moved over.
            FrameworkElement warningGlyphElement = null;
            if (_warningGlyph != null)
            {
                try
                {
                    warningGlyphElement = _warningGlyph.Create();
                }
                catch (ArgumentException)
                {
                }
            }

            FrameworkElement symbolGlyphElement = null;
            if (_symbolGlyph != null)
            {
                symbolGlyphElement = _symbolGlyph.Create();
            }

            return new QuickInfoDisplayPanel(
                symbolGlyphElement,
                warningGlyphElement,
                _mainDescription.Create(),
                _documentation.Create(),
                _typeParameterMap.Create(),
                _anonymousTypes.Create(),
                _usageText.Create(),
                _exceptionText.Create());
        }

        // For testing...
        internal ClassifiableDeferredContent MainDescription
        {
            get
            {
                return (ClassifiableDeferredContent)_mainDescription;
            }
        }

        internal IDeferredQuickInfoContent Documentation
        {
            get
            {
                return _documentation;
            }
        }

        internal ClassifiableDeferredContent TypeParameterMap
        {
            get
            {
                return (ClassifiableDeferredContent)_typeParameterMap;
            }
        }

        internal ClassifiableDeferredContent AnonymousTypes
        {
            get
            {
                return (ClassifiableDeferredContent)_anonymousTypes;
            }
        }

        internal ClassifiableDeferredContent UsageText
        {
            get
            {
                return (ClassifiableDeferredContent)_usageText;
            }
        }

        internal ClassifiableDeferredContent ExceptionText
        {
            get
            {
                return (ClassifiableDeferredContent)_exceptionText;
            }
        }

        internal SymbolGlyphDeferredContent SymbolGlyph
        {
            get
            {
                return (SymbolGlyphDeferredContent)_symbolGlyph;
            }
        }

        internal SymbolGlyphDeferredContent WarningGlyph
        {
            get
            {
                return (SymbolGlyphDeferredContent)_warningGlyph;
            }
        }
    }
}
