// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    /// <summary>
    /// Indicate the relationship between the member and its inheritance target
    /// </summary>
    [Flags]
    internal enum InheritanceRelationship
    {
        /// <summary>
        /// Indicate the target is implementing the member.
        /// e.g.
        /// 1. interface
        /// </summary>
        Implementing = 0x1,

        /// <summary>
        /// Indicate the target is implemented by the member. Also include class inherits interface and class inherit class
        /// </summary>
        Implemented = 0x2,

        /// <summary>
        /// Indicate the target is overriding by the member.
        /// e.g.
        ///
        /// </summary>
        Overriding = 0x4,

        /// <summary>
        ///
        /// </summary>
        Overriden = 0x8,
    }

    internal readonly struct InheritanceMemberItem
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

    internal readonly struct InheritanceTargetItem
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
