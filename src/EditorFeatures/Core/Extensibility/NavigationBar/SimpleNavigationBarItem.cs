// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal sealed class SimpleNavigationBarItem(ITextVersion textVersion, string text, Glyph glyph, ImmutableArray<TextSpan> spans, ImmutableArray<NavigationBarItem> childItems, int indent, bool bolded, bool grayed) : NavigationBarItem(textVersion, text, glyph, spans, childItems, indent, bolded, grayed), IEquatable<SimpleNavigationBarItem>
    {
        public override bool Equals(object? obj)
            => Equals(obj as SimpleNavigationBarItem);

        public bool Equals(SimpleNavigationBarItem? other)
            => base.Equals(other);

        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}
