// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class SyntaxTriviaExtensions
{
    extension(SyntaxTrivia trivia)
    {
        public void Deconstruct(out SyntaxKind kind)
        => kind = trivia.Kind();

        public bool IsSingleOrMultiLineComment()
            => trivia.Kind() is SyntaxKind.MultiLineCommentTrivia or SyntaxKind.SingleLineCommentTrivia;

        public bool IsRegularComment()
            => trivia.IsSingleOrMultiLineComment() || trivia.IsShebangDirective();

        public bool IsWhitespaceOrSingleOrMultiLineComment()
            => trivia.IsWhitespace() || trivia.IsSingleOrMultiLineComment();

        public bool IsRegularOrDocComment()
            => trivia.IsRegularComment() || trivia.IsDocComment();

        public bool IsSingleLineComment()
            => trivia.Kind() == SyntaxKind.SingleLineCommentTrivia;

        public bool IsMultiLineComment()
            => trivia.Kind() == SyntaxKind.MultiLineCommentTrivia;

        public bool IsShebangDirective()
            => trivia.Kind() == SyntaxKind.ShebangDirectiveTrivia;

        public bool IsCompleteMultiLineComment()
        {
            if (trivia.Kind() != SyntaxKind.MultiLineCommentTrivia)
                return false;

            var text = trivia.ToFullString();
            return text is [.., _, _, '*', '/'];
        }

        public bool IsDocComment()
            => trivia.IsSingleLineDocComment() || trivia.IsMultiLineDocComment();

        public bool IsSingleLineDocComment()
            => trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia;

        public bool IsMultiLineDocComment()
            => trivia.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia;

        public string GetCommentText()
        {
            var commentText = trivia.ToString();
            if (trivia.Kind() == SyntaxKind.SingleLineCommentTrivia)
            {
                if (commentText.StartsWith("//", StringComparison.Ordinal))
                {
                    commentText = commentText[2..];
                }

                return commentText.TrimStart(null);
            }
            else if (trivia.Kind() == SyntaxKind.MultiLineCommentTrivia)
            {
                var textBuilder = new StringBuilder();

                if (commentText.EndsWith("*/", StringComparison.Ordinal))
                {
                    commentText = commentText[..^2];
                }

                if (commentText.StartsWith("/*", StringComparison.Ordinal))
                {
                    commentText = commentText[2..];
                }

                commentText = commentText.Trim();

                var newLine = Environment.NewLine;
                var lines = commentText.Split([newLine], StringSplitOptions.None);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Note: we trim leading '*' characters in multi-line comments.
                    // If the '*' was intentional, sorry, it's gone.
                    if (trimmedLine.StartsWith("*", StringComparison.Ordinal))
                    {
                        trimmedLine = trimmedLine.TrimStart('*');
                        trimmedLine = trimmedLine.TrimStart(null);
                    }

                    textBuilder.AppendLine(trimmedLine);
                }

                // remove last line break
                textBuilder.Remove(textBuilder.Length - newLine.Length, newLine.Length);

                return textBuilder.ToString();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public bool IsWhitespaceOrEndOfLine()
            => IsWhitespace(trivia) || IsEndOfLine(trivia);

        public bool IsEndOfLine()
            => trivia.Kind() == SyntaxKind.EndOfLineTrivia;

        public bool IsWhitespace()
            => trivia.Kind() == SyntaxKind.WhitespaceTrivia;

        public SyntaxTrivia GetPreviousTrivia(
    SyntaxTree syntaxTree, CancellationToken cancellationToken, bool findInsideTrivia = false)
        {
            var span = trivia.FullSpan;
            if (span.Start == 0)
            {
                return default;
            }

            return syntaxTree.GetRoot(cancellationToken).FindTrivia(span.Start - 1, findInsideTrivia);
        }

#if false
    public static int Width(this SyntaxTrivia trivia)
    {
        return trivia.Span.Length;
    }

    public static int FullWidth(this SyntaxTrivia trivia)
    {
        return trivia.FullSpan.Length;
    }
#endif

        public bool IsPragmaDirective(out bool isDisable, out bool isActive, out SeparatedSyntaxList<SyntaxNode> errorCodes)
        {
            if (trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia))
            {
                var pragmaWarning = (PragmaWarningDirectiveTriviaSyntax)trivia.GetStructure();
                isDisable = pragmaWarning.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword);
                isActive = pragmaWarning.IsActive;
                errorCodes = pragmaWarning.ErrorCodes;
                return true;
            }

            isDisable = false;
            isActive = false;
            errorCodes = default;
            return false;
        }
    }

    extension(IEnumerable<SyntaxTrivia> trivia)
    {
        public string AsString()
        {
            Contract.ThrowIfNull(trivia);

            if (trivia.Any())
            {
                var sb = new StringBuilder();
                trivia.Select(t => t.ToFullString()).Do(s => sb.Append(s));
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        public int GetFullWidth()
        {
            Contract.ThrowIfNull(trivia);
            return trivia.Sum(t => t.FullWidth());
        }

        public IEnumerable<SyntaxTrivia> FilterComments(bool addElasticMarker)
        {
            var previousIsSingleLineComment = false;
            foreach (var t in trivia)
            {
                if (previousIsSingleLineComment && t.IsEndOfLine())
                {
                    yield return t;
                }

                if (t.IsSingleOrMultiLineComment())
                {
                    yield return t;
                }

                previousIsSingleLineComment = t.IsSingleLineComment();
            }

            if (addElasticMarker)
            {
                yield return SyntaxFactory.ElasticMarker;
            }
        }
    }

    extension(string s)
    {
        public SyntaxTriviaList AsTrivia()
        => SyntaxFactory.ParseLeadingTrivia(s ?? string.Empty);
    }
}
