// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
