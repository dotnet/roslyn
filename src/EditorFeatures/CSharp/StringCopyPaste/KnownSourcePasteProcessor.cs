// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    using static StringCopyPasteHelpers;

    internal class KnownSourcePasteProcessor : AbstractPasteProcessor
    {
        private readonly ExpressionSyntax _stringExpressionCopiedFrom;
        private readonly ITextSnapshot _snapshotCopiedFrom;

        public KnownSourcePasteProcessor(
            string newLine,
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            Document documentBeforePaste,
            Document documentAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            ExpressionSyntax stringExpressionCopiedFrom,
            ITextSnapshot snapshotCopiedFrom)
            : base(newLine, snapshotBeforePaste, snapshotAfterPaste, documentBeforePaste, documentAfterPaste, stringExpressionBeforePaste)
        {
            _stringExpressionCopiedFrom = stringExpressionCopiedFrom;
            _snapshotCopiedFrom = snapshotCopiedFrom;
        }

        public override ImmutableArray<TextChange> GetEdits(CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);

            foreach (var change in Changes)
            {
                var wrappedChange = WrapChangeWithOriginalQuotes(change.NewText);
                var parsedChange = SyntaxFactory.ParseExpression(wrappedChange);

                // If for some reason we can't actually successfully parse this copied text, then bail out.
                if (ContainsError(parsedChange))
                    return default;

                var modifiedText = TransformValueToDestinationKind(parsedChange);
                edits.Add(new TextChange(change.OldSpan.ToTextSpan(), modifiedText));
            }

            return edits.ToImmutable();
        }

        private string TransformValueToDestinationKind(ExpressionSyntax parsedChange)
        {
            // we have a matrix of every string source type to every string destination type.
            // 
            // Normal string
            // Interpolated string
            // Verbatim string
            // Verbatim interpolated string
            // Raw single line string
            // Raw multi line string
            // Raw interpolated line string
            // Raw interpolated multi-line string.
            return (parsedChange, StringExpressionBeforePaste) switch
            {
                (LiteralExpressionSyntax pastedText, LiteralExpressionSyntax pastedInto) => TransformLiteralToLiteral(pastedText, pastedInto),
                (LiteralExpressionSyntax pastedText, InterpolatedStringExpressionSyntax pastedInto) => TransformLiteralToInterpolatedString(pastedText, pastedInto),
                (InterpolatedStringExpressionSyntax pastedText, LiteralExpressionSyntax pastedInto) => TransformInterpolatedStringToLiteral(pastedText, pastedInto),
                (InterpolatedStringExpressionSyntax pastedText, InterpolatedStringExpressionSyntax pastedInto) => TransformInterpolatedStringToInterpolatedString(pastedText, pastedInto),
                _ => throw ExceptionUtilities.Unreachable,
            };
        }

        private string TransformLiteralToLiteral(LiteralExpressionSyntax pastedText, LiteralExpressionSyntax pastedInto)
        {
            // Pasting into raw strings can be complex.  A single-line raw string may need to become multi-line, and
            // a multi-line raw string has indentation whitespace we have to respect.
            if (IsAnyRawStringExpression(pastedInto))
                TransformLiteralToRawStringLiteral(pastedText, pastedInto);

            // All other literal->literal pastes are trivial.  The compiler has already determined the 'value' of the
            // the string we're pasting.  So we just need to get that value and ensure it is properly
            var textValue = pastedText.Token.ValueText;
            return EscapeForNonRawStringLiteral(
                pastedInto.Token.IsVerbatimStringLiteral(),
                isInterpolated: false, trySkipExistingEscapes: false, textValue);
        }

        private void TransformLiteralToRawStringLiteral(LiteralExpressionSyntax pastedText, LiteralExpressionSyntax pastedInto)
        {
            throw new NotImplementedException();
        }

        private string TransformLiteralToInterpolatedString(LiteralExpressionSyntax pastedText, InterpolatedStringExpressionSyntax pastedInto)
        {
            // Pasting into raw strings can be complex.  A single-line raw string may need to become multi-line, and
            // a multi-line raw string has indentation whitespace we have to respect.
            if (IsAnyRawStringExpression(pastedInto))
                TransformLiteralToInterpolatedRawStringLiteral(pastedText, pastedInto);

            // All other literal->literal pastes are trivial.  The compiler has already determined the 'value' of the
            // the string we're pasting.  So we just need to get that value and ensure it is properly
            var textValue = pastedText.Token.ValueText;
            return EscapeForNonRawStringLiteral(
                pastedInto.StringStartToken.Kind() is SyntaxKind.InterpolatedVerbatimStringStartToken,
                isInterpolated: true, trySkipExistingEscapes: false, textValue);
        }

        private void TransformLiteralToInterpolatedRawStringLiteral(LiteralExpressionSyntax pastedText, InterpolatedStringExpressionSyntax pastedInto)
        {
            throw new NotImplementedException();
        }

        private string TransformInterpolatedStringToLiteral(InterpolatedStringExpressionSyntax pastedText, LiteralExpressionSyntax pastedInto)
        {
            throw new NotImplementedException();
        }

        private string TransformInterpolatedStringToInterpolatedString(InterpolatedStringExpressionSyntax pastedText, InterpolatedStringExpressionSyntax pastedInto)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Takes a chunk of pasted text and reparses it as if it was surrounded by the original quotes it had in the
        /// string it came from.  With this we can determine how to interpret things like the escapes in their original
        /// context.  We can also figure out how to deal with copied interpolations.
        /// </summary>
        private string WrapChangeWithOriginalQuotes(string pastedText)
        {
            var textCopiedFrom = _snapshotCopiedFrom.AsText();
            GetTextContentSpans(
                textCopiedFrom, _stringExpressionCopiedFrom, out _, out _,
                out var startQuoteSpan, out var endQuoteSpan);

            var startQuote = textCopiedFrom.ToString(startQuoteSpan);
            var endQuote = textCopiedFrom.ToString(endQuoteSpan);
            if (!IsAnyMultiLineRawStringExpression(_stringExpressionCopiedFrom))
                return $"{startQuote}{pastedText}{endQuote}";

            // With a raw string we have the issue that the contents may need to be indented properly in order for the
            // string to parsed successfully.  Because we're using the original start/end quote to wrap the text that
            // was pasted this normally is not an issue.  However, it can be a problem in the following case:
            //
            //      var source = """
            //              exiting text
            //              [|copy
            //              this|]
            //              existing text
            //              """
            //
            // In this case, the first line of the text will not start with enough indentation and we will generate:
            //
            // """
            // copy
            //              this
            //              """
            //
            // To address this.  We ensure that if the content starts with spaces to not be a problem.
            var endLine = textCopiedFrom.Lines.GetLineFromPosition(_stringExpressionCopiedFrom.Span.End);
            var rawStringIndentation = endLine.GetLeadingWhitespace();

            var pastedTextWhitespace = pastedText.GetLeadingWhitespace();

            // First, if we don't have enough indentation whitespace in the string, but we do have a portion of the
            // necessary whitespace, then synthesize the remainder we need.
            if (pastedTextWhitespace.Length < rawStringIndentation.Length)
            {
                if (rawStringIndentation.EndsWith(pastedTextWhitespace))
                    return $"{startQuote}{rawStringIndentation[..^pastedTextWhitespace.Length]}{pastedText}{endQuote}";
            }
            else
            {
                // We have a lot of indentation whitespace.  Make sure it's legal though for this raw string.  If so,
                // nothing to do.
                if (pastedTextWhitespace.StartsWith(rawStringIndentation))
                    return $"{startQuote}{pastedText}{endQuote}";
            }

            // We have something with whitespace incompatible with the raw string indentation.  Just add the required
            // indentation we need to ensure this can parse.  Note: this is a heuristic, and it's possible we could
            // figure out something better here (for example copying just enough indentation whitespace to make things
            // successfully parse).
            return $"{startQuote}{rawStringIndentation}{pastedText}{endQuote}";
        }
    }
}
