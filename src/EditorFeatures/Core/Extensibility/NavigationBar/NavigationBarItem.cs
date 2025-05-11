// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor;

internal abstract class NavigationBarItem(
    ITextVersion? textVersion,
    string text,
    Glyph glyph,
    ImmutableArray<TextSpan> spans,
    ImmutableArray<NavigationBarItem> childItems = default,
    int indent = 0,
    bool bolded = false,
    bool grayed = false) : IEquatable<NavigationBarItem>
{
    public string Text { get; } = text;
    public Glyph Glyph { get; } = glyph;
    public bool Bolded { get; } = bolded;
    public bool Grayed { get; } = grayed;
    public int Indent { get; } = indent;
    public ImmutableArray<NavigationBarItem> ChildItems { get; } = childItems.NullToEmpty();

    /// <summary>
    /// The spans in the owning document corresponding to this nav bar item.  If the user's caret enters one of
    /// these spans, we'll select that item in the nav bar (except if they're in an item's span that is nested
    /// within this).
    /// </summary>
    /// <remarks>This can be empty for items whose location is in another document.</remarks>
    public ImmutableArray<TextSpan> Spans { get; } = spans;

    internal ITextVersion? TextVersion { get; } = textVersion;

    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public bool Equals(NavigationBarItem? other)
    {
        return other != null &&
               Text == other.Text &&
               Glyph == other.Glyph &&
               Bolded == other.Bolded &&
               Grayed == other.Grayed &&
               Indent == other.Indent &&
               ChildItems.SequenceEqual(other.ChildItems) &&
               Spans.SequenceEqual(other.Spans);
    }
}

internal static class NavigationBarItemExtensions
{
    public static TextSpan GetCurrentItemSpan(this NavigationBarItem item, ITextVersion toVersion, TextSpan span)
    {
        Contract.ThrowIfNull(item.TextVersion, "This should only be called for locations the caller knows to be in the open file");
        return item.TextVersion.CreateTrackingSpan(span.ToSpan(), SpanTrackingMode.EdgeExclusive)
                               .GetSpan(toVersion)
                               .ToTextSpan();
    }
}
