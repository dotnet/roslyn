// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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
        public readonly DefinitionItem.DetachedDefinitionItem DefinitionItem;

        /// <summary>
        /// The glyph for this target.
        /// </summary>
        public readonly Glyph Glyph;

        /// <summary>
        /// TaggedTexts used to show the colorized display name.
        /// </summary>
        public readonly ImmutableArray<TaggedText> DisplayTaggedTexts;

        public InheritanceTargetItem(
            InheritanceRelationship relationToMember,
            DefinitionItem.DetachedDefinitionItem definitionItem,
            Glyph glyph,
            ImmutableArray<TaggedText> displayTaggedTexts)
        {
            RelationToMember = relationToMember;
            DefinitionItem = definitionItem;
            Glyph = glyph;
            DisplayTaggedTexts = displayTaggedTexts;
        }

        public static async ValueTask<InheritanceTargetItem> ConvertAsync(
            Solution solution,
            SerializableInheritanceTargetItem serializableItem,
            CancellationToken cancellationToken)
        {
            var definitionItem = await serializableItem.DefinitionItem.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);

            // detach this item so that it doesn't hold onto a full solution snapshot in other documents that
            // are not getting updated.
            return new InheritanceTargetItem(
                serializableItem.RelationToMember,
                definitionItem.Detach(),
                serializableItem.Glyph,
                serializableItem.DisplayTaggedTexts);
        }
    }
}
