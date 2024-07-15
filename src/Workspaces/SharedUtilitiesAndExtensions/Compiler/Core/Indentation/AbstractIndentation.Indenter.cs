// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Indentation;

internal abstract partial class AbstractIndentation<TSyntaxRoot>
{
    protected readonly struct Indenter
    {
        private readonly AbstractIndentation<TSyntaxRoot> _service;

        public readonly IndentationOptions Options;
        public readonly TextLine LineToBeIndented;
        public readonly CancellationToken CancellationToken;

        public readonly IEnumerable<AbstractFormattingRule> Rules;
        public readonly BottomUpBaseIndentationFinder Finder;

        private readonly ISyntaxFacts _syntaxFacts;
        private readonly int _tabSize;

        public readonly SyntaxTree Tree;
        public readonly SourceText Text;
        public readonly TSyntaxRoot Root;

        public readonly ISmartTokenFormatter SmartTokenFormatter;

        public Indenter(
            AbstractIndentation<TSyntaxRoot> service,
            SyntaxTree tree,
            ImmutableArray<AbstractFormattingRule> rules,
            IndentationOptions options,
            TextLine lineToBeIndented,
            ISmartTokenFormatter smartTokenFormatter,
            CancellationToken cancellationToken)
        {
            _service = service;
            _syntaxFacts = service.SyntaxFacts;
            Options = options;
            Tree = tree;
            Text = tree.GetText(cancellationToken);
            Root = (TSyntaxRoot)tree.GetRoot(cancellationToken);
            LineToBeIndented = lineToBeIndented;
            _tabSize = options.FormattingOptions.TabSize;
            SmartTokenFormatter = smartTokenFormatter;
            CancellationToken = cancellationToken;

            Rules = rules;
            Finder = new BottomUpBaseIndentationFinder(
                new ChainedFormattingRules(this.Rules, options.FormattingOptions),
                _tabSize,
                options.FormattingOptions.IndentationSize,
                tokenStream: null,
                service.HeaderFacts);
        }

        public IndentationResult? GetDesiredIndentation(FormattingOptions2.IndentStyle indentStyle)
        {
            // If the caller wants no indent, then we'll return an effective '0' indent.
            if (indentStyle == FormattingOptions2.IndentStyle.None)
                return null;

            // If the user has explicitly set 'block' indentation, or they're in an inactive preprocessor region,
            // then just do simple block indentation.
            if (indentStyle == FormattingOptions2.IndentStyle.Block ||
                _syntaxFacts.IsInInactiveRegion(this.Tree, LineToBeIndented.Start, this.CancellationToken))
            {
                return GetDesiredBlockIndentation();
            }

            Debug.Assert(indentStyle == FormattingOptions2.IndentStyle.Smart);
            return GetDesiredSmartIndentation();
        }

        private readonly IndentationResult? GetDesiredSmartIndentation()
        {
            // For smart indent, we generally will be computing from either the previous token in the code, or in a
            // few special cases, the previous trivia.
            var token = TryGetPrecedingVisibleToken();

            // Look to see if we're immediately following some visible piece of trivia.  There may
            // be some cases where we'll base our indent off of that.  However, we only do this as 
            // long as we're immediately after the trivia.  If there are any blank lines between us
            // then we consider that unimportant for indentation.
            var trivia = TryGetImmediatelyPrecedingVisibleTrivia();

            if (token == null && trivia == null)
                return null;

            return _service.GetDesiredIndentationWorker(this, token, trivia);
        }

        private readonly SyntaxTrivia? TryGetImmediatelyPrecedingVisibleTrivia()
        {
            if (LineToBeIndented.LineNumber == 0)
                return null;

            var previousLine = this.Text.Lines[LineToBeIndented.LineNumber - 1];
            var lastPos = previousLine.GetLastNonWhitespacePosition();
            if (lastPos == null)
                return null;

            var trivia = Root.FindTrivia(lastPos.Value);
            if (trivia == default)
                return null;

            return trivia;
        }

        private readonly SyntaxToken? TryGetPrecedingVisibleToken()
        {
            var token = Root.FindToken(LineToBeIndented.Start);

            // we'll either be after the token at the end of a line, or before a token.  We compute indentation
            // based on the preceding token.  So if we're before a token, look back to the previous token to
            // determine what our indentation is based off of.
            if (token.SpanStart >= LineToBeIndented.Start)
            {
                token = token.GetPreviousToken();

                // Skip past preceding blank tokens.  This can happen in VB for example where there can be
                // whitespace tokens in things like xml literals.  We want to get the first visible token that we
                // would actually anch would anchor indentation off of.
                while (token != default && string.IsNullOrWhiteSpace(token.ToString()))
                    token = token.GetPreviousToken();
            }

            if (token == default)
                return null;

            return token;
        }

        private IndentationResult? GetDesiredBlockIndentation()
        {
            // Block indentation is simple, we keep walking back lines until we find a line with any sort of
            // text on it.  We then set our indentation to whatever the indentation of that line was.
            for (var currentLine = this.LineToBeIndented.LineNumber - 1; currentLine >= 0; currentLine--)
            {
                var line = this.Text.Lines[currentLine];
                var offset = line.GetFirstNonWhitespaceOffset();
                if (offset == null)
                    continue;

                // Found the previous non-blank line.  indent to the same level that it is at
                return new IndentationResult(basePosition: line.Start + offset.Value, offset: 0);
            }

            // Couldn't find a previous non-blank line.
            return null;
        }

        public bool TryGetSmartTokenIndentation(out IndentationResult indentationResult)
        {
            if (_service.ShouldUseTokenIndenter(this, out var token))
            {
                var changes = SmartTokenFormatter.FormatToken(token, CancellationToken);

                var updatedSourceText = Text.WithChanges(changes);
                if (LineToBeIndented.LineNumber < updatedSourceText.Lines.Count)
                {
                    var updatedLine = updatedSourceText.Lines[LineToBeIndented.LineNumber];
                    var nonWhitespaceOffset = updatedLine.GetFirstNonWhitespaceOffset();
                    if (nonWhitespaceOffset != null)
                    {
                        // 'nonWhitespaceOffset' is simply an int indicating how many
                        // *characters* of indentation to include.  For example, an indentation
                        // string of \t\t\t would just count for nonWhitespaceOffset of '3' (one
                        // for each tab char).
                        //
                        // However, what we want is the true columnar offset for the line.
                        // That's what our caller (normally the editor) needs to determine where
                        // to actually put the caret and what whitespace needs to proceed it.
                        //
                        // This can be computed with GetColumnFromLineOffset which again looks
                        // at the contents of the line, but this time evaluates how \t characters 
                        // should translate to column chars.
                        var offset = updatedLine.GetColumnFromLineOffset(nonWhitespaceOffset.Value, _tabSize);
                        indentationResult = new IndentationResult(basePosition: LineToBeIndented.Start, offset: offset);
                        return true;
                    }
                }
            }

            indentationResult = default;
            return false;
        }

        public IndentationResult IndentFromStartOfLine(int addedSpaces)
            => new(this.LineToBeIndented.Start, addedSpaces);

        public IndentationResult GetIndentationOfToken(SyntaxToken token)
            => GetIndentationOfToken(token, addedSpaces: 0);

        public IndentationResult GetIndentationOfToken(SyntaxToken token, int addedSpaces)
            => GetIndentationOfPosition(token.SpanStart, addedSpaces);

        public IndentationResult GetIndentationOfLine(TextLine lineToMatch)
            => GetIndentationOfLine(lineToMatch, addedSpaces: 0);

        public IndentationResult GetIndentationOfLine(TextLine lineToMatch, int addedSpaces)
        {
            var firstNonWhitespace = lineToMatch.GetFirstNonWhitespacePosition();
            firstNonWhitespace ??= lineToMatch.End;

            return GetIndentationOfPosition(firstNonWhitespace.Value, addedSpaces);
        }

        private IndentationResult GetIndentationOfPosition(int position, int addedSpaces)
        {
            if (this.Tree.OverlapsHiddenPosition(GetNormalizedSpan(position), CancellationToken))
            {
                // Oops, the line we want to line up to is either hidden, or is in a different
                // visible region.
                var token = Root.FindTokenFromEnd(LineToBeIndented.Start);
                var indentation = Finder.GetIndentationOfCurrentPosition(this.Tree, token, LineToBeIndented.Start, CancellationToken.None);

                return new IndentationResult(LineToBeIndented.Start, indentation);
            }

            return new IndentationResult(position, addedSpaces);
        }

        private TextSpan GetNormalizedSpan(int position)
        {
            if (LineToBeIndented.Start < position)
            {
                return TextSpan.FromBounds(LineToBeIndented.Start, position);
            }

            return TextSpan.FromBounds(position, LineToBeIndented.Start);
        }

        public int GetCurrentPositionNotBelongToEndOfFileToken(int position)
            => Math.Min(Root.EndOfFileToken.FullSpan.Start, position);
    }
}
