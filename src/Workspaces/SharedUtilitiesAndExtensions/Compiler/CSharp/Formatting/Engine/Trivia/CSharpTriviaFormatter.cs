// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal partial class CSharpTriviaFormatter : AbstractTriviaFormatter
    {
        private bool _succeeded = true;

        private SyntaxTrivia _newLine;

        public CSharpTriviaFormatter(
            FormattingContext context,
            ChainedFormattingRules formattingRules,
            SyntaxToken token1,
            SyntaxToken token2,
            string originalString,
            int lineBreaks,
            int spaces)
            : base(context, formattingRules, token1, token2, originalString, lineBreaks, spaces)
        {
        }

        protected override bool Succeeded()
            => _succeeded;

        protected override bool IsWhitespace(SyntaxTrivia trivia)
            => trivia.RawKind == (int)SyntaxKind.WhitespaceTrivia;

        protected override bool IsEndOfLine(SyntaxTrivia trivia)
            => trivia.RawKind == (int)SyntaxKind.EndOfLineTrivia;

        protected override bool IsWhitespace(char ch)
            => SyntaxFacts.IsWhitespace(ch);

        protected override bool IsNewLine(char ch)
            => SyntaxFacts.IsNewLine(ch);

        protected override SyntaxTrivia CreateWhitespace(string text)
            => SyntaxFactory.Whitespace(text);

        protected override SyntaxTrivia CreateEndOfLine()
        {
            if (_newLine == default)
            {
                _newLine = SyntaxFactory.EndOfLine(Context.Options.NewLine);
            }

            return _newLine;
        }

        protected override LineColumnRule GetLineColumnRuleBetween(SyntaxTrivia trivia1, LineColumnDelta existingWhitespaceBetween, bool implicitLineBreak, SyntaxTrivia trivia2, CancellationToken cancellationToken)
        {
            if (IsStartOrEndOfFile(trivia1, trivia2))
            {
                return LineColumnRule.PreserveLinesWithAbsoluteIndentation(lines: 0, indentation: 0);
            }

            // [trivia] [whitespace] [token] case
            if (trivia2.IsKind(SyntaxKind.None))
            {
                if (IsMultilineComment(trivia1))
                {
                    var insertNewLine = this.FormattingRules.GetAdjustNewLinesOperation(this.Token1, this.Token2) != null;
                    return LineColumnRule.PreserveLinesWithGivenIndentation(lines: insertNewLine ? 1 : 0);
                }

                if (existingWhitespaceBetween.Spaces != this.Spaces)
                {
                    return LineColumnRule.PreserveWithGivenSpaces(spaces: this.Spaces);
                }

                return LineColumnRule.Preserve;
            }

            // preprocessor case
            if (SyntaxFacts.IsPreprocessorDirective(trivia2.Kind()))
            {
                // Check for immovable preprocessor directives, which are bad directive trivia
                // without a preceding line break
                if (trivia2.IsKind(SyntaxKind.BadDirectiveTrivia) && existingWhitespaceBetween.Lines == 0 && !implicitLineBreak)
                {
                    _succeeded = false;
                    return LineColumnRule.Preserve;
                }

                // if current line is the first line of the file, don't put extra line 1
                var lines = (trivia1.IsKind(SyntaxKind.None) && this.Token1.IsKind(SyntaxKind.None)) ? 0 : 1;

                if (trivia2.Kind() is SyntaxKind.RegionDirectiveTrivia or SyntaxKind.EndRegionDirectiveTrivia)
                {
                    // When we have a '#region' in conditionally disabled conditional (e.g, `#if false`), we cannot determine a correct indentation for '#region'.
                    // So we preserve the existing indentation.
                    // To figure whether we are in a disabled region, we do the following:
                    // - Starting from the given trivia, keep going back.
                    // - Once we find a disabled text, we know this is a disabled region.
                    // - If we find a BranchingDirectiveTriviaSyntax, we can directly determine whether it's active or not via BranchTaken property.
                    var previous = trivia2;
                    while ((previous = previous.GetPreviousTrivia(previous.SyntaxTree, cancellationToken)) != default)
                    {
                        if (previous.IsKind(SyntaxKind.DisabledTextTrivia))
                        {
                            return LineColumnRule.Preserve;
                        }
                        else if (previous.IsKind(SyntaxKind.EndIfDirectiveTrivia))
                        {
                            // To correctly determine if we are in a disabled region or not, we'll have to ignore
                            // everything until the corresponding #if (keeping in mind nested `#if` conditionals).
                            // Then, continue from there.
                            // For now, we don't do that and assume we are in active region.
                            break;
                        }
                        else if (previous.HasStructure && previous.GetStructure() is BranchingDirectiveTriviaSyntax branchingDirectiveTrivia)
                        {
                            if (!branchingDirectiveTrivia.BranchTaken)
                            {
                                return LineColumnRule.Preserve;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    return LineColumnRule.PreserveLinesWithDefaultIndentation(lines);
                }

                return LineColumnRule.PreserveLinesWithAbsoluteIndentation(lines, indentation: 0);
            }

            // comments case
            if (trivia2.IsRegularOrDocComment())
            {
                // Start of new comments group.
                //
                // 1. Comment groups must contain the same kind of comments
                // 2. Every block comment is a group of its own
                if (!trivia1.IsKind(trivia2.Kind()) || trivia2.IsMultiLineComment() || trivia2.IsMultiLineDocComment() || existingWhitespaceBetween.Lines > 1)
                {
                    if (this.FormattingRules.GetAdjustNewLinesOperation(this.Token1, this.Token2) != null)
                    {
                        return LineColumnRule.PreserveLinesWithDefaultIndentation(lines: 0);
                    }

                    return LineColumnRule.PreserveLinesWithGivenIndentation(lines: 0);
                }

                // comments after existing comment
                if (existingWhitespaceBetween.Lines == 0)
                {
                    return LineColumnRule.PreserveLinesWithGivenIndentation(lines: 0);
                }

                return LineColumnRule.PreserveLinesWithFollowingPrecedingIndentation;
            }

            if (trivia2.IsKind(SyntaxKind.SkippedTokensTrivia))
            {
                // if there is any skipped tokens, it is not possible to format this trivia range.
                _succeeded = false;
            }

            return LineColumnRule.Preserve;
        }

        protected override bool ContainsImplicitLineBreak(SyntaxTrivia trivia)
        {
            if (!trivia.HasStructure)
            {
                return false;
            }

            var structuredTrivia = trivia.GetStructure();

            return structuredTrivia != null &&
                structuredTrivia.HasTrailingTrivia &&
                structuredTrivia.GetTrailingTrivia().Any(SyntaxKind.EndOfLineTrivia);
        }

        private bool IsStartOrEndOfFile(SyntaxTrivia trivia1, SyntaxTrivia trivia2)
        {
            // Below represents the tokens for a file:
            // (None) - It is the start of the file. This means there are no previous tokens.
            // (...) - All the tokens in the compilation unit.
            // (EndOfFileToken) - This is the synthetic end of file token. Should be treated as the end of the file.
            // (None) - It is the end of the file. This means there are no more tokens.

            var isStartOrEndOfFile = (this.Token1.RawKind == 0 || this.Token2.RawKind == 0) && (trivia1.Kind() == 0 || trivia2.Kind() == 0);
            var isAtEndOfFileToken = (Token2.IsKind(SyntaxKind.EndOfFileToken) && trivia2.Kind() == 0);

            return isStartOrEndOfFile || isAtEndOfFileToken;
        }

        private static bool IsMultilineComment(SyntaxTrivia trivia1)
            => trivia1.IsMultiLineComment() || trivia1.IsMultiLineDocComment();

        private bool TryFormatMultiLineCommentTrivia(LineColumn lineColumn, SyntaxTrivia trivia, out SyntaxTrivia result)
        {
            if (trivia.Kind() == SyntaxKind.MultiLineCommentTrivia)
            {
                var indentation = lineColumn.Column;
                var indentationDelta = indentation - GetExistingIndentation(trivia);
                if (indentationDelta != 0)
                {
                    var multiLineComment = trivia.ToFullString().ReindentStartOfXmlDocumentationComment(
                        false /* forceIndentation */,
                        indentation,
                        indentationDelta,
                        Options.UseTabs,
                        Options.TabSize,
                        Options.NewLine);

                    var multilineCommentTrivia = SyntaxFactory.ParseLeadingTrivia(multiLineComment);
                    Contract.ThrowIfFalse(multilineCommentTrivia.Count == 1);

                    // Preserve annotations on this comment as the formatter is only supposed to touch whitespace, and
                    // thus should make it appear as if the original comment trivia (with annotations) is still there in
                    // the resultant formatted tree.
                    var firstTrivia = multilineCommentTrivia.First();
                    result = trivia.CopyAnnotationsTo(firstTrivia);
                    return true;
                }
            }

            result = default;
            return false;
        }

        protected override LineColumnDelta Format(
            LineColumn lineColumn, SyntaxTrivia trivia, ArrayBuilder<SyntaxTrivia> changes,
            CancellationToken cancellationToken)
        {
            if (trivia.HasStructure)
            {
                return FormatStructuredTrivia(lineColumn, trivia, changes, cancellationToken);
            }

            if (TryFormatMultiLineCommentTrivia(lineColumn, trivia, out var newComment))
            {
                changes.Add(newComment);
                return GetLineColumnDelta(lineColumn, newComment);
            }

            changes.Add(trivia);
            return GetLineColumnDelta(lineColumn, trivia);
        }

        protected override LineColumnDelta Format(
            LineColumn lineColumn, SyntaxTrivia trivia, ArrayBuilder<TextChange> changes, CancellationToken cancellationToken)
        {
            if (trivia.HasStructure)
            {
                return FormatStructuredTrivia(lineColumn, trivia, changes, cancellationToken);
            }

            if (TryFormatMultiLineCommentTrivia(lineColumn, trivia, out var newComment))
            {
                changes.Add(new TextChange(trivia.FullSpan, newComment.ToFullString()));
                return GetLineColumnDelta(lineColumn, newComment);
            }

            return GetLineColumnDelta(lineColumn, trivia);
        }

        private SyntaxTrivia FormatDocumentComment(LineColumn lineColumn, SyntaxTrivia trivia)
        {
            var indentation = lineColumn.Column;

            if (trivia.IsSingleLineDocComment())
            {
                var text = trivia.ToFullString();

                // When the doc comment is parsed from source, even if it is only one
                // line long, the end-of-line will get included into the trivia text.
                // If the doc comment was parsed from a text fragment, there may not be
                // an end-of-line at all. We need to trim the end before we check the
                // number of line breaks in the text.
                var textWithoutFinalNewLine = text.TrimEnd(null);
                if (!textWithoutFinalNewLine.ContainsLineBreak())
                {
                    return trivia;
                }

                var singleLineDocumentationCommentExteriorCommentRewriter = new DocumentationCommentExteriorCommentRewriter(
                    true /* forceIndentation */,
                    indentation,
                    0 /* indentationDelta */,
                    this.Options);
                var newTrivia = singleLineDocumentationCommentExteriorCommentRewriter.VisitTrivia(trivia);

                return newTrivia;
            }

            var indentationDelta = indentation - GetExistingIndentation(trivia);
            if (indentationDelta == 0)
            {
                return trivia;
            }

            var multiLineDocumentationCommentExteriorCommentRewriter = new DocumentationCommentExteriorCommentRewriter(
                    false /* forceIndentation */,
                    indentation,
                    indentationDelta,
                    this.Options);
            var newMultiLineTrivia = multiLineDocumentationCommentExteriorCommentRewriter.VisitTrivia(trivia);

            return newMultiLineTrivia;
        }

        private LineColumnDelta FormatStructuredTrivia(
            LineColumn lineColumn, SyntaxTrivia trivia, ArrayBuilder<SyntaxTrivia> changes, CancellationToken cancellationToken)
        {
            if (trivia.Kind() == SyntaxKind.SkippedTokensTrivia)
            {
                // don't touch anything if it contains skipped tokens
                _succeeded = false;
                changes.Add(trivia);

                return GetLineColumnDelta(lineColumn, trivia);
            }

            // TODO : make document comment to be formatted by structured trivia formatter as well.
            if (!trivia.IsDocComment())
            {
                var result = CSharpStructuredTriviaFormatEngine.Format(
                    trivia, this.InitialLineColumn.Column, this.Options, this.FormattingRules, cancellationToken);
                var formattedTrivia = SyntaxFactory.Trivia((StructuredTriviaSyntax)result.GetFormattedRoot(cancellationToken));

                changes.Add(formattedTrivia);
                return GetLineColumnDelta(lineColumn, formattedTrivia);
            }

            var docComment = FormatDocumentComment(lineColumn, trivia);
            changes.Add(docComment);

            return GetLineColumnDelta(lineColumn, docComment);
        }

        private LineColumnDelta FormatStructuredTrivia(
            LineColumn lineColumn, SyntaxTrivia trivia, ArrayBuilder<TextChange> changes, CancellationToken cancellationToken)
        {
            if (trivia.Kind() == SyntaxKind.SkippedTokensTrivia)
            {
                // don't touch anything if it contains skipped tokens
                _succeeded = false;
                return GetLineColumnDelta(lineColumn, trivia);
            }

            // TODO : make document comment to be formatted by structured trivia formatter as well.
            if (!trivia.IsDocComment())
            {
                var result = CSharpStructuredTriviaFormatEngine.Format(
                    trivia, this.InitialLineColumn.Column, this.Options, this.FormattingRules, cancellationToken);
                if (result.GetTextChanges(cancellationToken).Count == 0)
                {
                    return GetLineColumnDelta(lineColumn, trivia);
                }

                changes.AddRange(result.GetTextChanges(cancellationToken));

                var formattedTrivia = SyntaxFactory.Trivia((StructuredTriviaSyntax)result.GetFormattedRoot(cancellationToken));
                return GetLineColumnDelta(lineColumn, formattedTrivia);
            }

            var docComment = FormatDocumentComment(lineColumn, trivia);
            if (docComment != trivia)
            {
                changes.Add(new TextChange(trivia.FullSpan, docComment.ToFullString()));
            }

            return GetLineColumnDelta(lineColumn, docComment);
        }

        protected override bool LineContinuationFollowedByWhitespaceComment(SyntaxTrivia trivia, SyntaxTrivia nextTrivia)
        {
            return false;
        }

        /// <summary>
        /// C# never passes a VB Comment
        /// </summary>
        /// <param name="trivia"></param>
        protected override bool IsVisualBasicComment(SyntaxTrivia trivia)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
