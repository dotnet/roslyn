// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;

internal sealed class InheritanceMarginTag : IGlyphTag, IEquatable<InheritanceMarginTag>
{
    /// <summary>
    /// Margin moniker.
    /// </summary>
    public ImageMoniker Moniker { get; }

    /// <summary>
    /// Members needs to be shown on this line. There might be multiple members.
    /// For example:
    /// interface IBar { void Foo1(); void Foo2(); }
    /// class Bar : IBar { void Foo1() { } void Foo2() { } }
    /// </summary>
    public readonly ImmutableArray<InheritanceMarginItem> MembersOnLine;

    /// <summary>
    /// Used for accessibility purpose.
    /// </summary>
    public readonly int LineNumber;

    public InheritanceMarginTag(int lineNumber, ImmutableArray<InheritanceMarginItem> membersOnLine)
    {
        Contract.ThrowIfTrue(membersOnLine.IsEmpty);

        LineNumber = lineNumber;
        MembersOnLine = membersOnLine;

        // The common case is that one line has one member.
        using var _ = ArrayBuilder<InheritanceTargetItem>.GetInstance(out var allItems);
        foreach (var marginItem in membersOnLine)
            allItems.AddRange(marginItem.TargetItems);

        var relationship = allItems[0].RelationToMember;
        for (var i = 1; i < allItems.Count; i++)
            relationship |= allItems[i].RelationToMember;

        Moniker = InheritanceMarginHelpers.GetMoniker(relationship);
    }

    // Intentionally throwing, we have never supported this facility, and there is no contract around placing
    // these tags in sets or maps.
    public override int GetHashCode()
        => throw new NotImplementedException();

    public override bool Equals(object? obj)
        => Equals(obj as InheritanceMarginTag);

    public bool Equals(InheritanceMarginTag? other)
    {
        return other != null &&
            this.LineNumber == other.LineNumber &&
            this.Moniker.Guid == other.Moniker.Guid &&
            this.Moniker.Id == other.Moniker.Id &&
            this.MembersOnLine.SequenceEqual(other.MembersOnLine);
    }
}
