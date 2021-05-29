﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal abstract class AbstractDocumentationCommentSnippetService<TDocumentationComment, TMemberNode> : IDocumentationCommentSnippetService
        where TDocumentationComment : SyntaxNode, IStructuredTriviaSyntax
        where TMemberNode : SyntaxNode
    {
        protected abstract TMemberNode? GetContainingMember(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        protected abstract bool SupportsDocumentationComments(TMemberNode member);
        protected abstract bool HasDocumentationComment(TMemberNode member);
        protected abstract int GetPrecedingDocumentationCommentCount(TMemberNode member);
        protected abstract bool IsMemberDeclaration(TMemberNode member);
        protected abstract List<string> GetDocumentationCommentStubLines(TMemberNode member);

        protected abstract SyntaxToken GetTokenToRight(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        protected abstract SyntaxToken GetTokenToLeft(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        protected abstract bool IsDocCommentNewLine(SyntaxToken token);
        protected abstract bool IsEndOfLineTrivia(SyntaxTrivia trivia);

        protected abstract bool IsSingleExteriorTrivia(TDocumentationComment documentationComment, bool allowWhitespace = false);
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
            DocumentOptionSet options,
            CancellationToken cancellationToken)
        {
            if (!options.GetOption(DocumentationCommentOptions.AutoXmlDocCommentGeneration))
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

            var lines = GetDocumentationCommentLines(token, text, options, out var indentText);
            if (lines == null)
            {
                return null;
            }

            var newLine = options.GetOption(FormattingOptions.NewLine);

            var lastLine = lines[^1];
            lines[^1] = lastLine.Substring(0, lastLine.Length - newLine.Length);

            var comments = string.Join(string.Empty, lines);
            var offset = lines[0].Length + lines[1].Length - newLine.Length;

            // When typing we don't replace a token, but insert before it
            var replaceSpan = new TextSpan(token.Span.Start, 0);

            return new DocumentationCommentSnippet(replaceSpan, comments, offset);
        }

        private List<string>? GetDocumentationCommentLines(SyntaxToken token, SourceText text, DocumentOptionSet options, out string? indentText)
        {
            indentText = null;
            var documentationComment = token.GetAncestor<TDocumentationComment>();

            if (documentationComment == null || !IsSingleExteriorTrivia(documentationComment))
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

            var lines = GetDocumentationCommentStubLines(targetMember);
            Debug.Assert(lines.Count > 2);

            var newLine = options.GetOption(FormattingOptions.NewLine);
            AddLineBreaks(lines, newLine);

            // Shave off initial three slashes
            lines[0] = lines[0][3..];

            // Add indents
            var lineOffset = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(options.GetOption(FormattingOptions.TabSize));
            indentText = lineOffset.CreateIndentationString(options.GetOption(FormattingOptions.UseTabs), options.GetOption(FormattingOptions.TabSize));

            IndentLines(lines, indentText);
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

            if (targetMember == null || !IsMemberDeclaration(targetMember))
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

        public DocumentationCommentSnippet? GetDocumentationCommentSnippetOnEnterTyped(SyntaxTree syntaxTree, SourceText text, int position, DocumentOptionSet options, CancellationToken cancellationToken)
        {
            // Don't attempt to generate a new XML doc comment on ENTER if the option to auto-generate
            // them isn't set. Regardless of the option, we should generate exterior trivia (i.e. /// or ''')
            // on ENTER inside an existing XML doc comment.

            if (options.GetOption(DocumentationCommentOptions.AutoXmlDocCommentGeneration))
            {
                var result = GenerateDocumentationCommentAfterEnter(syntaxTree, text, position, options, cancellationToken);
                if (result != null)
                {
                    return result;
                }
            }

            return GenerateExteriorTriviaAfterEnter(syntaxTree, text, position, options, cancellationToken);
        }

        private DocumentationCommentSnippet? GenerateDocumentationCommentAfterEnter(SyntaxTree syntaxTree, SourceText text, int position, DocumentOptionSet options, CancellationToken cancellationToken)
        {
            // Find the documentation comment before the new line that was just pressed
            var token = GetTokenToLeft(syntaxTree, position, cancellationToken);
            if (!IsDocCommentNewLine(token))
            {
                return null;
            }

            var newLine = options.GetOption(FormattingOptions.NewLine);
            var lines = GetDocumentationCommentLines(token, text, options, out var indentText);
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
                newText = newText.Substring(0, newText.Length - newLine.Length);
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

        public DocumentationCommentSnippet? GetDocumentationCommentSnippetOnCommandInvoke(SyntaxTree syntaxTree, SourceText text, int position, DocumentOptionSet options, CancellationToken cancellationToken)
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

            var lines = GetDocumentationCommentStubLines(targetMember);
            Debug.Assert(lines.Count > 2);

            var newLine = options.GetOption(FormattingOptions.NewLine);
            AddLineBreaks(lines, newLine);

            // Add indents
            var lineOffset = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(options.GetOption(FormattingOptions.TabSize));
            Debug.Assert(line.Start + lineOffset == startPosition);

            var indentText = lineOffset.CreateIndentationString(options.GetOption(FormattingOptions.UseTabs), options.GetOption(FormattingOptions.TabSize));
            IndentLines(lines, indentText);

            lines[^1] = lines[^1] + indentText;

            var comments = string.Join(string.Empty, lines);
            var offset = lines[0].Length + lines[1].Length - newLine.Length;

            // For a command we don't replace a token, but insert before it
            var replaceSpan = new TextSpan(token.Span.Start, 0);

            return new DocumentationCommentSnippet(replaceSpan, comments, offset);
        }

        private DocumentationCommentSnippet? GenerateExteriorTriviaAfterEnter(SyntaxTree syntaxTree, SourceText text, int position, DocumentOptionSet options, CancellationToken cancellationToken)
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

        public DocumentationCommentSnippet GetDocumentationCommentSnippetFromPreviousLine(DocumentOptionSet options, TextLine currentLine, TextLine previousLine)
        {
            var insertionText = CreateInsertionTextFromPreviousLine(previousLine, options);

            var firstNonWhitespaceOffset = currentLine.GetFirstNonWhitespaceOffset();
            var replaceSpan = firstNonWhitespaceOffset != null
                ? TextSpan.FromBounds(currentLine.Start, currentLine.Start + firstNonWhitespaceOffset.Value)
                : currentLine.Span;

            return new DocumentationCommentSnippet(replaceSpan, insertionText, insertionText.Length);
        }

        private string CreateInsertionTextFromPreviousLine(TextLine previousLine, DocumentOptionSet options)
        {
            var useTabs = options.GetOption(FormattingOptions.UseTabs);
            var tabSize = options.GetOption(FormattingOptions.TabSize);

            var previousLineText = previousLine.ToString();
            var firstNonWhitespaceColumn = previousLineText.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);

            var trimmedPreviousLine = previousLineText.Trim();
            Debug.Assert(trimmedPreviousLine.StartsWith(ExteriorTriviaText), "Unexpected: previous line does not begin with doc comment exterior trivia.");

            // skip exterior trivia.
            trimmedPreviousLine = trimmedPreviousLine[3..];

            var firstNonWhitespaceOffsetInPreviousXmlText = trimmedPreviousLine.GetFirstNonWhitespaceOffset();

            var extraIndent = firstNonWhitespaceOffsetInPreviousXmlText != null
                ? trimmedPreviousLine.Substring(0, firstNonWhitespaceOffsetInPreviousXmlText.Value)
                : " ";

            return firstNonWhitespaceColumn.CreateIndentationString(useTabs, tabSize) + ExteriorTriviaText + extraIndent;
        }
    }
}
