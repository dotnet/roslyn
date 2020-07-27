// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
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
            DocumentOptionSet options,
            CancellationToken cancellationToken);

        DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCommandInvoke(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            DocumentOptionSet options,
            CancellationToken cancellationToken);

        DocumentationCommentSnippet? GetDocumentationCommentSnippetOnEnterTyped(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            DocumentOptionSet options,
            CancellationToken cancellationToken);

        DocumentationCommentSnippet? GetDocumentationCommentSnippetFromPreviousLine(
            DocumentOptionSet options,
            TextLine currentLine,
            TextLine previousLine);

        bool IsValidTargetMember(SyntaxTree syntaxTree, SourceText text, int caretPosition, CancellationToken cancellationToken);
    }
}
