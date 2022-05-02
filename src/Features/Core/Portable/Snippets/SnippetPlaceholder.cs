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
        /// Will be null in the case of the final tab stop location,
        /// the '$0' case.
        /// </summary>
        public readonly string Identifier;

        /// <summary>
        /// The spans associated with the identifier that will need to
        /// be converted into LSP formatted strings.
        /// </summary>
        public readonly ImmutableArray<int> PlaceHolderPositions;

        public SnippetPlaceholder(string identifier, ImmutableArray<int> placeholderPositions)
        {
            Identifier = identifier;
            PlaceHolderPositions = placeholderPositions;
        }
    }
}
