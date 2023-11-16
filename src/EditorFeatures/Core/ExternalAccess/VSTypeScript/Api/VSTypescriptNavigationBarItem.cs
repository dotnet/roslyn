// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal class VSTypescriptNavigationBarItem(
        string text,
        VSTypeScriptGlyph glyph,
        ImmutableArray<TextSpan> spans,
        ImmutableArray<VSTypescriptNavigationBarItem> childItems = default,
        int indent = 0,
        bool bolded = false,
        bool grayed = false)
    {
        public string Text { get; } = text;
        public VSTypeScriptGlyph Glyph { get; } = glyph;
        public bool Bolded { get; } = bolded;
        public bool Grayed { get; } = grayed;
        public int Indent { get; } = indent;
        public ImmutableArray<VSTypescriptNavigationBarItem> ChildItems { get; } = childItems.NullToEmpty();
        public ImmutableArray<TextSpan> Spans { get; } = spans.NullToEmpty();
    }
}
