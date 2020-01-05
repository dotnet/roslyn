// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp
{
    internal static class FSharpGlyphTags
    {
        public static ImmutableArray<string> GetTags(FSharpGlyph glyph)
        {
            return GlyphTags.GetTags(FSharpGlyphHelpers.ConvertTo(glyph));
        }
    }
}
