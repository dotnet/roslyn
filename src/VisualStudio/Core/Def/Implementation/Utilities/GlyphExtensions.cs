// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class GlyphExtensions
    {
        public static ushort GetGlyphIndex(this Glyph glyph)
        {
            var glyphGroup = glyph.GetStandardGlyphGroup();
            var glyphItem = glyph.GetStandardGlyphItem();

            return glyphGroup < StandardGlyphGroup.GlyphGroupError
                ? (ushort)((int)glyphGroup + (int)glyphItem)
                : (ushort)glyphGroup;
        }
    }
}
