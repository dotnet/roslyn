// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    internal abstract partial class AbstractIndentationService
    {
        internal abstract class AbstractIndenter
        {
            protected readonly OptionSet OptionSet;
            protected readonly SyntacticDocument Document;
            protected readonly ITextSnapshotLine LineToBeIndented;
            protected readonly int TabSize;
            protected readonly CancellationToken CancellationToken;

            protected readonly SyntaxTree Tree;
            protected readonly IEnumerable<IFormattingRule> Rules;
            protected readonly BottomUpBaseIndentationFinder Finder;

            private static readonly Func<SyntaxToken, bool> _tokenHasDirective = tk => tk.ContainsDirectives &&
                                                  (tk.LeadingTrivia.Any(tr => tr.IsDirective) || tk.TrailingTrivia.Any(tr => tr.IsDirective));

            public AbstractIndenter(Document document, IEnumerable<IFormattingRule> rules, OptionSet optionSet, ITextSnapshotLine lineToBeIndented, CancellationToken cancellationToken)
            {
                this.OptionSet = optionSet;
                this.Document = SyntacticDocument.CreateAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                this.LineToBeIndented = lineToBeIndented;
                this.TabSize = this.OptionSet.GetOption(FormattingOptions.TabSize, this.Document.Root.Language);
                this.CancellationToken = cancellationToken;

                this.Rules = rules;
                this.Tree = this.Document.SyntaxTree;
                this.Finder = new BottomUpBaseIndentationFinder(
                         new ChainedFormattingRules(this.Rules, OptionSet),
                         this.TabSize,
                         this.OptionSet.GetOption(FormattingOptions.IndentationSize, this.Document.Root.Language),
                         tokenStream: null,
                         lastToken: default(SyntaxToken));
            }

            public abstract IndentationResult? GetDesiredIndentation();

            protected IndentationResult IndentFromStartOfLine(int addedSpaces)
            {
                return new IndentationResult(this.LineToBeIndented.Start, addedSpaces);
            }

            protected IndentationResult GetIndentationOfToken(SyntaxToken token)
            {
                return GetIndentationOfToken(token, addedSpaces: 0);
            }

            protected IndentationResult GetIndentationOfToken(SyntaxToken token, int addedSpaces)
            {
                return GetIndentationOfPosition(new SnapshotPoint(LineToBeIndented.Snapshot, token.SpanStart), addedSpaces);
            }

            protected IndentationResult GetIndentationOfLine(ITextSnapshotLine lineToMatch)
            {
                return GetIndentationOfLine(lineToMatch, addedSpaces: 0);
            }

            protected IndentationResult GetIndentationOfLine(ITextSnapshotLine lineToMatch, int addedSpaces)
            {
                var firstNonWhitespace = lineToMatch.GetFirstNonWhitespacePosition();
                firstNonWhitespace = firstNonWhitespace ?? lineToMatch.End.Position;

                return GetIndentationOfPosition(new SnapshotPoint(lineToMatch.Snapshot, firstNonWhitespace.Value), addedSpaces);
            }

            protected IndentationResult GetIndentationOfPosition(SnapshotPoint position, int addedSpaces)
            {
                var tree = Document.SyntaxTree;

                if (tree.OverlapsHiddenPosition(GetNormalizedSpan(position), CancellationToken))
                {
                    // Oops, the line we want to line up to is either hidden, or is in a different
                    // visible region.
                    var root = tree.GetRoot(CancellationToken.None);
                    var token = root.FindTokenFromEnd(LineToBeIndented.Start);
                    var indentation = Finder.GetIndentationOfCurrentPosition(tree, token, LineToBeIndented.Start, CancellationToken.None);

                    return new IndentationResult(LineToBeIndented.Start, indentation);
                }

                return new IndentationResult(position, addedSpaces);
            }

            private TextSpan GetNormalizedSpan(SnapshotPoint position)
            {
                if (LineToBeIndented.Start < position)
                {
                    return TextSpan.FromBounds(LineToBeIndented.Start, position);
                }

                return TextSpan.FromBounds(position, LineToBeIndented.Start);
            }

            protected ITextSnapshotLine GetPreviousNonBlankOrPreprocessorLine()
            {
                if (Tree == null)
                {
                    throw new ArgumentNullException(nameof(Tree));
                }

                if (LineToBeIndented.LineNumber <= 0)
                {
                    return null;
                }

                var syntaxFacts = this.Document.Document.GetLanguageService<ISyntaxFactsService>();
                var snapshot = this.LineToBeIndented.Snapshot;

                var lineNumber = this.LineToBeIndented.LineNumber - 1;
                while (lineNumber >= 0)
                {
                    var actualLine = snapshot.GetLineFromLineNumber(lineNumber);

                    // Empty line, no indentation to match.
                    if (string.IsNullOrWhiteSpace(actualLine.GetText()))
                    {
                        lineNumber--;
                        continue;
                    }

                    // No preprocessors in the entire tree, so this
                    // line definitely doesn't have one
                    var root = Tree.GetRoot(CancellationToken);
                    if (!root.ContainsDirectives)
                    {
                        return snapshot.GetLineFromLineNumber(lineNumber);
                    }

                    // This line is inside an inactive region. Examine the 
                    // first preceding line not in an inactive region.
                    var disabledSpan = syntaxFacts.GetInactiveRegionSpanAroundPosition(this.Tree, actualLine.Extent.Start, CancellationToken);
                    if (disabledSpan != default(TextSpan))
                    {
                        var targetLine = snapshot.GetLineNumberFromPosition(disabledSpan.Start);
                        lineNumber = targetLine - 1;
                        continue;
                    }

                    // A preprocessor directive starts on this line.
                    if (HasPreprocessorCharacter(actualLine) &&
                        root.DescendantTokens(actualLine.Extent.Span.ToTextSpan(), tk => tk.FullWidth() > 0).Any(_tokenHasDirective))
                    {
                        lineNumber--;
                        continue;
                    }

                    return snapshot.GetLineFromLineNumber(lineNumber);
                }

                return null;
            }

            protected abstract bool HasPreprocessorCharacter(ITextSnapshotLine currentLine);
        }
    }
}
