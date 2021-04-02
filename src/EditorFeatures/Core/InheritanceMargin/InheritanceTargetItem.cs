// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
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

        /// <summary>
        /// The glyph for this target.
        /// </summary>
        public readonly Glyph Glyph;

        /// <summary>
        /// The name for containing type of this target. Empty if it is global namespace.
        /// </summary>
        public readonly string DisplayNameForContainingType;

        public InheritanceTargetItem(
            InheritanceRelationship relationToMember,
            DefinitionItem definitionItem,
            Glyph glyph,
            string displayNameForContainingType)
        {
            RelationToMember = relationToMember;
            DefinitionItem = definitionItem;
            Glyph = glyph;
            DisplayNameForContainingType = displayNameForContainingType;
        }
    }
}
