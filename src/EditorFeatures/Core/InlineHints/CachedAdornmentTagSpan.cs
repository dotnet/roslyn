// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineHints;

internal partial class InlineHintsTaggerProvider
{
    /// <summary>
    /// The computed adornment tag for an inline hint, along with information needed to determine if it can be reused.
    /// This is created and cached on <see cref="InlineHintDataTag{TAdditionalInformation}.AdditionalData"/> on demand
    /// so that we only create adornment tags once and reuse as long as possible.
    /// </summary>
    /// <param name="classified">Whether or not the adornment tag was classified.  If the option for this changes, this
    /// cached tag should not be reused.</param>
    /// <param name="format">The text formatting used to create the hint.  If this format no longer matches the current
    /// formatting, this should not be reused.</param>
    /// <param name="adornmentTagSpan">The actual adornment tag to render.</param>
    private sealed class CachedAdornmentTagSpan(
        bool classified,
        TextFormattingRunProperties format,
        TagSpan<IntraTextAdornmentTag> adornmentTagSpan)
    {
        public bool Classified { get; } = classified;
        public TextFormattingRunProperties Format { get; } = format;
        public TagSpan<IntraTextAdornmentTag> AdornmentTagSpan { get; } = adornmentTagSpan;
    }
}
