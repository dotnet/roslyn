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
        public readonly string DisplayText;

        /// The TextChange's associated with introducing a snippet into a document
        public readonly ImmutableArray<TextChange> TextChanges;

        /// The position that the cursor should end up on
        public readonly int CursorPosition;

        // The TextSpans that need to be renamed if we insert a snippet with values that the user may want to change
        public readonly ImmutableArray<TextSpan> RenameLocations;

        public Snippet(
            string displayText,
            ImmutableArray<TextChange> textChanges,
            int cursorPosition,
            ImmutableArray<TextSpan> renameLocations)
        {
            if (textChanges.IsEmpty)
            {
                throw new ArgumentException($"{ textChanges.Length } must not be empty");
            }

            DisplayText = displayText;
            TextChanges = textChanges;
            CursorPosition = cursorPosition;
            RenameLocations = renameLocations;
        }
    }
}
