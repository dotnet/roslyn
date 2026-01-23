// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis.ExternalAccess.FSharp;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal class FSharpNavigationBarItem
{
    public string Text { get; }
    public FSharpGlyph Glyph { get; }
    public bool Bolded { get; }
    public bool Grayed { get; }
    public int Indent { get; }
    public IList<FSharpNavigationBarItem> ChildItems { get; }

    public IList<TextSpan> Spans { get; internal set; }
    internal IList<ITrackingSpan> TrackingSpans { get; set; }

    public FSharpNavigationBarItem(
        string text,
        FSharpGlyph glyph,
        IList<TextSpan> spans,
        IList<FSharpNavigationBarItem> childItems = null,
        int indent = 0,
        bool bolded = false,
        bool grayed = false)
    {
        this.Text = text;
        this.Glyph = glyph;
        this.Spans = spans;
        this.ChildItems = childItems ?? SpecializedCollections.EmptyList<FSharpNavigationBarItem>();
        this.Indent = indent;
        this.Bolded = bolded;
        this.Grayed = grayed;
    }
}
