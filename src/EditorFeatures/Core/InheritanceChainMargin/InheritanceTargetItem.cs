// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceChainMargin
{
    [Flags]
    internal enum Relationship
    {
        Implementing = 0x1,
        Implemented = 0x2,
        Overriding = 0x4,
        Overriden = 0x8,
    }

    internal class InheritanceMemberItem
    {
        public readonly TaggedText MemberDescription;
        public readonly Glyph Glyph;
        public readonly ImmutableArray<InheritanceTargetItem> TargetItems;

        public InheritanceMemberItem(TaggedText memberDescription, Glyph glyph, ImmutableArray<InheritanceTargetItem> targetItems)
        {
            MemberDescription = memberDescription;
            Glyph = glyph;
            TargetItems = targetItems;
        }
    }

    internal class InheritanceTargetItem
    {
        public readonly TaggedText TargetDescription;

        public readonly Glyph Glyph;

        public readonly Relationship RelationToMember;

        public readonly ImmutableArray<DefinitionItem> TargetDefinitionItems;

        public InheritanceTargetItem(
            TaggedText targetDescription,
            Glyph glyph,
            Relationship relationToMember,
            ImmutableArray<DefinitionItem> targetDefinitionItems)
        {
            TargetDescription = targetDescription;
            Glyph = glyph;
            RelationToMember = relationToMember;
            TargetDefinitionItems = targetDefinitionItems;
        }
    }
}
