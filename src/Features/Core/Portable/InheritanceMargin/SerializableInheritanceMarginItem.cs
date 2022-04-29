// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    [DataContract]
    internal readonly struct SerializableInheritanceMarginItem
    {
        [DataMember(Order = 0)]
        public readonly int LineNumber;

        [DataMember(Order = 1)]
        public readonly string? TopLevelDisplayText;

        [DataMember(Order = 2)]
        public readonly ImmutableArray<TaggedText> DisplayTexts;

        [DataMember(Order = 3)]
        public readonly Glyph Glyph;

        [DataMember(Order = 4)]
        public readonly bool IsOrdered;

        [DataMember(Order = 5)]
        public readonly ImmutableArray<SerializableInheritanceTargetItem> TargetItems;

        public SerializableInheritanceMarginItem(
            int lineNumber,
            string? topLevelDisplayText,
            ImmutableArray<TaggedText> displayTexts,
            Glyph glyph,
            bool isOrdered,
            ImmutableArray<SerializableInheritanceTargetItem> targetItems)
        {
            LineNumber = lineNumber;
            TopLevelDisplayText = topLevelDisplayText;
            DisplayTexts = displayTexts;
            Glyph = glyph;
            IsOrdered = isOrdered;
            TargetItems = targetItems;
        }
    }
}
