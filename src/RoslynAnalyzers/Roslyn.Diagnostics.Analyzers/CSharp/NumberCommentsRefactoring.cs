// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Roslyn.Diagnostics.Analyzers
{
    /// <summary>
    /// This refactoring looks for numbered comments `// N` or `// N, N+1, ...` on non-empty lines within string literals
    /// and checks that the numbers are sequential. If they are not, the refactoring is offered.
    ///
    /// This pattern is commonly used by compiler tests.
    /// Comments that don't look like numbered comments are left alone. For instance, any comment that contains alpha characters.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(NumberCommentsRefactoring)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    internal sealed class NumberCommentsRefactoring() : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var literal = await context.TryGetRelevantNodeAsync<LiteralExpressionSyntax>(CSharpRefactoringHelpers.Instance).ConfigureAwait(false);
            if (literal is null)
                return;

            if (literal.Kind() == SyntaxKind.StringLiteralExpression &&
                !IsProperlyNumbered(literal.Token.ValueText))
            {
                var action = CodeAction.Create(
                   RoslynDiagnosticsAnalyzersResources.FixNumberedComments,
                   c => FixCommentsAsync(context.Document, literal, c));
                context.RegisterRefactoring(action);
            }
        }

        private static async Task<Document> FixCommentsAsync(Document document, LiteralExpressionSyntax stringLiteral, CancellationToken c)
        {
            var newValueText = FixComments(stringLiteral.Token.ValueText, prefix: null);
            var oldText = stringLiteral.Token.Text;
            var newText = FixComments(oldText, getPrefix(oldText));

            var oldToken = stringLiteral.Token;
            var newToken = SyntaxFactory.Token(oldToken.LeadingTrivia, kind: oldToken.Kind(), text: newText, valueText: newValueText, oldToken.TrailingTrivia);
            var newStringLiteral = stringLiteral.Update(newToken);

            var editor = await DocumentEditor.CreateAsync(document, c).ConfigureAwait(false);
            editor.ReplaceNode(stringLiteral, newStringLiteral);
            return editor.GetChangedDocument();

            static string? getPrefix(string text)
            {
                if (text.StartsWith("""
                    @"
                    """, StringComparison.OrdinalIgnoreCase))
                {
                    return """
                        @"
                        """;
                }

                if (text.StartsWith("""
                    "
                    """, StringComparison.OrdinalIgnoreCase))
                {
                    return """
                        "
                        """;
                }

                return null;
            }
        }

        public static bool IsProperlyNumbered(string text)
        {
            int exptectedNumber = 1;
            int cursor = 0;
            do
            {
                var (eolOrEofIndex, newLine) = FindNewLineOrEndOfFile(cursor, text, hasPrefix: false);

                // find the last comment between cursor and newLineIndex
                (int commentStartIndex, _) = FindNumberComment(cursor, eolOrEofIndex, text);
                if (commentStartIndex > 0)
                {
                    var separatedNumbers = text[commentStartIndex..eolOrEofIndex];
                    var numbers = separatedNumbers.Split(',').Select(removeWhiteSpace);
                    foreach (var number in numbers)
                    {
                        if (string.IsNullOrEmpty(number))
                        {
                            return false;
                        }

                        if (!int.TryParse(number, out var actualNumber) || exptectedNumber != actualNumber)
                        {
                            return false;
                        }

                        exptectedNumber++;
                    }
                }

                cursor = eolOrEofIndex + newLine.Length;
            }
            while (cursor < text.Length);

            return true;

            static string removeWhiteSpace(string self)
                => new(self.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>
        /// Returns the index of the end of line terminator or the end of file.
        /// If hasPrefix, we'll consider the terminating double-quotes (") to be the end of file.
        /// </summary>
        internal static (int Index, string NewLine) FindNewLineOrEndOfFile(int index, string comment, bool hasPrefix)
        {
            int length = GetStringLengthIgnoringQuote(comment, hasPrefix);
            for (int i = index; i < length; i++)
            {
                var current = comment[i];
                if (current == '\n')
                {
                    return (i, "\n");
                }
                else if (current == '\r')
                {
                    if (i + 1 < length && comment[i + 1] == '\n')
                    {
                        return (i, "\r\n");
                    }

                    return (i, "\r");
                }
            }

            return (length, ""); // EOF
        }

        internal static (int FoundIndex, int CommaCount) FindNumberComment(int cursor, int newLineIndex, string comment)
        {
            int commaCount = 0;
            for (int index = newLineIndex - 1; index > cursor + 2; index--)
            {
                var current = comment[index];
                if (current == ',')
                {
                    commaCount++;
                }

                if (current == '/' && comment[index - 1] == '/' && comment[index - 2] == ' ')
                {
                    // found start of comment
                    return (index + 1, commaCount);
                }

                if (!IsDigitOrComma(current))
                {
                    break;
                }
            }

            return (-1, 0);
        }

        internal static bool IsDigitOrComma(char c)
        {
            if (c is >= '0' and <= '9')
            {
                return true;
            }

            if (c is ' ' or ',')
            {
                return true;
            }

            return false;
        }

        internal static int GetStringLengthIgnoringQuote(string text, bool hasPrefix)
        {
            if (hasPrefix)
            {
                return text.Length - 1;
            }

            return text.Length;
        }

        private static string FixComments(string text, string? prefix)
        {
            var builder = new StringBuilder();
            int nextNumber = 1;
            int cursor = 0;

            if (prefix != null)
            {
                builder.Append(prefix);
                cursor += prefix.Length;
            }

            int length = GetStringLengthIgnoringQuote(text, prefix != null);

            do
            {
                var (eolOrEofIndex, newLine) = FindNewLineOrEndOfFile(cursor, text, prefix != null);
                // find the last comment between cursor and newLineIndex
                (int commentStartIndex, int commaCount) = FindNumberComment(cursor, eolOrEofIndex, text);
                if (commentStartIndex > 0)
                {
                    builder.Append(text, cursor, commentStartIndex - cursor);
                    appendFixedNumberComment(builder, commaCount, ref nextNumber);
                    builder.Append(newLine);
                }
                else
                {
                    builder.Append(text, cursor, eolOrEofIndex + newLine.Length - cursor);
                }

                cursor = eolOrEofIndex + newLine.Length;
            }
            while (cursor < length);

            if (prefix != null)
            {
                builder.Append('"');
            }

            return builder.ToString();

            static void appendFixedNumberComment(StringBuilder builder, int commaCount, ref int nextNumber)
            {
                for (int commaIndex = 0; commaIndex <= commaCount; commaIndex++)
                {
                    builder.Append(' ');
                    builder.Append(nextNumber.ToString(CultureInfo.InvariantCulture));
                    nextNumber++;
                    if (commaIndex < commaCount)
                    {
                        builder.Append(',');
                    }
                }
            }
        }
    }
}
