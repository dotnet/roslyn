// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints;

/// <summary>
/// The simple tag that only holds information regarding the associated parameter name
/// for the argument
/// </summary>
internal sealed class InlineHintDataTag(InlineHintsDataTaggerProvider provider, ITextSnapshot snapshot, InlineHint hint) : ITag, IEquatable<InlineHintDataTag>
{
    private readonly InlineHintsDataTaggerProvider _provider = provider;

    /// <summary>
    /// The snapshot this tag was created against.
    /// </summary>
    private readonly ITextSnapshot _snapshot = snapshot;

    public readonly InlineHint Hint = hint;

    // Intentionally throwing, we have never supported this facility, and there is no contract around placing
    // these tags in sets or maps.
    public override int GetHashCode()
        => throw new NotImplementedException();

    public override bool Equals(object? obj)
        => obj is InlineHintDataTag tag && Equals(tag);

    public bool Equals(InlineHintDataTag? other)
    {
        if (other is null)
            return false;

        // they have to match if they're going to change text.
        if (this.Hint.ReplacementTextChange is null != other.Hint.ReplacementTextChange is null)
            return false;

        // the text change text has to match.
        if (this.Hint.ReplacementTextChange?.NewText != other.Hint.ReplacementTextChange?.NewText)
            return false;

        // Ensure both hints are talking about the same snapshot.
        if (!_provider.SpanEquals(_snapshot, this.Hint.Span, other._snapshot, other.Hint.Span))
            return false;

        if (this.Hint.ReplacementTextChange != null &&
            other.Hint.ReplacementTextChange != null &&
            !_provider.SpanEquals(_snapshot, this.Hint.ReplacementTextChange.Value.Span, other._snapshot, other.Hint.ReplacementTextChange.Value.Span))
        {
            return false;
        }

        // ensure all the display parts are the same.
        return this.Hint.DisplayParts.SequenceEqual(other.Hint.DisplayParts);
    }
}
