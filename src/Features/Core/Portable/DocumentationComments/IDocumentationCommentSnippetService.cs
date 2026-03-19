// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal interface IDocumentationCommentSnippetService : ILanguageService
{
    /// <summary>
    /// A single character string indicating what the comment character is for the documentation comments
    /// </summary>
    string DocumentationCommentCharacter { get; }

    DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCharacterTyped(
        ParsedDocument document,
        int position,
        in DocumentationCommentOptions options,
        CancellationToken cancellationToken);

    DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCommandInvoke(
        ParsedDocument document,
        int position,
        in DocumentationCommentOptions options,
        CancellationToken cancellationToken);

    DocumentationCommentSnippet? GetDocumentationCommentSnippetOnEnterTyped(
        ParsedDocument document,
        int position,
        in DocumentationCommentOptions options,
        CancellationToken cancellationToken);

    DocumentationCommentSnippet? GetDocumentationCommentSnippetFromPreviousLine(
        in DocumentationCommentOptions options,
        TextLine currentLine,
        TextLine previousLine);

    bool IsValidTargetMember(ParsedDocument document, int caretPosition, CancellationToken cancellationToken);
}
