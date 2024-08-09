// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor;

internal sealed class NavigationBarSelectedTypeAndMember(
    NavigationBarItem? typeItem,
    bool showTypeItemGrayed,
    NavigationBarItem? memberItem,
    bool showMemberItemGrayed) : IEquatable<NavigationBarSelectedTypeAndMember>
{
    public static readonly NavigationBarSelectedTypeAndMember Empty = new(typeItem: null, memberItem: null);

    public NavigationBarItem? TypeItem { get; } = typeItem;
    public bool ShowTypeItemGrayed { get; } = showTypeItemGrayed;
    public NavigationBarItem? MemberItem { get; } = memberItem;
    public bool ShowMemberItemGrayed { get; } = showMemberItemGrayed;

    public NavigationBarSelectedTypeAndMember(NavigationBarItem? typeItem, NavigationBarItem? memberItem)
        : this(typeItem, showTypeItemGrayed: false, memberItem, showMemberItemGrayed: false)
    {
    }

    public override bool Equals(object? obj)
        => Equals(obj as NavigationBarSelectedTypeAndMember);

    public bool Equals(NavigationBarSelectedTypeAndMember? other)
        => other != null &&
           this.ShowTypeItemGrayed == other.ShowTypeItemGrayed &&
           this.ShowMemberItemGrayed == other.ShowMemberItemGrayed &&
           Equals(this.TypeItem, other.TypeItem) &&
           Equals(this.MemberItem, other.MemberItem);

    public override int GetHashCode()
        => throw new NotImplementedException();
}
