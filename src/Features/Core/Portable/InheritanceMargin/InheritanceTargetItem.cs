// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// The glyph for source language. Used to disambiguate results when multiple targets have the same name.
        /// </summary>
        public readonly Glyph LanguageGlyph;

        /// <summary>
        /// The display name used in margin.
        /// </summary>
        public readonly string DisplayName;

        /// <summary>
        /// Name of the project the symbol is defined in (if known).  Used to disambiguate results when multiple targets
        /// have the same name and same language.
        /// </summary>
        public readonly string? ProjectName;

        public InheritanceTargetItem(
            InheritanceRelationship relationToMember,
            DefinitionItem.DetachedDefinitionItem definitionItem,
            Glyph glyph,
            Glyph languageGlyph,
            string displayName,
            string? projectName)
        {
            RelationToMember = relationToMember;
            DefinitionItem = definitionItem;
            Glyph = glyph;
            LanguageGlyph = languageGlyph;
            DisplayName = displayName;
            ProjectName = projectName;
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
                serializableItem.LanguageGlyph,
                serializableItem.DisplayName,
                serializableItem.ProjectName);
        }
    }
}
