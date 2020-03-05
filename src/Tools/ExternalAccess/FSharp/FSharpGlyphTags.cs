﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
