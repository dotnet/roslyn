// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class QuickInfoDisplayDeferredContent : IDeferredQuickInfoContent
    {
        public IDeferredQuickInfoContent SymbolGlyph { get; }
        public IDeferredQuickInfoContent MainDescription { get; }
        public IDeferredQuickInfoContent Documentation { get; }
        public IDeferredQuickInfoContent TypeParameterMap { get; }
        public IDeferredQuickInfoContent AnonymousTypes { get; }
        public IDeferredQuickInfoContent UsageText { get; }
        public IDeferredQuickInfoContent ExceptionText { get; }
        public IDeferredQuickInfoContent CapturesText { get; }
        public IDeferredQuickInfoContent WarningGlyph { get; }

        public QuickInfoDisplayDeferredContent(
            IDeferredQuickInfoContent symbolGlyph,
            IDeferredQuickInfoContent warningGlyph,
            IDeferredQuickInfoContent mainDescription,
            IDeferredQuickInfoContent documentation,
            IDeferredQuickInfoContent typeParameterMap,
            IDeferredQuickInfoContent anonymousTypes,
            IDeferredQuickInfoContent usageText,
            IDeferredQuickInfoContent exceptionText,
            IDeferredQuickInfoContent capturesText)
        {
            SymbolGlyph = symbolGlyph;
            WarningGlyph = warningGlyph;
            MainDescription = mainDescription;
            Documentation = documentation;
            TypeParameterMap = typeParameterMap;
            AnonymousTypes = anonymousTypes;
            UsageText = usageText;
            ExceptionText = exceptionText;
            CapturesText = capturesText;
        }
    }
}
