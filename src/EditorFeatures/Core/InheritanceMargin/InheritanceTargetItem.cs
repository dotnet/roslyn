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
        /// The display name used in margin.
        /// </summary>
        public readonly string DisplayName;

        /// <summary>
        /// The language of the symbol. Used to disambiguate results when multiple targets have the same name.
        /// </summary>
        public readonly string LanguageName;

        /// <summary>
        /// Name of the project the symbol is defined in (if known).  Used to disambiguate results when multiple targets
        /// have the same name and same language.
        /// </summary>
        public readonly string? ProjectName;

        /// <summary>
        /// The glyph for source language.
        /// </summary>
        public readonly Glyph LanguageGlyph;

        public InheritanceTargetItem(
            InheritanceRelationship relationToMember,
            DefinitionItem.DetachedDefinitionItem definitionItem,
            Glyph glyph,
            string displayName,
            string languageName,
            string? projectName,
            Glyph languageGlyph)
        {
            RelationToMember = relationToMember;
            DefinitionItem = definitionItem;
            Glyph = glyph;
            DisplayName = displayName;
            LanguageName = languageName;
            ProjectName = projectName;
            LanguageGlyph = languageGlyph;
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
                serializableItem.DisplayName,
                serializableItem.LanguageName,
                serializableItem.ProjectName,
                serializableItem.LanguageGlyph);
        }
    }
}
