// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;

internal partial class InheritanceGlyphManager
{
    private sealed record GlyphData
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
        public TextSpan GetSpan(GlyphData data)
            => data.SnapshotSpan.Span.ToTextSpan();
    }
}
