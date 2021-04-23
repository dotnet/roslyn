// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal readonly struct InheritanceMarginItem
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
        /// An array of the implementing/implemented/overriding/overridden targets for this member.
        /// </summary>
        public readonly ImmutableArray<InheritanceTargetItem> TargetItems;

        public InheritanceMarginItem(
            int lineNumber,
            ImmutableArray<TaggedText> displayTexts,
            Glyph glyph,
            ImmutableArray<InheritanceTargetItem> targetItems)
        {
            LineNumber = lineNumber;
            DisplayTexts = displayTexts;
            Glyph = glyph;
            TargetItems = targetItems;
        }

        public static async ValueTask<InheritanceMarginItem> ConvertAsync(
            Solution solution,
            SerializableInheritanceMarginItem serializableItem,
            CancellationToken cancellationToken)
        {
            var targetItems = await serializableItem.TargetItems.SelectAsArrayAsync(
                    (item, _) => InheritanceTargetItem.ConvertAsync(solution, item, cancellationToken), cancellationToken).ConfigureAwait(false);
            return new InheritanceMarginItem(serializableItem.LineNumber, serializableItem.DisplayTexts, serializableItem.Glyph, targetItems);
        }
    }
}
