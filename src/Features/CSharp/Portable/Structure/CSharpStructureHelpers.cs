// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal static class CSharpStructureHelpers
{
    public const string Ellipsis = "...";
    public const string MultiLineCommentSuffix = "*/";
    public const int MaxXmlDocCommentBannerLength = 120;
    private static readonly char[] s_newLineCharacters = new[] { '\r', '\n' };

    private static int GetCollapsibleStart(SyntaxToken firstToken)
    {
        // Check *this* token to see if it has any trailing comments and use the last one; otherwise, we use the end
        // of this token.

        var lastTrailingCommentOrWhitespaceTrivia = firstToken.TrailingTrivia.GetLastCommentOrWhitespace();
        return lastTrailingCommentOrWhitespaceTrivia?.Span.End ?? firstToken.Span.End;
    }

    private static (int spanEnd, int hintEnd) GetCollapsibleEnd(SyntaxToken lastToken, bool compressEmptyLines)
    {
        // If the token has any trailing comments, we use the end of the token;
        // otherwise, the behavior depends on 'compressEmptyLines':
        //   false: skip to the start of the first new line trivia
        //   true: skip to the start of the last new line trivia preceding a non-whitespace line
        //
        // The hint span never includes the compressed empty lines.

        var trailingTrivia = lastToken.TrailingTrivia;
        var nextLeadingTrivia = compressEmptyLines ? lastToken.GetNextToken(includeZeroWidth: true, includeSkipped: true).LeadingTrivia : default;

        var end = lastToken.Span.End;
        int? hintEnd = null;

        foreach (var trivia in trailingTrivia)
        {
            if (!ProcessTrivia(trivia, compressEmptyLines, ref end, ref hintEnd))
                return (end, hintEnd ?? end);
        }

        foreach (var trivia in nextLeadingTrivia)
        {
            if (!ProcessTrivia(trivia, compressEmptyLines, ref end, ref hintEnd))
                return (end, hintEnd ?? end);
        }

        return (end, hintEnd ?? end);

        // Return true to keep processing trivia; otherwise, false to return the current 'end'
        static bool ProcessTrivia(SyntaxTrivia trivia, bool compressEmptyLines, ref int end, ref int? hintEnd)
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                end = trivia.SpanStart;
                hintEnd ??= end;
                if (!compressEmptyLines)
                    return false;
            }
            else if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                // We want this trivia to be visible even when the element is collapsed
                return false;
            }

            return true;
        }
    }

    public static SyntaxToken GetLastInlineMethodBlockToken(SyntaxNode node)
    {
        var lastToken = node.GetLastToken(includeZeroWidth: true);
        if (lastToken.Kind() == SyntaxKind.None)
        {
            return default;
        }

        // If the next token is a semicolon, and we aren't in the initializer of a for-loop, use that token as the end.

        var nextToken = lastToken.GetNextToken(includeSkipped: true);
        if (nextToken.Kind() is not SyntaxKind.None and SyntaxKind.SemicolonToken)
        {
            var forStatement = nextToken.GetAncestor<ForStatementSyntax>();
            if (forStatement != null && forStatement.FirstSemicolonToken == nextToken)
            {
                return default;
            }

            lastToken = nextToken;
        }

        return lastToken;
    }

    private static string CreateCommentBannerTextWithPrefix(string text, string prefix)
    {
        Contract.ThrowIfNull(text);
        Contract.ThrowIfNull(prefix);

        var prefixLength = prefix.Length;
        return prefix + " " + text[prefixLength..].Trim() + " " + Ellipsis;
    }

    public static string GetCommentBannerText(SyntaxTrivia comment)
    {
        Contract.ThrowIfFalse(comment.IsSingleLineComment() || comment.IsMultiLineComment());

        if (comment.IsSingleLineComment())
        {
            return CreateCommentBannerTextWithPrefix(comment.ToString(), "//");
        }
        else if (comment.IsMultiLineComment())
        {
            var lineBreakStart = comment.ToString().IndexOfAny(s_newLineCharacters);

            var text = comment.ToString();
            if (lineBreakStart >= 0)
            {
                text = text[..lineBreakStart];
            }
            else
            {
                text = text.Length >= "/**/".Length && text.EndsWith(MultiLineCommentSuffix)
                    ? text[..^MultiLineCommentSuffix.Length]
                    : text;
            }

            return CreateCommentBannerTextWithPrefix(text, "/*");
        }
        else
        {
            return string.Empty;
        }
    }

    private static BlockSpan CreateCommentBlockSpan(
        SyntaxTrivia startComment, SyntaxTrivia endComment)
    {
        var span = TextSpan.FromBounds(startComment.SpanStart, endComment.Span.End);

        return new BlockSpan(
            isCollapsible: true,
            textSpan: span,
            hintSpan: span,
            type: BlockTypes.Comment,
            bannerText: GetCommentBannerText(startComment),
            autoCollapse: true);
    }

    public static void CollectCommentBlockSpans(
        SyntaxTriviaList triviaList, ref TemporaryArray<BlockSpan> spans)
    {
        if (triviaList.Count > 0)
        {
            SyntaxTrivia? startComment = null;
            SyntaxTrivia? endComment = null;

            // Iterate through trivia and collect the following:
            //    1. Groups of contiguous single-line comments that are only separated by whitespace
            //    2. Multi-line comments
            foreach (var trivia in triviaList)
            {
                if (trivia.IsSingleLineComment())
                {
                    startComment ??= trivia;
                    endComment = trivia;
                }
                else if (trivia.IsMultiLineComment())
                {
                    // Multiline comments are handled by the MultilineCommentBlockStructureProvider.
                    continue;
                }
                else if (trivia is not SyntaxTrivia(
                    SyntaxKind.WhitespaceTrivia or SyntaxKind.EndOfLineTrivia or SyntaxKind.EndOfFileToken))
                {
                    completeSingleLineCommentGroup(ref spans);
                }
            }

            completeSingleLineCommentGroup(ref spans);
            return;

            void completeSingleLineCommentGroup(ref TemporaryArray<BlockSpan> spans)
            {
                if (startComment != null)
                {
                    var singleLineCommentGroupRegion = CreateCommentBlockSpan(startComment.Value, endComment!.Value);
                    spans.Add(singleLineCommentGroupRegion);
                    startComment = null;
                    endComment = null;
                }
            }
        }
    }

    public static void CollectCommentBlockSpans(
        SyntaxNode node,
        ref TemporaryArray<BlockSpan> spans,
        in BlockStructureOptions options)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (options.IsMetadataAsSource && TryGetLeadingCollapsibleSpan(node, out var span))
        {
            spans.Add(span);
        }
        else
        {
            var triviaList = node.GetLeadingTrivia();
            CollectCommentBlockSpans(triviaList, ref spans);
        }

        return;

        // Local functions
        static bool TryGetLeadingCollapsibleSpan(SyntaxNode node, out BlockSpan span)
        {
            var startToken = node.GetFirstToken();
            var endToken = GetEndToken(node);
            if (startToken.IsKind(SyntaxKind.None) || endToken.IsKind(SyntaxKind.None))
            {
                // if valid tokens can't be found then a meaningful span can't be generated
                span = default;
                return false;
            }

            var firstComment = startToken.LeadingTrivia.FirstOrNull(t => t.Kind() is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.SingleLineDocumentationCommentTrivia);

            var startPosition = firstComment.HasValue ? firstComment.Value.FullSpan.Start : startToken.SpanStart;
            var endPosition = endToken.SpanStart;

            // TODO (tomescht): Mark the regions to be collapsed by default.
            if (startPosition != endPosition)
            {
                var hintTextEndToken = GetHintTextEndToken(node);
                span = new BlockSpan(
                    isCollapsible: true,
                    type: BlockTypes.Comment,
                    textSpan: TextSpan.FromBounds(startPosition, endPosition),
                    hintSpan: TextSpan.FromBounds(startPosition, hintTextEndToken.Span.End),
                    bannerText: Ellipsis,
                    autoCollapse: true);
                return true;
            }

            span = default;
            return false;
        }

        static SyntaxToken GetEndToken(SyntaxNode node)
            => node switch
            {
                ConstructorDeclarationSyntax constructorDeclaration => constructorDeclaration.Modifiers.FirstOrNull() ?? constructorDeclaration.Identifier,
                ConversionOperatorDeclarationSyntax conversionOperatorDeclaration => conversionOperatorDeclaration.Modifiers.FirstOrNull() ?? conversionOperatorDeclaration.ImplicitOrExplicitKeyword,
                DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.Modifiers.FirstOrNull() ?? delegateDeclaration.DelegateKeyword,
                DestructorDeclarationSyntax destructorDeclaration => destructorDeclaration.TildeToken,
                EnumDeclarationSyntax enumDeclaration => enumDeclaration.Modifiers.FirstOrNull() ?? enumDeclaration.EnumKeyword,
                EnumMemberDeclarationSyntax enumMemberDeclaration => enumMemberDeclaration.Identifier,
                EventDeclarationSyntax eventDeclaration => eventDeclaration.Modifiers.FirstOrNull() ?? eventDeclaration.EventKeyword,
                EventFieldDeclarationSyntax eventFieldDeclaration => eventFieldDeclaration.Modifiers.FirstOrNull() ?? eventFieldDeclaration.EventKeyword,
                FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Modifiers.FirstOrNull() ?? fieldDeclaration.Declaration.GetFirstToken(),
                IndexerDeclarationSyntax indexerDeclaration => indexerDeclaration.Modifiers.FirstOrNull() ?? indexerDeclaration.Type.GetFirstToken(),
                MethodDeclarationSyntax methodDeclaration => methodDeclaration.Modifiers.FirstOrNull() ?? methodDeclaration.ReturnType.GetFirstToken(),
                OperatorDeclarationSyntax operatorDeclaration => operatorDeclaration.Modifiers.FirstOrNull() ?? operatorDeclaration.ReturnType.GetFirstToken(),
                PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Modifiers.FirstOrNull() ?? propertyDeclaration.Type.GetFirstToken(),
                TypeDeclarationSyntax typeDeclaration => typeDeclaration.Modifiers.FirstOrNull() ?? typeDeclaration.Keyword,
                _ => default
            };

        static SyntaxToken GetHintTextEndToken(SyntaxNode node)
            => node switch
            {
                EnumDeclarationSyntax enumDeclaration => enumDeclaration.OpenBraceToken.GetPreviousToken(),
                TypeDeclarationSyntax typeDeclaration => typeDeclaration.OpenBraceToken.GetPreviousToken(),
                _ => node.GetLastToken()
            };
    }

    private static BlockSpan CreateBlockSpan(
        TextSpan textSpan, string bannerText, bool autoCollapse,
        string type, bool isCollapsible)
    {
        return CreateBlockSpan(
            textSpan, textSpan, bannerText, autoCollapse, type, isCollapsible, isDefaultCollapsed: false);
    }

    private static BlockSpan CreateBlockSpan(
        TextSpan textSpan, TextSpan hintSpan,
        string bannerText, bool autoCollapse,
        string type, bool isCollapsible, bool isDefaultCollapsed)
    {
        return new BlockSpan(
            textSpan: textSpan,
            hintSpan: hintSpan,
            bannerText: bannerText,
            autoCollapse: autoCollapse,
            type: type,
            isCollapsible: isCollapsible,
            isDefaultCollapsed: isDefaultCollapsed);
    }

    public static BlockSpan CreateBlockSpan(
        SyntaxNode node, string bannerText, bool autoCollapse,
        string type, bool isCollapsible)
    {
        return CreateBlockSpan(
            node.Span,
            bannerText,
            autoCollapse,
            type,
            isCollapsible);
    }

    public static BlockSpan? CreateBlockSpan(
        SyntaxNode node, SyntaxToken syntaxToken, bool compressEmptyLines,
        string bannerText, bool autoCollapse,
        string type, bool isCollapsible)
    {
        return CreateBlockSpan(
            node, syntaxToken, node.GetLastToken(), compressEmptyLines,
            bannerText, autoCollapse, type, isCollapsible);
    }

    public static BlockSpan? CreateBlockSpan(
        SyntaxNode node, SyntaxToken startToken,
        int spanEndPos, int hintEndPos, string bannerText, bool autoCollapse,
        string type, bool isCollapsible)
    {
        // If the SyntaxToken is actually missing, don't attempt to create an outlining region.
        if (startToken.IsMissing)
        {
            return null;
        }

        // Since we creating a span for everything after syntaxToken to ensure
        // that it collapses properly. However, the hint span begins at the start
        // of the next token so indentation in the tooltip is accurate.

        var span = TextSpan.FromBounds(GetCollapsibleStart(startToken), spanEndPos);
        var hintSpan = GetHintSpan(node, hintEndPos);

        return CreateBlockSpan(
            span,
            hintSpan,
            bannerText,
            autoCollapse,
            type,
            isCollapsible,
            isDefaultCollapsed: false);
    }

    private static TextSpan GetHintSpan(SyntaxNode node, int endPos)
    {
        // Don't include attributes in the BlockSpan for a node.  When the user
        // hovers over the indent-guide we don't want to show them the line with
        // the attributes, we want to show them the line with the start of the
        // actual structure.
        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.Kind() != SyntaxKind.AttributeList)
            {
                return TextSpan.FromBounds(child.SpanStart, endPos);
            }
        }

        return TextSpan.FromBounds(node.SpanStart, endPos);
    }

    public static BlockSpan? CreateBlockSpan(
        SyntaxNode node, SyntaxToken startToken,
        SyntaxToken endToken, bool compressEmptyLines, string bannerText, bool autoCollapse,
        string type, bool isCollapsible)
    {
        var (spanEnd, hintEnd) = GetCollapsibleEnd(endToken, compressEmptyLines);
        return CreateBlockSpan(
            node, startToken, spanEnd, hintEnd,
            bannerText, autoCollapse, type, isCollapsible);
    }

    public static BlockSpan CreateBlockSpan(
        SyntaxNode node, bool autoCollapse, string type, bool isCollapsible)
    {
        return CreateBlockSpan(
            node,
            bannerText: Ellipsis,
            autoCollapse: autoCollapse,
            type: type,
            isCollapsible: isCollapsible);
    }

    // Adds everything after 'syntaxToken' up to and including the end 
    // of node as a region.  The snippet to display is just "..."
    public static BlockSpan? CreateBlockSpan(
        SyntaxNode node, SyntaxToken syntaxToken, bool compressEmptyLines,
        bool autoCollapse, string type, bool isCollapsible)
    {
        return CreateBlockSpan(
            node, syntaxToken, compressEmptyLines,
            bannerText: Ellipsis,
            autoCollapse: autoCollapse,
            type: type,
            isCollapsible: isCollapsible);
    }

    // Adds everything after 'syntaxToken' up to and including the end 
    // of node as a region.  The snippet to display is just "..."
    public static BlockSpan? CreateBlockSpan(
        SyntaxNode node, SyntaxToken startToken, SyntaxToken endToken, bool compressEmptyLines,
        bool autoCollapse, string type, bool isCollapsible)
    {
        return CreateBlockSpan(
            node, startToken, endToken, compressEmptyLines,
            bannerText: Ellipsis,
            autoCollapse: autoCollapse,
            type: type,
            isCollapsible: isCollapsible);
    }

    // Adds the span surrounding the syntax list as a region.  The
    // snippet shown is the text from the first line of the first 
    // node in the list.
    public static BlockSpan? CreateBlockSpan(
        IEnumerable<SyntaxNode> syntaxList, bool compressEmptyLines, bool autoCollapse,
        string type, bool isCollapsible, bool isDefaultCollapsed)
    {
        if (syntaxList.IsEmpty())
        {
            return null;
        }

        var (end, hintEnd) = GetCollapsibleEnd(syntaxList.Last().GetLastToken(), compressEmptyLines);

        var spanStart = syntaxList.First().GetFirstToken().FullSpan.End;
        var spanEnd = end >= spanStart
            ? end
            : spanStart;

        var hintSpanStart = syntaxList.First().SpanStart;
        var hintSpanEnd = hintEnd >= hintSpanStart
            ? hintEnd
            : hintSpanStart;

        return CreateBlockSpan(
            textSpan: TextSpan.FromBounds(spanStart, spanEnd),
            hintSpan: TextSpan.FromBounds(hintSpanStart, hintSpanEnd),
            bannerText: Ellipsis,
            autoCollapse: autoCollapse,
            type: type,
            isCollapsible: isCollapsible,
            isDefaultCollapsed: isDefaultCollapsed);
    }
}
