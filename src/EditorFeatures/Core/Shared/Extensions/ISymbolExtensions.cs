// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class ISymbolExtensions
    {
        public static StandardGlyphGroup GetStandardGlyphGroup(this ISymbol symbol)
        {
            return symbol.GetGlyph().GetStandardGlyphGroup();
        }

        public static StandardGlyphItem GetStandardGlyphItem(this ISymbol symbol)
        {
            return symbol.GetGlyph().GetStandardGlyphItem();
        }
    }
}
