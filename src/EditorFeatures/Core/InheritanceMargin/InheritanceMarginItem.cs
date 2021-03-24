// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal readonly struct InheritanceMarginItem
    {
        /// <summary>
        /// Line number used to show the margin for the member.
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Member's display name.
        /// </summary>
        public readonly string DisplayName;

        /// <summary>
        /// Member's glyph.
        /// </summary>
        public readonly Glyph Glyph;

        /// <summary>
        /// An array of the implementing/implemented/overriding/overridden targets for this member.
        /// </summary>
        public readonly ImmutableArray<InheritanceTargetItem> TargetItems;

        public InheritanceMarginItem(
            int lineNumber,
            string displayName,
            Glyph glyph,
            ImmutableArray<InheritanceTargetItem> targetItems)
        {
            LineNumber = lineNumber;
            DisplayName = displayName;
            Glyph = glyph;
            TargetItems = targetItems;
        }
    }
}
