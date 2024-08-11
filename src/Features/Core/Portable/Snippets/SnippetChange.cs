// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets;

/// <summary>
/// Encapsulates the information that makes up a Snippet.
/// </summary>
internal readonly struct SnippetChange
{
    /// <summary>
    /// The TextChange's associated with introducing a snippet into a document
    /// </summary>
    public readonly ImmutableArray<TextChange> TextChanges;

    /// <summary>
    /// The items that we will want to rename as well as the ordering
    /// in which to visit those items.
    /// </summary>
    public readonly ImmutableArray<SnippetPlaceholder> Placeholders;

    /// <summary>
    /// The position that the cursor should end up on
    /// </summary>
    public readonly int FinalCaretPosition;

    public SnippetChange(
        ImmutableArray<TextChange> textChanges,
        ImmutableArray<SnippetPlaceholder> placeholders,
        int finalCaretPosition)
    {
        if (textChanges.IsEmpty)
        {
            throw new ArgumentException($"{nameof(textChanges)} must not be empty.");
        }

        TextChanges = textChanges;
        Placeholders = placeholders;
        FinalCaretPosition = finalCaretPosition;
    }
}
