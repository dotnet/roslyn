// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

/// <summary>
/// The individual piece of each documentation comment that will eventually be proposed as an edit.
/// E.g. the summary tag, the param tag, etc.
/// </summary>
internal sealed record DocumentationCommentProposedEdit
{
    public TextSpan SpanToReplace { get; }

    // May be null if the piece of the comment to document does not have an
    // associated name.
    public string? SymbolName { get; }

    public DocumentationCommentTagType TagType { get; }

    public DocumentationCommentProposedEdit(TextSpan spanToReplace, string? symbolName, DocumentationCommentTagType tagType)
    {
        SpanToReplace = spanToReplace;
        SymbolName = symbolName;
        TagType = tagType;
    }
}
