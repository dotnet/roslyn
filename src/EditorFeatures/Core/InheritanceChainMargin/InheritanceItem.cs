// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.InheritanceChainMargin
{
    internal enum Relationship
    {
        SubType,
        BaseType,
        Implement,
        Override,
        Overriden,
    }

    internal class InheritanceInfo
    {
        public readonly TaggedText MemberTaggedText;

        public readonly int GlyphLineNumber;

        public readonly ImmutableArray<InheritanceItem> InheritanceItems;

        public InheritanceInfo(
            TaggedText memberTaggedText,
            int glyphLineNumber,
            ImmutableArray<InheritanceItem> inheritanceItems)
        {
            MemberTaggedText = memberTaggedText;
            GlyphLineNumber = glyphLineNumber;
            InheritanceItems = inheritanceItems;
        }
    }

    internal class InheritanceItem
    {
        public readonly TaggedText MemberTaggedText;

        public readonly Relationship RelationToMember;

        public readonly ImmutableArray<InheritanceItemNavigator> Navigators;

        public InheritanceItem(
            TaggedText memberTaggedText,
            Relationship relationToMember,
            ImmutableArray<InheritanceItemNavigator> navigators)
        {
            MemberTaggedText = memberTaggedText;
            RelationToMember = relationToMember;
            Navigators = navigators;
        }
    }

    internal class InheritanceItemNavigator
    {
        public readonly TaggedText Descriptions;
        public readonly Func<Task> NavigationFunc;

        public InheritanceItemNavigator(TaggedText descriptions, Func<Task> navigationFunc)
        {
            Descriptions = descriptions;
            NavigationFunc = navigationFunc;
        }
    }
}
