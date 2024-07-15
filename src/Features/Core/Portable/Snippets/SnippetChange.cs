// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
    /// The position that the cursor should end up on
    /// </summary>
    public readonly int CursorPosition;

    /// <summary>
    /// The items that we will want to rename as well as the ordering
    /// in which to visit those items.
    /// </summary>
    public readonly ImmutableArray<SnippetPlaceholder> Placeholders;

    public SnippetChange(
        ImmutableArray<TextChange> textChanges,
        int cursorPosition,
        ImmutableArray<SnippetPlaceholder> placeholders)
    {
        if (textChanges.IsEmpty)
        {
            throw new ArgumentException($"{nameof(textChanges)} must not be empty.");
        }

        TextChanges = textChanges;
        CursorPosition = cursorPosition;
        Placeholders = placeholders;
    }
}
