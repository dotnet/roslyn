// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal readonly struct InlineHint
    {
        public readonly TextSpan Span;
        public readonly ImmutableArray<TaggedText> DisplayParts;
        public readonly TextChange? ReplacementTextChange;
        public readonly double Ranking;
        private readonly Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? _getDescriptionAsync;

        public InlineHint(
            TextSpan span,
            ImmutableArray<TaggedText> displayParts,
            Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? getDescriptionAsync = null)
            : this(span, displayParts, replacementTextChange: null, ranking: 0.0, getDescriptionAsync)
        {
        }

        public InlineHint(
            TextSpan span,
            ImmutableArray<TaggedText> displayParts,
            TextChange? replacementTextChange,
            Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? getDescriptionAsync = null)
            : this(span, displayParts, replacementTextChange, ranking: 0.0, getDescriptionAsync)
        {
        }

        public InlineHint(
            TextSpan span,
            ImmutableArray<TaggedText> displayParts,
            TextChange? replacementTextChange,
            double ranking,
            Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? getDescriptionAsync = null)
        {
            if (displayParts.Length == 0)
                throw new ArgumentException($"{nameof(displayParts)} must be non-empty");

            Span = span;
            DisplayParts = displayParts;
            _getDescriptionAsync = getDescriptionAsync;
            ReplacementTextChange = replacementTextChange;
            Ranking = ranking;
        }

        /// <summary>
        /// Gets a description for the inline hint, suitable to show when a user hovers over the editor adornment.  The
        /// <paramref name="document"/> will represent the file at the time this hint was created.
        /// </summary>
        public Task<ImmutableArray<TaggedText>> GetDescriptionAsync(Document document, CancellationToken cancellationToken)
            => _getDescriptionAsync?.Invoke(document, cancellationToken) ?? SpecializedTasks.EmptyImmutableArray<TaggedText>();
    }
}
