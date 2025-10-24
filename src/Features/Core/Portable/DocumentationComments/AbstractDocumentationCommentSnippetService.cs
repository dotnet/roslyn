// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
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
    protected abstract List<string> GetDocumentationCommentStubLines(TMemberNode member, string existingCommentText, DocumentationCommentOptions options);

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

    private bool IsAtEndOfDocCommentTriviaOnBlankLine(SourceText text, int endPosition)
    {
        var commentStart = endPosition - "///".Length;
        if (commentStart < 0)
            return false;

        var docCommentChar = this.DocumentationCommentCharacter[0];
        if (text[commentStart + 0] != docCommentChar ||
            text[commentStart + 1] != docCommentChar ||
            text[commentStart + 2] != docCommentChar)
        {
            return false;
        }

        var line = text.Lines.GetLineFromPosition(commentStart);
        for (var i = line.Start; i < commentStart; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
                return false;
        }

        return true;
    }

    public DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCharacterTyped(
        ParsedDocument document,
        int position,
        in DocumentationCommentOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.AutoXmlDocCommentGeneration)
            return null;

        // Only generate if the position is immediately after '///', with only whitespace before. And that is the only
        // documentation comment on the target member.

        var syntaxTree = document.SyntaxTree;
        var text = document.Text;

        if (!IsAtEndOfDocCommentTriviaOnBlankLine(text, position))
            return null;

        var token = syntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
        if (position != token.SpanStart)
            return null;

        var lines = GetDocumentationCommentLines(token, text, options, out var indentText, out var caretOffset, out var spanToReplaceLength);
        if (lines == null)
            return null;

        lines[^1] = lines[^1][..^options.NewLine.Length];

        return new DocumentationCommentSnippet(
            new TextSpan(token.Span.Start, spanToReplaceLength),
            string.Join(string.Empty, lines),
            caretOffset,
            position,
            GetContainingMember(syntaxTree, position, cancellationToken),
            indentText);
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
            return null;

        // Add indents
        var lineOffset = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(options.TabSize);
        indentText = lineOffset.CreateIndentationString(options.UseTabs, options.TabSize);

        IndentLines(lines, indentText);

        // Calculate caret offset based on whether we're using collapsed (single-line) mode
        if (options.GenerateSummaryTagOnSingleLine)
        {
            // For single-line mode, position caret inside <summary></summary>
            // The format after shaving off "///" is: " <summary></summary>"
            // We want the caret between > and <
            var summaryOpenTagLength = "<summary>".Length;
            caretOffset = " ".Length + summaryOpenTagLength;
        }
        else
        {
            // Multi-line mode: caret goes on the second line, with one space after the doc comment XML
            // GetDocumentationCommentStubLines ensures that space is always there
            caretOffset = lines[0].Length + indentText.Length + ExteriorTriviaText.Length + " ".Length;
        }

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

        var lines = GetDocumentationCommentStubLines(targetMember, existingCommentText, options);
        Debug.Assert(lines.Count >= 1);

        AddLineBreaks(lines, options.NewLine);

        // Shave off initial three slashes
        lines[0] = lines[0][3..];

        return lines;
    }

    public bool IsValidTargetMember(ParsedDocument document, int position, CancellationToken cancellationToken)
        => GetTargetMember(document, position, cancellationToken) != null;

    private TMemberNode? GetTargetMember(ParsedDocument document, int position, CancellationToken cancellationToken)
    {
        var syntaxTree = document.SyntaxTree;
        var text = document.Text;

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
            lines[i] = indentText + lines[i];
    }

    public DocumentationCommentSnippet? GetDocumentationCommentSnippetOnEnterTyped(ParsedDocument document, int position, in DocumentationCommentOptions options, CancellationToken cancellationToken)
    {
        // Don't attempt to generate a new XML doc comment on ENTER if the option to auto-generate
        // them isn't set. Regardless of the option, we should generate exterior trivia (i.e. /// or ''')
        // on ENTER inside an existing XML doc comment.

        if (options.AutoXmlDocCommentGeneration)
        {
            var result = GenerateDocumentationCommentAfterEnter(document, position, options, cancellationToken);
            if (result != null)
                return result;
        }

        return GenerateExteriorTriviaAfterEnter(document, position, options, cancellationToken);
    }

    private DocumentationCommentSnippet? GenerateDocumentationCommentAfterEnter(ParsedDocument document, int position, in DocumentationCommentOptions options, CancellationToken cancellationToken)
    {
        // Find the documentation comment before the new line that was just pressed
        var syntaxTree = document.SyntaxTree;
        var text = document.Text;

        // We will have already entered the newline for the <enter>.  So we need to look back to the prior line to make
        // sure it is well formed.
        var line = text.Lines.GetLineFromPosition(position);
        if (line.LineNumber == 0)
            return null;

        var previousLine = text.Lines[line.LineNumber - 1];
        if (!IsAtEndOfDocCommentTriviaOnBlankLine(text, previousLine.End))
            return null;

        var token = GetTokenToLeft(syntaxTree, position, cancellationToken);
        if (!IsDocCommentNewLine(token))
            return null;

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

        return new DocumentationCommentSnippet(replaceSpan, newText, offset, position: null, memberNode: null, indentText: null);
    }

    public DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCommandInvoke(
        ParsedDocument document, int position, in DocumentationCommentOptions options, CancellationToken cancellationToken)
    {
        var text = document.Text;

        var targetMember = GetTargetMember(document, position, cancellationToken);
        if (targetMember == null)
        {
            return null;
        }

        var token = targetMember.GetFirstToken();
        var startPosition = token.SpanStart;
        var line = text.Lines.GetLineFromPosition(startPosition);
        Debug.Assert(!line.IsEmptyOrWhitespace());

        var lines = GetDocumentationCommentStubLines(targetMember, string.Empty, options);
        Debug.Assert(lines.Count >= 1);

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

        return new DocumentationCommentSnippet(replaceSpan, comments, offset, position: null, memberNode: null, indentText: null);
    }

    private DocumentationCommentSnippet? GenerateExteriorTriviaAfterEnter(ParsedDocument document, int position, in DocumentationCommentOptions options, CancellationToken cancellationToken)
    {
        var syntaxTree = document.SyntaxTree;
        var text = document.Text;

        // Find the documentation comment before the new line that was just pressed
        var syntaxFacts = document.LanguageServices.GetRequiredService<ISyntaxFactsService>();
        if (syntaxFacts.IsEntirelyWithinStringOrCharOrNumericLiteral(syntaxTree, position, cancellationToken))
            return null;

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

        return new DocumentationCommentSnippet(replaceSpan, insertionText, insertionText.Length, position: null, memberNode: null, indentText: null);
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
