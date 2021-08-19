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
        private readonly Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? _getDescriptionAsync;
        private readonly Func<string>? _getReplacementText;

        public InlineHint(
            TextSpan span,
            ImmutableArray<TaggedText> displayParts,
            Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? getDescriptionAsync = null,
            Func<string>? getReplacementText = null)
        {
            if (displayParts.Length == 0)
                throw new ArgumentException($"{nameof(displayParts)} must be non-empty");

            Span = span;
            DisplayParts = displayParts;
            _getDescriptionAsync = getDescriptionAsync;
            _getReplacementText = getReplacementText;
        }

        /// <summary>
        /// Gets a description for the inline hint, suitable to show when a user hovers over the editor adornment.  The
        /// <paramref name="document"/> will represent the file at the time this hint was created.
        /// </summary>
        public Task<ImmutableArray<TaggedText>> GetDescriptionAsync(Document document, CancellationToken cancellationToken)
            => _getDescriptionAsync?.Invoke(document, cancellationToken) ?? SpecializedTasks.EmptyImmutableArray<TaggedText>();

        public string? GetReplacementText()
            => _getReplacementText?.Invoke() ?? null;
    }
}
