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
        public readonly ImmutableArray<TaggedText> DisplayTexts;

        [DataMember(Order = 2)]
        public readonly Glyph Glyph;

        [DataMember(Order = 3)]
        public readonly ImmutableArray<SerializableInheritanceTargetItem> TargetItems;

        public SerializableInheritanceMarginItem(int lineNumber, ImmutableArray<TaggedText> displayTexts, Glyph glyph, ImmutableArray<SerializableInheritanceTargetItem> targetItems)
        {
            LineNumber = lineNumber;
            DisplayTexts = displayTexts;
            Glyph = glyph;
            TargetItems = targetItems;
        }
    }
}
