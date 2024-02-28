// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Snippets;

internal readonly struct SnippetPlaceholder
{
    /// <summary>
    /// The identifier in the snippet that needs to be renamed.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// The positions associated with the identifier that will need to
    /// be converted into LSP formatted strings.
    /// </summary>
    public readonly ImmutableArray<int> PlaceHolderPositions;

    /// <summary>
    /// <example> 
    /// For loop would have two placeholders:
    /// <code>
    ///     for (var {1:i} = 0; {1:i} &lt; {2:length}; {1:i}++)
    /// </code>
    /// Identifier: i, 3 associated positions <br/>
    /// Identifier: length, 1 associated position <br/>
    /// </example>
    /// </summary>
    public SnippetPlaceholder(string identifier, ImmutableArray<int> placeholderPositions)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentException($"{nameof(identifier)} must not be an null or empty.");
        }

        Identifier = identifier;
        PlaceHolderPositions = placeholderPositions;
    }

    /// <summary>
    /// Initialize a placeholder with a single position
    /// </summary>
    public SnippetPlaceholder(string identifier, int placeholderPosition)
        : this(identifier, [placeholderPosition])
    {
    }
}
