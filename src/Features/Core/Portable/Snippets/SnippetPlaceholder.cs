// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Snippets;

internal readonly struct SnippetPlaceholder
{
    /// <summary>
    /// Editable text in the snippet.
    /// </summary>
    public readonly string Text;

    /// <summary>
    /// The positions associated with the identifier that will need to
    /// be converted into LSP formatted strings.
    /// </summary>
    public readonly ImmutableArray<int> StartingPositions;

    /// <summary>
    /// <example> 
    /// For loop would have two placeholders:
    /// <code>
    ///     for (var {1:i} = 0; {1:i} &lt; {2:length}; {1:i}++)
    /// </code>
    /// Text: <c>i</c>, 3 associated positions <br/>
    /// Text: <c>length</c>, 1 associated position <br/>
    /// </example>
    /// </summary>
    public SnippetPlaceholder(string text, ImmutableArray<int> startingPositions)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException($"{nameof(text)} must not be an null or empty.");
        }

        Text = text;
        StartingPositions = startingPositions;
    }

    /// <summary>
    /// Initialize a placeholder with a single position
    /// </summary>
    public SnippetPlaceholder(string text, int startingPosition)
        : this(text, [startingPosition])
    {
    }

    public void Deconstruct(out string text, out ImmutableArray<int> startingPositions)
    {
        text = Text;
        startingPositions = StartingPositions;
    }
}
