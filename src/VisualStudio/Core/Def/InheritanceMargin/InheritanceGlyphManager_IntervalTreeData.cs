// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;

internal partial class InheritanceGlyphManager
{
    private record GlyphData
    {
        public SnapshotSpan SnapshotSpan { get; }
        public InheritanceMarginGlyph Glyph { get; }

        public GlyphData(SnapshotSpan snapshotSpan, InheritanceMarginGlyph glyph)
        {
            SnapshotSpan = snapshotSpan;
            Glyph = glyph;
        }

        public void Deconstruct(out SnapshotSpan span, out InheritanceMarginGlyph glyph)
        {
            span = SnapshotSpan;
            glyph = Glyph;
        }
    }

    private readonly struct GlyphDataIntrospector : IIntervalIntrospector<GlyphData>
    {
        public int GetStart(GlyphData data)
            => data.SnapshotSpan.Start;

        public int GetLength(GlyphData data)
            => data.SnapshotSpan.Length;
    }
}
