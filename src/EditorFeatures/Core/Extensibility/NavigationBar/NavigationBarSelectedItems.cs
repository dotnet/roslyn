// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor
{
    internal class NavigationBarSelectedTypeAndMember
    {
        public NavigationBarItem TypeItem { get; }
        public bool ShowTypeItemGrayed { get; }
        public NavigationBarItem MemberItem { get; }
        public bool ShowMemberItemGrayed { get; }

        public NavigationBarSelectedTypeAndMember(NavigationBarItem typeItem, NavigationBarItem memberItem)
        {
            TypeItem = typeItem;
            MemberItem = memberItem;
        }

        public NavigationBarSelectedTypeAndMember(
            NavigationBarItem typeItem,
            bool showTypeItemGrayed,
            NavigationBarItem memberItem,
            bool showMemberItemGrayed)
            : this(typeItem, memberItem)
        {
            ShowTypeItemGrayed = showTypeItemGrayed;
            ShowMemberItemGrayed = showMemberItemGrayed;
        }
    }
}
