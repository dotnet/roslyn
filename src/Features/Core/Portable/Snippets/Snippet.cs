// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    /// <summary>
    /// Encapsulates the information that makes up a Snippet.
    /// </summary>
    internal readonly struct Snippet
    {
        /// The type of snippet, equivalent to what gets displayed in the Completion list
        public readonly string SnippetType;

        /// The TextChange that gets created for the Snippet
        public readonly TextChange TextChange;

        /// The position that the cursor should end up on
        public readonly int CursorPosition;

        // The TextSpans that need to be renamed if we insert a snippet with values that the user may want to change
        public readonly ImmutableArray<TextSpan> RenameLocations;

        public Snippet(
            string snippetType,
            TextChange textChange,
            int cursorPosition,
            ImmutableArray<TextSpan> renameLocations)
        {
            if (textChange.NewText is null)
            {
                throw new ArgumentException($"{ textChange.NewText } must be non-null");
            }

            SnippetType = snippetType;
            TextChange = textChange;
            CursorPosition = cursorPosition;
            RenameLocations = renameLocations;
        }
    }
}
