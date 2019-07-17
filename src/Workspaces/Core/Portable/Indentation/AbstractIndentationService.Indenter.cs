// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal abstract partial class AbstractIndentationService<TSyntaxRoot>
    {
        protected struct Indenter
        {
            private readonly AbstractIndentationService<TSyntaxRoot> _service;

            public readonly OptionSet OptionSet;
            public readonly TextLine LineToBeIndented;
            public readonly CancellationToken CancellationToken;

            public readonly SyntacticDocument Document;
            public readonly TSyntaxRoot Root;
            public SyntaxTree Tree => Document.SyntaxTree;
            public readonly IEnumerable<AbstractFormattingRule> Rules;
            public readonly BottomUpBaseIndentationFinder Finder;

            private static readonly Func<SyntaxToken, bool> s_tokenHasDirective = tk => tk.ContainsDirectives &&
                                                  (tk.LeadingTrivia.Any(tr => tr.IsDirective) || tk.TrailingTrivia.Any(tr => tr.IsDirective));

            private readonly ISyntaxFactsService _syntaxFacts;
            private readonly int _tabSize;

            public Indenter(
                AbstractIndentationService<TSyntaxRoot> service,
                SyntacticDocument document,
                IEnumerable<AbstractFormattingRule> rules,
                OptionSet optionSet,
                TextLine lineToBeIndented,
                CancellationToken cancellationToken)
            {
                Document = document;

                this._service = service;
                this._syntaxFacts = document.Document.GetLanguageService<ISyntaxFactsService>();
                this.OptionSet = optionSet;
                this.Root = (TSyntaxRoot)document.Root;
                this.LineToBeIndented = lineToBeIndented;
                this._tabSize = this.OptionSet.GetOption(FormattingOptions.TabSize, Root.Language);
                this.CancellationToken = cancellationToken;

                this.Rules = rules;
                this.Finder = new BottomUpBaseIndentationFinder(
                    new ChainedFormattingRules(this.Rules, OptionSet),
                    this._tabSize,
                    this.OptionSet.GetOption(FormattingOptions.IndentationSize, Root.Language),
                    tokenStream: null);
            }

            public IndentationResult GetDesiredIndentation(FormattingOptions.IndentStyle indentStyle)
            {
                // If the caller wants no indent, then we'll return an effective '0' indent.
                if (indentStyle == FormattingOptions.IndentStyle.None)
                {
                    return new IndentationResult(basePosition: 0, offset: 0);
                }

                // find previous line that is not blank.  this will skip over things like preprocessor
                // regions and inactive code.
                var previousLineOpt = GetPreviousNonBlankOrPreprocessorLine();

                // it is beginning of the file, there is no previous line exists. 
                // in that case, indentation 0 is our base indentation.
                if (previousLineOpt == null)
                {
                    return IndentFromStartOfLine(0);
                }

                var previousNonWhitespaceOrPreprocessorLine = previousLineOpt.Value;

                // If the user wants block indentation, then we just return the indentation
                // of the last piece of real code.  
                //
                // TODO(cyrusn): It's not clear to me that this is correct.  Block indentation
                // should probably follow the indentation of hte last non-blank line *regardless
                // if it is inactive/preprocessor region.  By skipping over thse, we are essentially
                // being 'smart', and that seems to be overriding the user desire to have Block
                // indentation.
                if (indentStyle == FormattingOptions.IndentStyle.Block)
                {
                    // If it's block indentation, then just base 
                    return GetIndentationOfLine(previousNonWhitespaceOrPreprocessorLine);
                }

                Debug.Assert(indentStyle == FormattingOptions.IndentStyle.Smart);

                // Because we know that previousLine is not-whitespace, we know that we should be
                // able to get the last non-whitespace position.
                var lastNonWhitespacePosition = previousNonWhitespaceOrPreprocessorLine.GetLastNonWhitespacePosition().Value;

                var token = Root.FindToken(lastNonWhitespacePosition);
                Debug.Assert(token.RawKind != 0, "FindToken should always return a valid token");

                return _service.GetDesiredIndentationWorker(
                    this, token, previousNonWhitespaceOrPreprocessorLine, lastNonWhitespacePosition);
            }

            public bool TryGetSmartTokenIndentation(out IndentationResult indentationResult)
            {
                if (_service.ShouldUseTokenIndenter(this, out var token))
                {
                    // var root = document.GetSyntaxRootSynchronously(cancellationToken);
                    var sourceText = Tree.GetText(CancellationToken);

                    var formatter = _service.CreateSmartTokenFormatter(this);
                    var changes = formatter.FormatTokenAsync(Document.Project.Solution.Workspace, token, CancellationToken)
                                           .WaitAndGetResult(CancellationToken);

                    var updatedSourceText = sourceText.WithChanges(changes);
                    if (LineToBeIndented.LineNumber < updatedSourceText.Lines.Count)
                    {
                        var updatedLine = updatedSourceText.Lines[LineToBeIndented.LineNumber];
                        var offset = updatedLine.GetFirstNonWhitespaceOffset();
                        if (offset != null)
                        {
                            indentationResult = new IndentationResult(
                                basePosition: LineToBeIndented.Start,
                                offset: offset.Value);
                            return true;
                        }
                    }
                }

                indentationResult = default;
                return false;
            }

            public IndentationResult IndentFromStartOfLine(int addedSpaces)
                => new IndentationResult(this.LineToBeIndented.Start, addedSpaces);

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

            private TextLine? GetPreviousNonBlankOrPreprocessorLine()
            {
                if (LineToBeIndented.LineNumber <= 0)
                {
                    return null;
                }

                var sourceText = this.LineToBeIndented.Text;

                var lineNumber = this.LineToBeIndented.LineNumber - 1;
                while (lineNumber >= 0)
                {
                    var actualLine = sourceText.Lines[lineNumber];

                    // Empty line, no indentation to match.
                    if (string.IsNullOrWhiteSpace(actualLine.ToString()))
                    {
                        lineNumber--;
                        continue;
                    }

                    // No preprocessors in the entire tree, so this
                    // line definitely doesn't have one
                    if (!Root.ContainsDirectives)
                    {
                        return sourceText.Lines[lineNumber];
                    }

                    // This line is inside an inactive region. Examine the 
                    // first preceding line not in an inactive region.
                    var disabledSpan = _syntaxFacts.GetInactiveRegionSpanAroundPosition(this.Tree, actualLine.Span.Start, CancellationToken);
                    if (disabledSpan != default)
                    {
                        var targetLine = sourceText.Lines.GetLineFromPosition(disabledSpan.Start).LineNumber;
                        lineNumber = targetLine - 1;
                        continue;
                    }

                    // A preprocessor directive starts on this line.
                    if (HasPreprocessorCharacter(actualLine) &&
                        Root.DescendantTokens(actualLine.Span, tk => tk.FullWidth() > 0).Any(s_tokenHasDirective))
                    {
                        lineNumber--;
                        continue;
                    }

                    return sourceText.Lines[lineNumber];
                }

                return null;
            }

            public int GetCurrentPositionNotBelongToEndOfFileToken(int position)
                => Math.Min(Root.EndOfFileToken.FullSpan.Start, position);

            private bool HasPreprocessorCharacter(TextLine currentLine)
            {
                var text = currentLine.ToString();
                Debug.Assert(!string.IsNullOrWhiteSpace(text));

                var trimmedText = text.Trim();

                return trimmedText[0] == '#';
            }
        }
    }
}
