// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Navigation;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
#endif

internal class FSharpNavigableItem
{
    public FSharpNavigableItem(FSharpGlyph glyph, ImmutableArray<TaggedText> displayTaggedParts, Document document, TextSpan sourceSpan)
    {
        Glyph = glyph;
        DisplayTaggedParts = displayTaggedParts;
        Document = document;
        SourceSpan = sourceSpan;
    }

    public FSharpGlyph Glyph { get; }

    public ImmutableArray<TaggedText> DisplayTaggedParts { get; }

    public Document Document { get; }

    public TextSpan SourceSpan { get; }
}
