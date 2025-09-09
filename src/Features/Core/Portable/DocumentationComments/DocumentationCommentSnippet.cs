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

    /// <summary>
    /// The original position of the caret in the original text.
    /// </summary>
    public int? Position { get; }

    /// <summary>
    /// The node that is being documented.
    /// </summary>
    public SyntaxNode? MemberNode { get; }

    /// <summary>
    /// The text to use for indentation. This is specifically used for the generate documentation with
    /// Copilot case to ensure the wrapped comments are indented correctly.
    /// </summary>
    public string? IndentText { get; }

    internal DocumentationCommentSnippet(TextSpan spanToReplace, string snippetText, int caretOffset, int? position, SyntaxNode? memberNode, string? indentText)
    {
        SpanToReplace = spanToReplace;
        SnippetText = snippetText;
        CaretOffset = caretOffset;
        Position = position;
        MemberNode = memberNode;
        IndentText = indentText;
    }
}
