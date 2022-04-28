// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    [DataContract]
    internal readonly struct SerializableInheritanceTargetItem
    {
        [DataMember(Order = 0)]
        public readonly InheritanceRelationship RelationToMember;

        [DataMember(Order = 1)]
        public readonly SerializableDefinitionItem DefinitionItem;

        [DataMember(Order = 2)]
        public readonly Glyph Glyph;

        [DataMember(Order = 3)]
        public readonly Glyph LanguageGlyph;

        [DataMember(Order = 4)]
        public readonly string DisplayName;

        [DataMember(Order = 5)]
        public readonly string? ProjectName;

        public SerializableInheritanceTargetItem(
            InheritanceRelationship relationToMember,
            SerializableDefinitionItem definitionItem,
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
