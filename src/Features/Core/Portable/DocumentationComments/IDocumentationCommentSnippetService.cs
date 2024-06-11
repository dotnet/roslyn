// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal interface IDocumentationCommentSnippetService : ILanguageService
    {
        /// <summary>
        /// A single character string indicating what the comment character is for the documentation comments
        /// </summary>
        string DocumentationCommentCharacter { get; }

        DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCharacterTyped(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            in DocumentationCommentOptions options,
            CancellationToken cancellationToken,
            bool addIndentation = true);

        DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCommandInvoke(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            in DocumentationCommentOptions options,
            CancellationToken cancellationToken);

        DocumentationCommentSnippet? GetDocumentationCommentSnippetOnEnterTyped(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            in DocumentationCommentOptions options,
            CancellationToken cancellationToken);

        DocumentationCommentSnippet? GetDocumentationCommentSnippetFromPreviousLine(
            in DocumentationCommentOptions options,
            TextLine currentLine,
            TextLine previousLine);

        bool IsValidTargetMember(SyntaxTree syntaxTree, SourceText text, int caretPosition, CancellationToken cancellationToken);
    }
}
