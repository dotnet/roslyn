// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

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
        public IDeferredQuickInfoContent WarningGlyph { get; }

        // DO NOT REMOVE: compat for Typescript
        public QuickInfoDisplayDeferredContent(
            IDeferredQuickInfoContent symbolGlyph,
            IDeferredQuickInfoContent warningGlyph,
            IDeferredQuickInfoContent mainDescription,
            IDeferredQuickInfoContent documentation,
            IDeferredQuickInfoContent typeParameterMap,
            IDeferredQuickInfoContent anonymousTypes,
            IDeferredQuickInfoContent usageText,
            IDeferredQuickInfoContent exceptionText)
            : this(
                  symbolGlyph,
                  warningGlyph,
                  mainDescription,
                  documentation,
                  typeParameterMap,
                  anonymousTypes,
                  usageText,
                  exceptionText,
                  capturesText: new ClassifiableDeferredContent(new List<TaggedText>()))
        {
        }

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
            SymbolGlyph = symbolGlyph;
            WarningGlyph = warningGlyph;
            MainDescription = mainDescription;
            Documentation = documentation;
            TypeParameterMap = typeParameterMap;
            AnonymousTypes = anonymousTypes;
            UsageText = usageText;
            ExceptionText = exceptionText;
        }
    }
}
