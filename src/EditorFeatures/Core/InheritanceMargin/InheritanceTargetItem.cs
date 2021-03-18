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
        /// A default case that should not be used.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Indicate the target is implementing the member. It would be shown as I↑.
        /// </summary>
        Implementing = 0x1,

        /// <summary>
        /// Indicate the target is implemented by the member. It would be shown as I↓.
        /// </summary>
        Implemented = 0x2,

        /// <summary>
        /// Indicate the target is overriding the member. It would be shown as O↑.
        /// </summary>
        Overriding = 0x4,

        /// <summary>
        /// Indicate the target is overridden by the member. It would be shown as O↓.
        /// </summary>
        Overridden = 0x8,
    }

    internal readonly struct InheritanceMemberItem
    {
        /// <summary>
        /// Line number used to show the margin for the member.
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Member's display name.
        /// </summary>
        public readonly string MemberDisplayName;

        /// <summary>
        /// Member's glyph
        /// </summary>
        public readonly Glyph Glyph;

        /// <summary>
        /// An array of the implementing/implemented/overriding/overriden targets for this member
        /// </summary>
        public readonly ImmutableArray<InheritanceTargetItem> TargetItems;

        public InheritanceMemberItem(
            int lineNumber,
            string memberDisplayName,
            Glyph glyph,
            ImmutableArray<InheritanceTargetItem> targetItems)
        {
            LineNumber = lineNumber;
            MemberDisplayName = memberDisplayName;
            Glyph = glyph;
            TargetItems = targetItems;
        }
    }

    /// <summary>
    /// Information used to decided the margin image and responsible for performing navigations
    /// </summary>
    internal readonly struct InheritanceTargetItem
    {
        /// <summary>
        /// Indicate the inheritance relationship between the target and member.
        /// </summary>
        public readonly InheritanceRelationship RelationToMember;

        /// <summary>
        /// DefinitionItem used to display the additional information and performs navigation.
        /// </summary>
        public readonly DefinitionItem DefinitionItem;

        public InheritanceTargetItem(
            InheritanceRelationship relationToMember,
            DefinitionItem definitionItem)
        {
            RelationToMember = relationToMember;
            DefinitionItem = definitionItem;
        }
    }
}
