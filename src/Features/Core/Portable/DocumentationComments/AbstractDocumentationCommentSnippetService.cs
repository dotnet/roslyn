// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal abstract class AbstractDocumentationCommentSnippetService<TDocumentationComment, TMemberNode> : IDocumentationCommentSnippetService
    where TDocumentationComment : SyntaxNode, IStructuredTriviaSyntax
    where TMemberNode : SyntaxNode
{
    protected abstract TMemberNode? GetContainingMember(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
    protected abstract bool SupportsDocumentationComments(TMemberNode member);
    protected abstract bool HasDocumentationComment(TMemberNode member);
    protected abstract int GetPrecedingDocumentationCommentCount(TMemberNode member);
    protected abstract List<string> GetDocumentationCommentStubLines(TMemberNode member, string existingCommentText);

    protected abstract SyntaxToken GetTokenToRight(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
    protected abstract SyntaxToken GetTokenToLeft(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
    protected abstract bool IsDocCommentNewLine(SyntaxToken token);
    protected abstract bool IsEndOfLineTrivia(SyntaxTrivia trivia);

    protected abstract bool IsSingleExteriorTrivia(TDocumentationComment documentationComment, [NotNullWhen(true)] out string? existingCommentText);
    protected abstract bool EndsWithSingleExteriorTrivia(TDocumentationComment? documentationComment);
    protected abstract bool IsMultilineDocComment(TDocumentationComment? documentationComment);
    protected abstract bool HasSkippedTrailingTrivia(SyntaxToken token);

    public abstract string DocumentationCommentCharacter { get; }

    protected abstract string ExteriorTriviaText { get; }
    protected abstract bool AddIndent { get; }

    public DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCharacterTyped(
        SyntaxTree syntaxTree,
        SourceText text,
        int position,
        in DocumentationCommentOptions options,
        CancellationToken cancellationToken,
        bool addIndentation = true)
    {
        if (!options.AutoXmlDocCommentGeneration)
        {
            return null;
        }

        // Only generate if the position is immediately after '///', 
        // and that is the only documentation comment on the target member.

        var token = syntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
        if (position != token.SpanStart)
        {
            return null;
        }

        var lines = addIndentation
            ? GetDocumentationCommentLines(token, text, options, out _, out var caretOffset, out var spanToReplaceLength)
            : GetDocumentationCommentLinesNoIndentation(token, text, options, out caretOffset, out spanToReplaceLength);

        if (lines == null)
        {
            return null;
        }

        var newLine = options.NewLine;

        var lastLine = lines[^1];
        lines[^1] = lastLine[..^newLine.Length];

        var comments = string.Join(string.Empty, lines);

        var replaceSpan = new TextSpan(token.Span.Start, spanToReplaceLength);

        return new DocumentationCommentSnippet(replaceSpan, comments, caretOffset);
    }

    private List<string>? GetDocumentationCommentLines(SyntaxToken token, SourceText text, in DocumentationCommentOptions options, out string? indentText, out int caretOffset, out int spanToReplaceLength)
    {
        indentText = null;

        var lines = GetDocumentationStubLines(token, text, options, out caretOffset, out spanToReplaceLength, out var existingCommentText);
        if (lines is null)
        {
            return lines;
        }

        var documentationComment = token.GetAncestor<TDocumentationComment>();
        var line = text.Lines.GetLineFromPosition(documentationComment!.FullSpan.Start);
        if (line.IsEmptyOrWhitespace())
        {
            return null;
        }

        // Add indents
        var lineOffset = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(options.TabSize);
        indentText = lineOffset.CreateIndentationString(options.UseTabs, options.TabSize);

        IndentLines(lines, indentText);

        // We always want the caret text to be on the second line, with one space after the doc comment XML
        // GetDocumentationCommentStubLines ensures that space is always there
        caretOffset = lines[0].Length + indentText.Length + ExteriorTriviaText.Length + 1;
        spanToReplaceLength = existingCommentText!.Length;

        return lines;
    }

    private List<string>? GetDocumentationCommentLinesNoIndentation(SyntaxToken token, SourceText text, in DocumentationCommentOptions options, out int caretOffset, out int spanToReplaceLength)
    {
        var lines = GetDocumentationStubLines(token, text, options, out caretOffset, out spanToReplaceLength, out var existingCommentText);
        if (lines is null)
        {
            return lines;
        }

        // We always want the caret text to be on the second line, with one space after the doc comment XML
        // GetDocumentationCommentStubLines ensures that space is always there
        caretOffset = lines[0].Length + ExteriorTriviaText.Length + 1;
        spanToReplaceLength = existingCommentText!.Length;

        return lines;
    }

    private List<string>? GetDocumentationStubLines(SyntaxToken token, SourceText text, in DocumentationCommentOptions options, out int caretOffset, out int spanToReplaceLength, out string? existingCommentText)
    {
        caretOffset = 0;
        spanToReplaceLength = 0;
        existingCommentText = null;

        var documentationComment = token.GetAncestor<TDocumentationComment>();

        if (documentationComment == null || !IsSingleExteriorTrivia(documentationComment, out existingCommentText))
        {
            return null;
        }

        var targetMember = GetTargetMember(documentationComment);

        // Ensure that the target member is only preceded by a single documentation comment (i.e. our ///).
        if (targetMember == null || GetPrecedingDocumentationCommentCount(targetMember) != 1)
        {
            return null;
        }

        var line = text.Lines.GetLineFromPosition(documentationComment.FullSpan.Start);
        if (line.IsEmptyOrWhitespace())
        {
            return null;
        }

        var lines = GetDocumentationCommentStubLines(targetMember, existingCommentText);
        Debug.Assert(lines.Count > 2);

        AddLineBreaks(lines, options.NewLine);

        // Shave off initial three slashes
        lines[0] = lines[0][3..];

        return lines;
    }

    public bool IsValidTargetMember(SyntaxTree syntaxTree, SourceText text, int position, CancellationToken cancellationToken)
        => GetTargetMember(syntaxTree, text, position, cancellationToken) != null;

    private TMemberNode? GetTargetMember(SyntaxTree syntaxTree, SourceText text, int position, CancellationToken cancellationToken)
    {
        var member = GetContainingMember(syntaxTree, position, cancellationToken);
        if (member == null)
        {
            return null;
        }

        if (!SupportsDocumentationComments(member) || HasDocumentationComment(member))
        {
            return null;
        }

        var startPosition = member.GetFirstToken().SpanStart;
        var line = text.Lines.GetLineFromPosition(startPosition);
        var lineOffset = line.GetFirstNonWhitespaceOffset();
        if (!lineOffset.HasValue || line.Start + lineOffset.Value < startPosition)
        {
            return null;
        }

        return member;
    }

    private TMemberNode? GetTargetMember(TDocumentationComment documentationComment)
    {
        var targetMember = documentationComment.ParentTrivia.Token.GetAncestor<TMemberNode>();

        if (targetMember == null || !SupportsDocumentationComments(targetMember))
        {
            return null;
        }

        if (targetMember.SpanStart < documentationComment.SpanStart)
        {
            return null;
        }

        return targetMember;
    }

    private static void AddLineBreaks(IList<string> lines, string newLine)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            lines[i] = lines[i] + newLine;
        }
    }

    private static void IndentLines(List<string> lines, string? indentText)
    {
        for (var i = 1; i < lines.Count; i++)
        {
            lines[i] = indentText + lines[i];
        }
    }

    public DocumentationCommentSnippet? GetDocumentationCommentSnippetOnEnterTyped(SyntaxTree syntaxTree, SourceText text, int position, in DocumentationCommentOptions options, CancellationToken cancellationToken)
    {
        // Don't attempt to generate a new XML doc comment on ENTER if the option to auto-generate
        // them isn't set. Regardless of the option, we should generate exterior trivia (i.e. /// or ''')
        // on ENTER inside an existing XML doc comment.

        if (options.AutoXmlDocCommentGeneration)
        {
            var result = GenerateDocumentationCommentAfterEnter(syntaxTree, text, position, options, cancellationToken);
            if (result != null)
            {
                return result;
            }
        }

        return GenerateExteriorTriviaAfterEnter(syntaxTree, text, position, options, cancellationToken);
    }

    private DocumentationCommentSnippet? GenerateDocumentationCommentAfterEnter(SyntaxTree syntaxTree, SourceText text, int position, in DocumentationCommentOptions options, CancellationToken cancellationToken)
    {
        // Find the documentation comment before the new line that was just pressed
        var token = GetTokenToLeft(syntaxTree, position, cancellationToken);
        if (!IsDocCommentNewLine(token))
        {
            return null;
        }

        var newLine = options.NewLine;
        var lines = GetDocumentationCommentLines(token, text, options, out var indentText, out _, out _);
        if (lines == null)
        {
            return null;
        }

        var newText = string.Join(string.Empty, lines);
        var offset = lines[0].Length + lines[1].Length - newLine.Length;

        // Shave off final line break or add trailing indent if necessary
        var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(position, findInsideTrivia: false);
        if (IsEndOfLineTrivia(trivia))
        {
            newText = newText[..^newLine.Length];
        }
        else
        {
            newText += indentText;
        }

        var replaceSpan = token.Span;
        var currentLine = text.Lines.GetLineFromPosition(position);
        var currentLinePosition = currentLine.GetFirstNonWhitespacePosition();
        if (currentLinePosition.HasValue)
        {
            var start = token.Span.Start;
            replaceSpan = new TextSpan(start, currentLinePosition.Value - start);
        }

        return new DocumentationCommentSnippet(replaceSpan, newText, offset);
    }

    public DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCommandInvoke(SyntaxTree syntaxTree, SourceText text, int position, in DocumentationCommentOptions options, CancellationToken cancellationToken)
    {
        var targetMember = GetTargetMember(syntaxTree, text, position, cancellationToken);
        if (targetMember == null)
        {
            return null;
        }

        var token = targetMember.GetFirstToken();
        var startPosition = token.SpanStart;
        var line = text.Lines.GetLineFromPosition(startPosition);
        Debug.Assert(!line.IsEmptyOrWhitespace());

        var lines = GetDocumentationCommentStubLines(targetMember, string.Empty);
        Debug.Assert(lines.Count > 2);

        var newLine = options.NewLine;
        AddLineBreaks(lines, newLine);

        // Add indents
        var lineOffset = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(options.TabSize);
        Debug.Assert(line.Start + lineOffset == startPosition);

        var indentText = lineOffset.CreateIndentationString(options.UseTabs, options.TabSize);
        IndentLines(lines, indentText);

        lines[^1] = lines[^1] + indentText;

        var comments = string.Join(string.Empty, lines);
        var offset = lines[0].Length + lines[1].Length - newLine.Length;

        // For a command we don't replace a token, but insert before it
        var replaceSpan = new TextSpan(token.Span.Start, 0);

        return new DocumentationCommentSnippet(replaceSpan, comments, offset);
    }

    private DocumentationCommentSnippet? GenerateExteriorTriviaAfterEnter(SyntaxTree syntaxTree, SourceText text, int position, in DocumentationCommentOptions options, CancellationToken cancellationToken)
    {
        // Find the documentation comment before the new line that was just pressed
        var token = GetTokenToLeft(syntaxTree, position, cancellationToken);
        if (!IsDocCommentNewLine(token) && HasSkippedTrailingTrivia(token))
        {
            // See PressingEnter_InsertSlashes11 for an example of
            // a case where multiple skipped tokens trivia appear at the same position.
            // In that case, we need to ask for the token from the next position over.
            token = GetTokenToLeft(syntaxTree, position + 1, cancellationToken);

            if (!IsDocCommentNewLine(token))
            {
                return null;
            }
        }

        var currentLine = text.Lines.GetLineFromPosition(position);
        if (currentLine.LineNumber == 0)
        {
            return null;
        }

        // Previous line must begin with a doc comment
        var previousLine = text.Lines[currentLine.LineNumber - 1];
        var previousLineText = previousLine.ToString().Trim();
        if (!previousLineText.StartsWith(ExteriorTriviaText, StringComparison.Ordinal))
        {
            return null;
        }

        var nextLineStartsWithDocComment = text.Lines.Count > currentLine.LineNumber + 1 &&
            text.Lines[currentLine.LineNumber + 1].ToString().Trim().StartsWith(ExteriorTriviaText, StringComparison.Ordinal);

        // if previous line has only exterior trivia, current line is empty and next line doesn't begin
        // with exterior trivia then stop inserting auto generated xml doc string
        if (previousLineText.Equals(ExteriorTriviaText) &&
            string.IsNullOrWhiteSpace(currentLine.ToString()) &&
            !nextLineStartsWithDocComment)
        {
            return null;
        }

        var documentationComment = token.GetAncestor<TDocumentationComment>();
        if (IsMultilineDocComment(documentationComment))
        {
            return null;
        }

        if (EndsWithSingleExteriorTrivia(documentationComment) && currentLine.IsEmptyOrWhitespace() && !nextLineStartsWithDocComment)
        {
            return null;
        }

        return GetDocumentationCommentSnippetFromPreviousLine(options, currentLine, previousLine);
    }

    public DocumentationCommentSnippet GetDocumentationCommentSnippetFromPreviousLine(in DocumentationCommentOptions options, TextLine currentLine, TextLine previousLine)
    {
        var insertionText = CreateInsertionTextFromPreviousLine(previousLine, options);

        var firstNonWhitespaceOffset = currentLine.GetFirstNonWhitespaceOffset();
        var replaceSpan = firstNonWhitespaceOffset != null
            ? TextSpan.FromBounds(currentLine.Start, currentLine.Start + firstNonWhitespaceOffset.Value)
            : currentLine.Span;

        return new DocumentationCommentSnippet(replaceSpan, insertionText, insertionText.Length);
    }

    private string CreateInsertionTextFromPreviousLine(TextLine previousLine, in DocumentationCommentOptions options)
    {
        var previousLineText = previousLine.ToString();
        var firstNonWhitespaceColumn = previousLineText.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(options.TabSize);

        var trimmedPreviousLine = previousLineText.Trim();
        Debug.Assert(trimmedPreviousLine.StartsWith(ExteriorTriviaText), "Unexpected: previous line does not begin with doc comment exterior trivia.");

        // skip exterior trivia.
        trimmedPreviousLine = trimmedPreviousLine[3..];

        var firstNonWhitespaceOffsetInPreviousXmlText = trimmedPreviousLine.GetFirstNonWhitespaceOffset();

        var extraIndent = firstNonWhitespaceOffsetInPreviousXmlText != null
            ? trimmedPreviousLine[..firstNonWhitespaceOffsetInPreviousXmlText.Value]
            : " ";

        return firstNonWhitespaceColumn.CreateIndentationString(options.UseTabs, options.TabSize) + ExteriorTriviaText + extraIndent;
    }
}
