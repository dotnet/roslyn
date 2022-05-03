// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
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
        /// Example:
        /// For loop would have two placeholders:
        /// for (var {1:i} = 0; {1:i} &lt; {2:length}; {i}++)
        /// Identifier: i, 3 associated  positions 
        /// IdentifierL length, 1 associated position
        /// </summary>
        public SnippetPlaceholder(string identifier, ImmutableArray<int> placeholderPositions)
        {
            Identifier = identifier;
            PlaceHolderPositions = placeholderPositions;
        }
    }
}
