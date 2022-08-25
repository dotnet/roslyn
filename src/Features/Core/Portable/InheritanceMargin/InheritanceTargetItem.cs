// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    /// <summary>
    /// Information used to decided the margin image and responsible for performing navigations
    /// </summary>
    [DataContract]
    internal readonly struct InheritanceTargetItem
    {
        /// <summary>
        /// Indicate the inheritance relationship between the target and member.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly InheritanceRelationship RelationToMember;

        /// <summary>
        /// DefinitionItem used to display the additional information and performs navigation.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly DetachedDefinitionItem DefinitionItem;

        /// <summary>
        /// The glyph for this target.
        /// </summary>
        [DataMember(Order = 2)]
        public readonly Glyph Glyph;

        /// <summary>
        /// The glyph for source language. Used to disambiguate results when multiple targets have the same name.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly Glyph LanguageGlyph;

        /// <summary>
        /// The display name used in margin.
        /// </summary>
        [DataMember(Order = 4)]
        public readonly string DisplayName;

        /// <summary>
        /// Name of the project the symbol is defined in (if known).  Used to disambiguate results when multiple targets
        /// have the same name and same language.
        /// </summary>
        [DataMember(Order = 5)]
        public readonly string? ProjectName;

        public InheritanceTargetItem(
            InheritanceRelationship relationToMember,
            DetachedDefinitionItem definitionItem,
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
    }
}
