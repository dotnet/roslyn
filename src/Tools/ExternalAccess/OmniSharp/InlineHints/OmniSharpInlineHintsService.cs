﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.InlineHints
{
    internal static class OmniSharpInlineHintsService
    {
        public static async Task<ImmutableArray<OmniSharpInlineHint>> GetInlineHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IInlineHintsService>();
            var hints = await service.GetInlineHintsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            return hints.SelectAsArray(static h => new OmniSharpInlineHint(
                h.Span,
                h.DisplayParts,
                (document, cancellationToken) => h.GetDescriptionAsync(document, cancellationToken)));
        }
    }

    internal readonly struct OmniSharpInlineHint
    {
        private readonly Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>> _getDescriptionAsync;

        public OmniSharpInlineHint(
            TextSpan span,
            ImmutableArray<TaggedText> displayParts,
            Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>> getDescriptionAsync)
        {
            Span = span;
            DisplayParts = displayParts;
            _getDescriptionAsync = getDescriptionAsync;
        }

        public readonly TextSpan Span { get; }
        public readonly ImmutableArray<TaggedText> DisplayParts { get; }

        public Task<ImmutableArray<TaggedText>> GetDescrptionAsync(Document document, CancellationToken cancellationToken)
            => _getDescriptionAsync.Invoke(document, cancellationToken);
    }
}
