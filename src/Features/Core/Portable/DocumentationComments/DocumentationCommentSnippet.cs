// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal sealed class DocumentationCommentSnippet
{
    /// <summary>
    /// The span in the original text that should be replaced with the documentation comment.
    /// </summary>
    public TextSpan SpanToReplace { get; }

    /// <summary>
    /// The documentation comment text to replace the span with
    /// </summary>
    public string SnippetText { get; }

    /// <summary>
    /// The offset within <see cref="SnippetText"/> where the caret should be positioned after replacement
    /// </summary>
    public int CaretOffset { get; }

    internal DocumentationCommentSnippet(TextSpan spanToReplace, string snippetText, int caretOffset)
    {
        SpanToReplace = spanToReplace;
        SnippetText = snippetText;
        CaretOffset = caretOffset;
    }
}
