// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host;
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
            OmniSharpDocumentationCommentOptionsWrapper options,
            CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();
            return Translate(service.GetDocumentationCommentSnippetOnCharacterTyped(syntaxTree, text, position, options.UnderlyingObject, cancellationToken));
        }

        public static OmniSharpDocumentationCommentSnippet? GetDocumentationCommentSnippetOnEnterTyped(
            Document document,
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            OmniSharpDocumentationCommentOptionsWrapper options,
            CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();
            return Translate(service.GetDocumentationCommentSnippetOnEnterTyped(syntaxTree, text, position, options.UnderlyingObject, cancellationToken));
        }

        private static OmniSharpDocumentationCommentSnippet? Translate(DocumentationCommentSnippet? result)
            => result == null ? null : new(result.SpanToReplace, result.SnippetText, result.CaretOffset);
    }
}
