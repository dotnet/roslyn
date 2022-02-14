// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal readonly struct Snippet
    {
        public readonly string SnippetType;
        public readonly TextChange TextChange;
        public readonly int CursorPosition;
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
