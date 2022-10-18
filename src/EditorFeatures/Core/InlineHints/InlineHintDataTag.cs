// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The simple tag that only holds information regarding the associated parameter name
    /// for the argument
    /// </summary>
    internal sealed class InlineHintDataTag : ITag, IEquatable<InlineHintDataTag>
    {
        private readonly InlineHintsDataTaggerProvider _provider;

        /// <summary>
        /// The snapshot this tag was created against.
        /// </summary>
        private readonly ITextSnapshot _snapshot;

        public readonly InlineHint Hint;

        public InlineHintDataTag(InlineHintsDataTaggerProvider provider, ITextSnapshot snapshot, InlineHint hint)
        {
            _provider = provider;
            _snapshot = snapshot;
            Hint = hint;
        }

        public override int GetHashCode()
            => throw ExceptionUtilities.Unreachable();

        public override bool Equals(object? obj)
            => obj is InlineHintDataTag tag && Equals(tag);

        public bool Equals(InlineHintDataTag? other)
        {
            if (other is null)
                return false;

            // We arbitrarily choose to map from tag1's snapshot to tag2's.
            var snapshotToMapTo = other._snapshot;

            // they have to match if they're going to change text.
            if (this.Hint.ReplacementTextChange is null != other.Hint.ReplacementTextChange is null)
                return false;

            // the text change text has to match.
            if (this.Hint.ReplacementTextChange?.NewText != other.Hint.ReplacementTextChange?.NewText)
                return false;

            // Ensure both hints are talking about the same snapshot.
            var span1 = this.Hint.Span.ToSnapshotSpan(_snapshot).TranslateTo(snapshotToMapTo, _provider.SpanTrackingMode);
            var span2 = other.Hint.Span.ToSpan();

            if (span1.Span != span2)
                return false;

            if (this.Hint.ReplacementTextChange != null && this.Hint.ReplacementTextChange != null)
            {
                // ensure both changes are talking about the same span.
                var replacementSpan1 = this.Hint.ReplacementTextChange.Value.Span.ToSnapshotSpan(_snapshot).TranslateTo(snapshotToMapTo, _provider.SpanTrackingMode);
                var replacementSpan2 = other.Hint.ReplacementTextChange.Value.Span.ToSpan();

                if (replacementSpan1.Span != replacementSpan2)
                    return false;
            }

            // ensure all the display parts are the same.
            return this.Hint.DisplayParts.SequenceEqual(other.Hint.DisplayParts);
        }
    }
}
