// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin;

[DataContract]
internal readonly struct InheritanceMarginItem(
    int lineNumber,
    string? topLevelDisplayText,
    ImmutableArray<TaggedText> displayTexts,
    Glyph glyph,
    ImmutableArray<InheritanceTargetItem> targetItems) : IEquatable<InheritanceMarginItem>
{
    /// <summary>
    /// Line number used to show the margin for the member.
    /// </summary>
    [DataMember(Order = 0)]
    public readonly int LineNumber = lineNumber;

    /// <summary>
    /// Special display text to show when showing the 'hover' tip for a margin item.  Used to override the default
    /// text we show that says "'X' is inherited".  Used currently for showing information about top-level-imports.
    /// </summary>
    [DataMember(Order = 1)]
    public readonly string? TopLevelDisplayText = topLevelDisplayText;

    /// <summary>
    /// Display texts for this member.
    /// </summary>
    [DataMember(Order = 2)]
    public readonly ImmutableArray<TaggedText> DisplayTexts = displayTexts;

    /// <summary>
    /// Member's glyph.
    /// </summary>
    [DataMember(Order = 3)]
    public readonly Glyph Glyph = glyph;

    /// <summary>
    /// An array of the implementing/implemented/overriding/overridden targets for this member.
    /// </summary>
    [DataMember(Order = 4)]
    public readonly ImmutableArray<InheritanceTargetItem> TargetItems = targetItems;

    public override int GetHashCode()
        => throw ExceptionUtilities.Unreachable();

    public override bool Equals(object? obj)
        => obj is InheritanceMarginItem item && Equals(item);

    public bool Equals(InheritanceMarginItem other)
        => this.LineNumber == other.LineNumber &&
           this.TopLevelDisplayText == other.TopLevelDisplayText &&
           this.Glyph == other.Glyph &&
           this.DisplayTexts.SequenceEqual(other.DisplayTexts) &&
           this.TargetItems.SequenceEqual(other.TargetItems);

    public static InheritanceMarginItem? CreateOrdered(
        int lineNumber,
        string? topLevelDisplayText,
        ImmutableArray<TaggedText> displayTexts,
        Glyph glyph,
        ImmutableArray<InheritanceTargetItem> targetItems)
        => targetItems.IsEmpty ? null : new(lineNumber, topLevelDisplayText, displayTexts, glyph, Order(targetItems));

    public static ImmutableArray<InheritanceTargetItem> Order(ImmutableArray<InheritanceTargetItem> targetItems)
        => [.. targetItems.OrderBy(t => t.DisplayName).ThenByDescending(t => t.LanguageGlyph).ThenBy(t => t.ProjectName ?? "")];
}
