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
        public readonly string DisplayName;

        public SerializableInheritanceTargetItem(
            InheritanceRelationship relationToMember,
            SerializableDefinitionItem definitionItem,
            Glyph glyph,
            string displayName)
        {
            RelationToMember = relationToMember;
            DefinitionItem = definitionItem;
            Glyph = glyph;
            DisplayName = displayName;
        }
    }
}
