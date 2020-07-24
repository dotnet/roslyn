// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class DocumentationCommentSnippet
    {
        public TextSpan SpanToReplace { get; }
        public string SnippetText { get; }
        public int CaretOffset { get; }

        internal DocumentationCommentSnippet(TextSpan spanToReplace, string snippetText, int caretOffset)
        {
            SpanToReplace = spanToReplace;
            SnippetText = snippetText;
            CaretOffset = caretOffset;
        }
    }
}
