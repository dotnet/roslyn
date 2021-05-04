// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.DocumentationComments
{
    internal static class OmniSharpDocumentationCommentsSnippetService
    {
        public static OmniSharpDocumentationCommentSnippet? GetDocumentationCommentSnippetOnCharacterTyped(
            Document document,
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            DocumentOptionSet options,
            CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();
            return Translate(service.GetDocumentationCommentSnippetOnCharacterTyped(syntaxTree, text, position, options, cancellationToken));
        }

        public static OmniSharpDocumentationCommentSnippet? GetDocumentationCommentSnippetOnCommandInvoke(
            Document document,
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            DocumentOptionSet options,
            CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();
            return Translate(service.GetDocumentationCommentSnippetOnCommandInvoke(syntaxTree, text, position, options, cancellationToken));
        }

        public static OmniSharpDocumentationCommentSnippet? GetDocumentationCommentSnippetOnEnterTyped(
            Document document,
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            DocumentOptionSet options,
            CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();
            return Translate(service.GetDocumentationCommentSnippetOnEnterTyped(syntaxTree, text, position, options, cancellationToken));
        }

        public static OmniSharpDocumentationCommentSnippet? GetDocumentationCommentSnippetFromPreviousLine(
            Document document,
            DocumentOptionSet options,
            TextLine currentLine,
            TextLine previousLine)
        {
            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();
            return Translate(service.GetDocumentationCommentSnippetFromPreviousLine(options, currentLine, previousLine));
        }

        private static OmniSharpDocumentationCommentSnippet? Translate(DocumentationCommentSnippet? result)
            => result == null ? null : new(result.SpanToReplace, result.SnippetText, result.CaretOffset);
    }

    internal sealed class OmniSharpDocumentationCommentSnippet
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

        internal OmniSharpDocumentationCommentSnippet(TextSpan spanToReplace, string snippetText, int caretOffset)
        {
            SpanToReplace = spanToReplace;
            SnippetText = snippetText;
            CaretOffset = caretOffset;
        }
    }
}
