// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal sealed class InheritanceMarginItem
    {
        /// <summary>
        /// Line number used to show the margin for the member.
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Display texts for this member.
        /// </summary>
        public readonly ImmutableArray<TaggedText> DisplayTexts;

        /// <summary>
        /// Member's glyph.
        /// </summary>
        public readonly Glyph Glyph;

        /// <summary>
        /// Whether the target items are already ordered or not.
        /// </summary>
        public readonly bool IsOrdered;

        /// <summary>
        /// An array of the implementing/implemented/overriding/overridden targets for this member.
        /// </summary>
        public readonly ImmutableArray<InheritanceTargetItem> TargetItems;

        /// <summary>
        /// An array of other margin items to show under this.
        /// </summary>
        public readonly ImmutableArray<InheritanceMarginItem> NestedItems;

        public InheritanceMarginItem(
            int lineNumber,
            ImmutableArray<TaggedText> displayTexts,
            Glyph glyph,
            bool isOrdered,
            ImmutableArray<InheritanceTargetItem> targetItems,
            ImmutableArray<InheritanceMarginItem> nestedItems = default)
        {
            LineNumber = lineNumber;
            DisplayTexts = displayTexts;
            Glyph = glyph;
            TargetItems = targetItems;
            IsOrdered = isOrdered;
            NestedItems = nestedItems.NullToEmpty();
        }

        public static async ValueTask<InheritanceMarginItem> ConvertAsync(
            Solution solution,
            SerializableInheritanceMarginItem serializableItem,
            CancellationToken cancellationToken)
        {
            var targetItems = await serializableItem.TargetItems.SelectAsArrayAsync(
                (item, _) => InheritanceTargetItem.ConvertAsync(solution, item, cancellationToken), cancellationToken).ConfigureAwait(false);
            return new InheritanceMarginItem(serializableItem.LineNumber, serializableItem.DisplayTexts, serializableItem.Glyph, isOrdered: false, targetItems);
        }
    }
}
