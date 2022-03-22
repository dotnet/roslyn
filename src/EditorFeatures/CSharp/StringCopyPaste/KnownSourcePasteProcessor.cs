// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
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
                edits.Add(new TextChange(change.OldSpan, modifiedText));
            }

            return edits.ToImmutable();
        }

        /// <summary>
        /// Takes a chunk of pasted text and reparses it as if it was surrounded by the original quotes it had in the
        /// string it came from.  With this we can determine how to interpret things like the escapes in their original
        /// context.  We can also figure out how to deal with copied interpolations.
        /// </summary>
        private string WrapChangeWithOriginalQuotes(string text)
        {
            var textCopiedFrom = _snapshotCopiedFrom.AsText();
            GetTextContentSpans(
                textCopiedFrom, _stringExpressionCopiedFrom, out _, out _,
                out var startQuoteSpan, out var endQuoteSpan);

            var startQuote = textCopiedFrom.ToString(startQuoteSpan);
            var endQuote = textCopiedFrom.ToString(endQuoteSpan);
            if (!IsAnyRawStringExpression(_stringExpressionCopiedFrom))
                return $"{startQuote}{text}{endQuote}";

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
            var end = _stringExpressionCopiedFrom.Span.End;
            while (end > 0 && text[end - 1] == '"')
                end--;

            var whitespaceEnd = end;
            var whitespaceStart = whitespaceEnd;
            while (whitespaceStart > 0 && SyntaxFacts.IsWhitespace(text[whitespaceStart - 1]))
                whitespaceStart--;

            var rawStringIndentation = textCopiedFrom.ToString(TextSpan.FromBounds(whitespaceStart, whitespaceEnd));

            var leadingWhitespace = text.GetLeadingWhitespace();

            // First, if we don't have enough indentation whitespace in the string, but we do have a portion of the
            // necessary whitespace, then synthesize the remainder we need.
            if (leadingWhitespace.Length < rawStringIndentation.Length)
            {
                if (rawStringIndentation.EndsWith(leadingWhitespace))
                    return $"{startQuote}{rawStringIndentation[..^leadingWhitespace.Length]}{text}{endQuote}";
            }
            else
            {
                // we have a lot of indentation whitespace.  Make sure it's legal though for this raw string.  If so,
                // nothing to do.
                if (leadingWhitespace.StartsWith(rawStringIndentation))
                    return $"{startQuote}{text}{endQuote}";
            }

            // We have something with whitespace incompatible with the raw string indentation.  Just add the required
            // indentation we need to ensure this can parse.  Note: this is a heuristic, and it's possible we could
            // figure out something better here (for example copying just enough indentation whitespace to make things
            // successfully parse).
            return $"{startQuote}{rawStringIndentation}{text}{endQuote}";
        }
    }
}
