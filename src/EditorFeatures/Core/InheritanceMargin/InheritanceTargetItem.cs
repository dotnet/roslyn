// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    [Flags]
    internal enum InheritanceRelationship
    {
        Implementing = 0x1,
        Implemented = 0x2,
        Overriding = 0x4,
        Overriden = 0x8,
    }

    internal class InheritanceMemberItem
    {
        public readonly int LineNumber;
        public readonly TaggedText MemberDescription;
        public readonly Glyph Glyph;
        public readonly ImmutableArray<InheritanceTargetItem> TargetItems;

        public InheritanceMemberItem(
            int lineNumber,
            TaggedText memberDescription,
            Glyph glyph,
            ImmutableArray<InheritanceTargetItem> targetItems)
        {
            LineNumber = lineNumber;
            MemberDescription = memberDescription;
            Glyph = glyph;
            TargetItems = targetItems;
        }
    }

    internal class InheritanceTargetItem
    {
        public readonly TaggedText TargetDescription;
        public readonly Glyph Glyph;
        public readonly InheritanceRelationship RelationToMember;
        public readonly DefinitionItem DefinitionItem;

        public InheritanceTargetItem(
            TaggedText targetDescription,
            Glyph glyph,
            InheritanceRelationship relationToMember,
            DefinitionItem definitionItem)
        {
            TargetDescription = targetDescription;
            Glyph = glyph;
            RelationToMember = relationToMember;
            DefinitionItem = definitionItem;
        }
    }
}
