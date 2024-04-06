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
    public static void Deconstruct(this SyntaxTrivia trivia, out SyntaxKind kind)
        => kind = trivia.Kind();

    public static bool IsSingleOrMultiLineComment(this SyntaxTrivia trivia)
        => trivia.Kind() is SyntaxKind.MultiLineCommentTrivia or SyntaxKind.SingleLineCommentTrivia;

    public static bool IsRegularComment(this SyntaxTrivia trivia)
        => trivia.IsSingleOrMultiLineComment() || trivia.IsShebangDirective();

    public static bool IsWhitespaceOrSingleOrMultiLineComment(this SyntaxTrivia trivia)
        => trivia.IsWhitespace() || trivia.IsSingleOrMultiLineComment();

    public static bool IsRegularOrDocComment(this SyntaxTrivia trivia)
        => trivia.IsRegularComment() || trivia.IsDocComment();

    public static bool IsSingleLineComment(this SyntaxTrivia trivia)
        => trivia.Kind() == SyntaxKind.SingleLineCommentTrivia;

    public static bool IsMultiLineComment(this SyntaxTrivia trivia)
        => trivia.Kind() == SyntaxKind.MultiLineCommentTrivia;

    public static bool IsShebangDirective(this SyntaxTrivia trivia)
        => trivia.Kind() == SyntaxKind.ShebangDirectiveTrivia;

    public static bool IsCompleteMultiLineComment(this SyntaxTrivia trivia)
    {
        if (trivia.Kind() != SyntaxKind.MultiLineCommentTrivia)
            return false;

        var text = trivia.ToFullString();
        return text is [.., _, _, '*', '/'];
    }

    public static bool IsDocComment(this SyntaxTrivia trivia)
        => trivia.IsSingleLineDocComment() || trivia.IsMultiLineDocComment();

    public static bool IsSingleLineDocComment(this SyntaxTrivia trivia)
        => trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia;

    public static bool IsMultiLineDocComment(this SyntaxTrivia trivia)
        => trivia.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia;

    public static string GetCommentText(this SyntaxTrivia trivia)
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
            var lines = commentText.Split(new[] { newLine }, StringSplitOptions.None);
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

    public static string AsString(this IEnumerable<SyntaxTrivia> trivia)
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

    public static int GetFullWidth(this IEnumerable<SyntaxTrivia> trivia)
    {
        Contract.ThrowIfNull(trivia);
        return trivia.Sum(t => t.FullWidth());
    }

    public static SyntaxTriviaList AsTrivia(this string s)
        => SyntaxFactory.ParseLeadingTrivia(s ?? string.Empty);

    public static bool IsWhitespaceOrEndOfLine(this SyntaxTrivia trivia)
        => IsWhitespace(trivia) || IsEndOfLine(trivia);

    public static bool IsEndOfLine(this SyntaxTrivia trivia)
        => trivia.Kind() == SyntaxKind.EndOfLineTrivia;

    public static bool IsWhitespace(this SyntaxTrivia trivia)
        => trivia.Kind() == SyntaxKind.WhitespaceTrivia;

    public static SyntaxTrivia GetPreviousTrivia(
        this SyntaxTrivia trivia, SyntaxTree syntaxTree, CancellationToken cancellationToken, bool findInsideTrivia = false)
    {
        var span = trivia.FullSpan;
        if (span.Start == 0)
        {
            return default;
        }

        return syntaxTree.GetRoot(cancellationToken).FindTrivia(span.Start - 1, findInsideTrivia);
    }

    public static IEnumerable<SyntaxTrivia> FilterComments(this IEnumerable<SyntaxTrivia> trivia, bool addElasticMarker)
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

    public static bool IsPragmaDirective(this SyntaxTrivia trivia, out bool isDisable, out bool isActive, out SeparatedSyntaxList<SyntaxNode> errorCodes)
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
