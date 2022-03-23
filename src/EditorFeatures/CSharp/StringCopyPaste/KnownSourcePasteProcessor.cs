// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
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
    using static SyntaxFactory;

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
            // For pastes into non-raw strings, we can just determine how the change should be escaped in-line at that
            // same location the paste originally happened at.  For raw-strings things get more complex as we have to
            // deal with things like indentation and potentially adding newlines to make things legal.
            return IsAnyRawStringExpression(StringExpressionBeforePaste)
                ? GetEditsForRawString(cancellationToken)
                : GetEditsForNonRawString();
        }

        private ImmutableArray<TextChange> GetEditsForNonRawString()
        {
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);

            foreach (var change in Changes)
            {
                var wrappedChange = WrapChangeWithOriginalQuotes(change.NewText);
                var parsedChange = ParseExpression(wrappedChange);

                // If for some reason we can't actually successfully parse this copied text, then bail out.
                if (ContainsError(parsedChange))
                    return default;

                var modifiedText = TransformValueToDestinationKind(parsedChange);
                edits.Add(new TextChange(change.OldSpan.ToTextSpan(), modifiedText));
            }

            return edits.ToImmutable();

            string TransformValueToDestinationKind(ExpressionSyntax parsedChange)
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
                var pastingIntoVerbatimString = IsVerbatimStringExpression(StringExpressionBeforePaste);

                return (parsedChange, StringExpressionBeforePaste) switch
                {
                    (LiteralExpressionSyntax pastedText, LiteralExpressionSyntax) => TransformLiteralToLiteral(pastedText),
                    (LiteralExpressionSyntax pastedText, InterpolatedStringExpressionSyntax) => TransformLiteralToInterpolatedString(pastedText),
                    (InterpolatedStringExpressionSyntax pastedText, LiteralExpressionSyntax) => TransformInterpolatedStringToLiteral(pastedText),
                    (InterpolatedStringExpressionSyntax pastedText, InterpolatedStringExpressionSyntax) => TransformInterpolatedStringToInterpolatedString(pastedText),
                    _ => throw ExceptionUtilities.Unreachable,
                };

                string TransformLiteralToLiteral(LiteralExpressionSyntax pastedText)
                {
                    var textValue = pastedText.Token.ValueText;
                    return EscapeForNonRawStringLiteral(
                        isVerbatim: pastingIntoVerbatimString, isInterpolated: false, trySkipExistingEscapes: false, textValue);
                }

                string TransformLiteralToInterpolatedString(LiteralExpressionSyntax pastedText)
                {
                    var textValue = pastedText.Token.ValueText;
                    return EscapeForNonRawStringLiteral(
                        isVerbatim: pastingIntoVerbatimString, isInterpolated: true, trySkipExistingEscapes: false, textValue);
                }

                string TransformInterpolatedStringToLiteral(InterpolatedStringExpressionSyntax pastedText)
                {
                    using var _ = PooledStringBuilder.GetInstance(out var builder);
                    foreach (var content in pastedText.Contents)
                    {
                        if (content is InterpolatedStringTextSyntax stringText)
                        {
                            builder.Append(EscapeForNonRawStringLiteral(
                                pastingIntoVerbatimString, isInterpolated: true, trySkipExistingEscapes: false, stringText.TextToken.ValueText));
                        }
                        else if (content is InterpolationSyntax interpolation)
                        {
                            // we're copying an interpolation from an interpolated string to a string literal. For example,
                            // we're pasting `{x + y}` into the middle of `"goobar"`.  One thing we could potentially do in the
                            // future is split the literal into `"goo" + $"{x + y}" + "bar"`.  However, it's unclear if that
                            // would actually be desirable as `$"{x + x}"` may have no meaning in the destination location. So,
                            // for now, we do the simple thing and just treat the interpolation as raw text that should just be
                            // escaped as appropriate into the destination.
                            builder.Append(EscapeForNonRawStringLiteral(
                                pastingIntoVerbatimString, isInterpolated: false, trySkipExistingEscapes: false, interpolation.ToString()));
                        }
                    }

                    return builder.ToString();
                }

                string TransformInterpolatedStringToInterpolatedString(InterpolatedStringExpressionSyntax pastedText)
                {
                    using var _ = PooledStringBuilder.GetInstance(out var builder);
                    foreach (var content in pastedText.Contents)
                    {
                        if (content is InterpolatedStringTextSyntax stringText)
                        {
                            builder.Append(EscapeForNonRawStringLiteral(
                                pastingIntoVerbatimString, isInterpolated: false, trySkipExistingEscapes: false, stringText.TextToken.ValueText));
                        }
                        else if (content is InterpolationSyntax interpolation)
                        {
                            // we're moving an interpolation from one interpolation to another.  This can just be copied
                            // wholesale *except* for the format literal portion (e.g. `{...:XXXX}` which may have to be updated
                            // for the destination type.
                            if (interpolation.FormatClause is not null)
                            {
                                var oldToken = interpolation.FormatClause.FormatStringToken;
                                var newToken = Token(
                                    oldToken.LeadingTrivia, oldToken.Kind(),
                                    EscapeForNonRawStringLiteral(
                                        pastingIntoVerbatimString, isInterpolated: false, trySkipExistingEscapes: false, oldToken.ValueText),
                                    oldToken.ValueText, oldToken.TrailingTrivia);

                                interpolation = interpolation.ReplaceToken(oldToken, newToken);
                            }

                            builder.Append(interpolation.ToString());
                        }
                    }

                    return builder.ToString();
                }
            }
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

        private ImmutableArray<TextChange> GetEditsForRawString(CancellationToken cancellationToken)
        {
            // Determine the basic updates to the pasted pieces we need to make to properly unescape the pieces for a
            // raw string.

            // After we've inserted the text we may now have made the raw string illegal (for example, having too many
            // quotes in it now).  Update the delimiters to make the content legal if necessary.
            // if the content we're going to add itself contains quotes, then figure out how many start/end quotes the
            // final string literal will need (which also gives us the number of quotes to add to the start/end).
            //
            // note: we don't have to do this if the paste was successful.  Instead, we'll just process the contents,
            // adjusting whitespace below.
            var contentChanges = GetRawStringContentChanges(cancellationToken);

            var quotesToAdd = GetQuotesToAddToRawString();
            var dollarSignsToAdd = GetDollarSignsToAddToRawString();

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var editsToMake);

            // First, add any extra dollar signs needed.
            if (dollarSignsToAdd != null)
                editsToMake.Add(new TextChange(new TextSpan(StringExpressionBeforePaste.Span.Start, 0), dollarSignsToAdd));

            // Then any quotes to your starting delimiter
            if (quotesToAdd != null)
                editsToMake.Add(new TextChange(new TextSpan(TextContentsSpansBeforePaste.First().Start, 0), quotesToAdd));

            // Then add the actual changes in the content.
            editsToMake.AddRange(contentChanges);

            // Then add any extra end quotes needed.
            if (quotesToAdd != null)
                editsToMake.Add(new TextChange(new TextSpan(TextContentsSpansBeforePaste.Last().End, 0), quotesToAdd));

            return editsToMake.ToImmutable();
        }

        private string GetDollarSignsToAddToRawString()
        {
            throw new NotImplementedException();
        }

        private string GetQuotesToAddToRawString()
        {
            throw new NotImplementedException();
        }

        private ImmutableArray<TextChange> GetRawStringContentChanges(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
