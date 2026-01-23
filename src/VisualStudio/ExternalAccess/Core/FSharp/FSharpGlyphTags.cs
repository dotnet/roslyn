// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

#if Unified_ExternalAccess
using Microsoft.VisualStudio.ExternalAccess.FSharp.Internal;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal;
#endif

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp;

internal static class FSharpGlyphTags
{
    public static ImmutableArray<string> GetTags(FSharpGlyph glyph)
    {
        return GlyphTags.GetTags(FSharpGlyphHelpers.ConvertTo(glyph));
    }
}
