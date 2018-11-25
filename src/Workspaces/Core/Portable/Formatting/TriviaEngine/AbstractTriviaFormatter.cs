﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract class AbstractTriviaFormatter
    {
        #region Caches
        private static readonly string[] s_spaceCache;

        /// <summary>
        /// set up space string caches
        /// </summary>
        static AbstractTriviaFormatter()
        {
            s_spaceCache = new string[20];
            for (int i = 0; i < 20; i++)
            {
                s_spaceCache[i] = new string(' ', i);
            }
        }
        #endregion

        /// <summary>
        /// format the trivia at the line column and put changes to the changes
        /// </summary>
        private delegate LineColumnDelta Formatter<T>(LineColumn lineColumn, SyntaxTrivia trivia, List<T> changes, CancellationToken cancellationToken);

        /// <summary>
        /// create whitespace for the delta at the line column and put changes to the changes
        /// </summary>
        private delegate void WhitespaceAppender<T>(LineColumn lineColumn, LineColumnDelta delta, TextSpan span, List<T> changes);

        protected readonly FormattingContext Context;
        protected readonly ChainedFormattingRules FormattingRules;

        protected readonly string OriginalString;
        protected readonly int LineBreaks;
        protected readonly int Spaces;

        protected readonly LineColumn InitialLineColumn;

        protected readonly SyntaxToken Token1;
        protected readonly SyntaxToken Token2;

        private readonly string _language;
        private readonly int _indentation;
        private readonly bool _firstLineBlank;

        public AbstractTriviaFormatter(
            FormattingContext context,
            ChainedFormattingRules formattingRules,
            SyntaxToken token1,
            SyntaxToken token2,
            string originalString,
            int lineBreaks,
            int spaces)
        {
            Contract.ThrowIfNull(context);
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(originalString);

            Contract.ThrowIfFalse(lineBreaks >= 0);
            Contract.ThrowIfFalse(spaces >= 0);

            Contract.ThrowIfTrue(token1 == default && token2 == default);

            this.Context = context;
            this.FormattingRules = formattingRules;
            this.OriginalString = originalString;

            this.Token1 = token1;
            this.Token2 = token2;

            if (token1 == default)
            {
                _language = token2.Language;
            }
            else
            {
                _language = token1.Language;
            }

            this.LineBreaks = lineBreaks;
            this.Spaces = spaces;

            this.InitialLineColumn = GetInitialLineColumn();

            // "Spaces" holds either space counts between two tokens if two are on same line or indentation of token2 if
            // two are on different line. but actual "Indentation" of the line could be different than "Spaces" if there is
            // noisy trivia before token2 on the same line.
            // this.indentation indicates that trivia's indentation
            //
            // ex) [indentation]/** */ token2
            //     [spaces            ]
            _indentation = (this.LineBreaks > 0) ? GetIndentation() : -1;

            // check whether first line between two tokens contains only whitespace
            // depends on this we decide where to insert blank lines at the end
            _firstLineBlank = FirstLineBlank();
        }

        /// <summary>
        /// return whether this formatting succeeded or not
        /// for example, if there is skipped tokens in one of trivia between tokens
        /// we consider formatting this region is failed
        /// </summary>
        protected abstract bool Succeeded();

        /// <summary>
        /// check whether given trivia is whitespace trivia or not
        /// </summary>
        protected abstract bool IsWhitespace(SyntaxTrivia trivia);

        /// <summary>
        /// check whether given trivia is end of line trivia or not
        /// </summary>
        protected abstract bool IsEndOfLine(SyntaxTrivia trivia);

        /// <summary>
        /// check whether given string is either null or whitespace
        /// </summary>
        protected bool IsNullOrWhitespace(string text)
        {
            if (text == null)
            {
                return true;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (!IsWhitespace(text[i]) || !IsNewLine(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// check whether given char is whitespace
        /// </summary>
        protected abstract bool IsWhitespace(char ch);

        /// <summary>
        /// check whether given char is new line char
        /// </summary>
        protected abstract bool IsNewLine(char ch);

        /// <summary>
        /// create whitespace trivia
        /// </summary>
        protected abstract SyntaxTrivia CreateWhitespace(string text);

        /// <summary>
        /// create end of line trivia
        /// </summary>
        protected abstract SyntaxTrivia CreateEndOfLine();

        /// <summary>
        /// return line column rule for the given two trivia
        /// </summary>
        protected abstract LineColumnRule GetLineColumnRuleBetween(SyntaxTrivia trivia1, LineColumnDelta existingWhitespaceBetween, bool implicitLineBreak, SyntaxTrivia trivia2);

        /// <summary>
        /// format the given trivia at the line column position and put result to the changes list
        /// </summary>
        protected abstract LineColumnDelta Format(LineColumn lineColumn, SyntaxTrivia trivia, List<SyntaxTrivia> changes, CancellationToken cancellationToken);

        /// <summary>
        /// format the given trivia at the line column position and put text change result to the changes list
        /// </summary>
        protected abstract LineColumnDelta Format(LineColumn lineColumn, SyntaxTrivia trivia, List<TextChange> changes, CancellationToken cancellationToken);

        /// <summary>
        /// returns true if the trivia contains a Line break
        /// </summary>
        protected abstract bool ContainsImplicitLineBreak(SyntaxTrivia trivia);

        protected int StartPosition
        {
            get
            {
                if (this.Token1.RawKind == 0)
                {
                    return this.TreeInfo.StartPosition;
                }

                return this.Token1.Span.End;
            }
        }

        protected int EndPosition
        {
            get
            {
                if (this.Token2.RawKind == 0)
                {
                    return this.TreeInfo.EndPosition;
                }

                return this.Token2.SpanStart;
            }
        }

        protected TreeData TreeInfo
        {
            get { return this.Context.TreeData; }
        }

        protected OptionSet OptionSet
        {
            get { return this.Context.OptionSet; }
        }

        protected string Language => _language;

        protected TokenStream TokenStream
        {
            get { return this.Context.TokenStream; }
        }

        public List<SyntaxTrivia> FormatToSyntaxTrivia(CancellationToken cancellationToken)
        {
            var changes = ListPool<SyntaxTrivia>.Allocate();

            var lineColumn = FormatTrivia(Format, AddWhitespaceTrivia, changes, cancellationToken);

            // deal with edges
            // insert empty linebreaks at the beginning of trivia list
            AddExtraLines(lineColumn.Line, changes);

            if (Succeeded())
            {
                var temp = new List<SyntaxTrivia>(changes);
                ListPool<SyntaxTrivia>.Free(changes);

                return temp;
            }

            ListPool<SyntaxTrivia>.Free(changes);

            var triviaList = new TriviaList(this.Token1.TrailingTrivia, this.Token2.LeadingTrivia);
            return new List<SyntaxTrivia>(triviaList);
        }

        public List<TextChange> FormatToTextChanges(CancellationToken cancellationToken)
        {
            var changes = ListPool<TextChange>.Allocate();

            var lineColumn = FormatTrivia(Format, AddWhitespaceTextChange, changes, cancellationToken);

            // deal with edges
            // insert empty linebreaks at the beginning of trivia list
            AddExtraLines(lineColumn.Line, changes);

            if (Succeeded())
            {
                return ListPool<TextChange>.ReturnAndFree(changes);
            }

            ListPool<TextChange>.Free(changes);

            return new List<TextChange>();
        }

        private LineColumn FormatTrivia<T>(Formatter<T> formatter, WhitespaceAppender<T> whitespaceAdder, List<T> changes, CancellationToken cancellationToken)
        {
            var lineColumn = this.InitialLineColumn;

            var existingWhitespaceDelta = LineColumnDelta.Default;
            var previousWhitespaceTrivia = default(SyntaxTrivia);
            var previousTrivia = default(SyntaxTrivia);
            var implicitLineBreak = false;

            var list = new TriviaList(this.Token1.TrailingTrivia, this.Token2.LeadingTrivia);
            foreach (var trivia in list)
            {
                if (trivia.RawKind == 0)
                {
                    continue;
                }

                if (IsWhitespaceOrEndOfLine(trivia))
                {
                    if (IsEndOfLine(trivia))
                    {
                        implicitLineBreak = false;
                    }

                    existingWhitespaceDelta = existingWhitespaceDelta.With(
                        GetLineColumnOfWhitespace(
                            lineColumn,
                            previousTrivia,
                            previousWhitespaceTrivia,
                            existingWhitespaceDelta,
                            trivia));

                    previousWhitespaceTrivia = trivia;
                    continue;
                }

                previousWhitespaceTrivia = default;

                lineColumn = FormatFirstTriviaAndWhitespaceAfter(
                    lineColumn,
                    previousTrivia, existingWhitespaceDelta, trivia,
                    formatter, whitespaceAdder,
                    changes, implicitLineBreak, cancellationToken);

                implicitLineBreak = implicitLineBreak || ContainsImplicitLineBreak(trivia);
                existingWhitespaceDelta = LineColumnDelta.Default;

                previousTrivia = trivia;
            }

            lineColumn = FormatFirstTriviaAndWhitespaceAfter(
                lineColumn,
                previousTrivia, existingWhitespaceDelta, default,
                formatter, whitespaceAdder,
                changes, implicitLineBreak, cancellationToken);

            return lineColumn;
        }

        private LineColumn FormatFirstTriviaAndWhitespaceAfter<T>(
            LineColumn lineColumnBeforeTrivia1,
            SyntaxTrivia trivia1,
            LineColumnDelta existingWhitespaceBetween,
            SyntaxTrivia trivia2,
            Formatter<T> format,
            WhitespaceAppender<T> addWhitespaceTrivia,
            List<T> changes,
            bool implicitLineBreak,
            CancellationToken cancellationToken)
        {
            var lineColumnAfterTrivia1 = trivia1.RawKind == 0 ?
                    lineColumnBeforeTrivia1 : lineColumnBeforeTrivia1.With(format(lineColumnBeforeTrivia1, trivia1, changes, cancellationToken));

            var rule = GetOverallLineColumnRuleBetween(trivia1, existingWhitespaceBetween, implicitLineBreak, trivia2);
            var whitespaceDelta = Apply(lineColumnBeforeTrivia1, trivia1, lineColumnAfterTrivia1, existingWhitespaceBetween, trivia2, rule);

            var span = GetTextSpan(trivia1, trivia2);
            addWhitespaceTrivia(lineColumnAfterTrivia1, whitespaceDelta, span, changes);

            return lineColumnAfterTrivia1.With(whitespaceDelta);
        }

        /// <summary>
        /// get line column rule between two trivia
        /// </summary>
        private LineColumnRule GetOverallLineColumnRuleBetween(SyntaxTrivia trivia1, LineColumnDelta existingWhitespaceBetween, bool implicitLineBreak, SyntaxTrivia trivia2)
        {
            var defaultRule = GetLineColumnRuleBetween(trivia1, existingWhitespaceBetween, implicitLineBreak, trivia2);
            GetTokensAtEdgeOfStructureTrivia(trivia1, trivia2, out var token1, out var token2);

            // if there are tokens, try formatting rules to see whether there is a user supplied one
            if (token1.RawKind == 0 || token2.RawKind == 0)
            {
                return defaultRule;
            }

            // use line defined by the token formatting rules
            var lineOperation = this.FormattingRules.GetAdjustNewLinesOperation(token1, token2);

            // there is existing lines, but no line operation
            if (existingWhitespaceBetween.Lines != 0 && lineOperation == null)
            {
                return defaultRule;
            }

            if (lineOperation != null)
            {
                switch (lineOperation.Option)
                {
                    case AdjustNewLinesOption.PreserveLines:
                        if (existingWhitespaceBetween.Lines != 0)
                        {
                            return defaultRule.With(lines: lineOperation.Line, lineOperation: LineColumnRule.LineOperations.Preserve);
                        }

                        break;
                    case AdjustNewLinesOption.ForceLines:
                        return defaultRule.With(lines: lineOperation.Line, lineOperation: LineColumnRule.LineOperations.Force);

                    case AdjustNewLinesOption.ForceLinesIfOnSingleLine:
                        if (this.Context.TokenStream.TwoTokensOnSameLine(token1, token2))
                        {
                            return defaultRule.With(lines: lineOperation.Line, lineOperation: LineColumnRule.LineOperations.Force);
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(lineOperation.Option);
                }
            }

            // use space defined by the regular formatting rules
            var spaceOperation = this.FormattingRules.GetAdjustSpacesOperation(token1, token2);
            if (spaceOperation == null)
            {
                return defaultRule;
            }

            if (spaceOperation != null &&
                spaceOperation.Option == AdjustSpacesOption.DefaultSpacesIfOnSingleLine &&
                spaceOperation.Space == 1)
            {
                return defaultRule;
            }

            return defaultRule.With(spaces: spaceOperation.Space);
        }

        /// <summary>
        /// if the given trivia is the very first or the last trivia between two normal tokens and 
        /// if the trivia is structured trivia, get one token that belongs to the structured trivia and one belongs to the normal token stream
        /// </summary>
        private void GetTokensAtEdgeOfStructureTrivia(SyntaxTrivia trivia1, SyntaxTrivia trivia2, out SyntaxToken token1, out SyntaxToken token2)
        {
            token1 = default;
            if (trivia1.RawKind == 0)
            {
                token1 = this.Token1;
            }
            else if (trivia1.HasStructure)
            {
                var lastToken = trivia1.GetStructure().GetLastToken(includeZeroWidth: true);
                if (ContainsOnlyWhitespace(lastToken.Span.End, lastToken.FullSpan.End))
                {
                    token1 = lastToken;
                }
            }

            token2 = default;
            if (trivia2.RawKind == 0)
            {
                token2 = this.Token2;
            }
            else if (trivia2.HasStructure)
            {
                var firstToken = trivia2.GetStructure().GetFirstToken(includeZeroWidth: true);
                if (ContainsOnlyWhitespace(firstToken.FullSpan.Start, firstToken.SpanStart))
                {
                    token2 = firstToken;
                }
            }
        }

        /// <summary>
        /// check whether string between start and end position only contains whitespace
        /// </summary>
        private bool ContainsOnlyWhitespace(int start, int end)
        {
            var span = TextSpan.FromBounds(start, end);

            for (int i = span.Start - this.Token1.Span.End; i < span.Length; i++)
            {
                if (!char.IsWhiteSpace(this.OriginalString[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// check whether first line between two tokens contains only whitespace
        /// </summary>
        private bool FirstLineBlank()
        {
            // if we see elastic trivia as the first trivia in the trivia list, 
            // we consider it as blank line
            if (this.Token1.TrailingTrivia.Count > 0 &&
                this.Token1.TrailingTrivia[0].IsElastic())
            {
                return true;
            }

            var index = this.OriginalString.IndexOf(IsNewLine);
            if (index < 0)
            {
                return IsNullOrWhitespace(this.OriginalString);
            }

            for (int i = 0; i < index; i++)
            {
                if (!IsWhitespace(this.OriginalString[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private LineColumnDelta Apply(
            LineColumn lineColumnBeforeTrivia1, SyntaxTrivia trivia1, LineColumn lineColumnAfterTrivia1, LineColumnDelta existingWhitespaceBetween, SyntaxTrivia trivia2, LineColumnRule rule)
        {
            // we do not touch spaces adjacent to missing token
            // [missing token] [whitespace] [trivia] or [trivia] [whitespace] [missing token] case
            if ((this.Token1.IsMissing && trivia1.RawKind == 0) ||
                (trivia2.RawKind == 0 && this.Token2.IsMissing))
            {
                // leave things as it is
                return existingWhitespaceBetween;
            }

            var lines = GetRuleLines(rule, lineColumnAfterTrivia1, existingWhitespaceBetween);
            var spaceOrIndentations = GetRuleSpacesOrIndentation(lineColumnBeforeTrivia1, trivia1, lineColumnAfterTrivia1, existingWhitespaceBetween, trivia2, rule);

            return new LineColumnDelta(
                lines,
                spaceOrIndentations,
                whitespaceOnly: true,
                forceUpdate: existingWhitespaceBetween.ForceUpdate);
        }

        private int GetRuleSpacesOrIndentation(
            LineColumn lineColumnBeforeTrivia1, SyntaxTrivia trivia1, LineColumn lineColumnAfterTrivia1, LineColumnDelta existingWhitespaceBetween, SyntaxTrivia trivia2, LineColumnRule rule)
        {
            var lineColumnAfterExistingWhitespace = lineColumnAfterTrivia1.With(existingWhitespaceBetween);

            // next trivia is moved to next line or already on a new line, use indentation
            if (rule.Lines > 0 || lineColumnAfterExistingWhitespace.WhitespaceOnly)
            {
                switch (rule.IndentationOperation)
                {
                    case LineColumnRule.IndentationOperations.Absolute:
                        return Math.Max(0, rule.Indentation);

                    case LineColumnRule.IndentationOperations.Default:
                        return this.Context.GetBaseIndentation(trivia2.RawKind == 0 ? this.EndPosition : trivia2.SpanStart);

                    case LineColumnRule.IndentationOperations.Given:
                        return (trivia2.RawKind == 0) ? this.Spaces : Math.Max(0, _indentation);

                    case LineColumnRule.IndentationOperations.Follow:
                        return Math.Max(0, lineColumnBeforeTrivia1.Column);

                    case LineColumnRule.IndentationOperations.Preserve:
                        return existingWhitespaceBetween.Spaces;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(rule.IndentationOperation);
                }
            }

            // okay, we are not on a its own line, use space information
            switch (rule.SpaceOperation)
            {
                case LineColumnRule.SpaceOperations.Preserve:
                    return Math.Max(rule.Spaces, existingWhitespaceBetween.Spaces);

                case LineColumnRule.SpaceOperations.Force:
                    return Math.Max(rule.Spaces, 0);

                default:
                    throw ExceptionUtilities.UnexpectedValue(rule.SpaceOperation);
            }
        }

        private int GetRuleLines(LineColumnRule rule, LineColumn lineColumnAfterTrivia1, LineColumnDelta existingWhitespaceBetween)
        {
            var adjustedRuleLines = Math.Max(0, rule.Lines - GetTrailingLinesAtEndOfTrivia1(lineColumnAfterTrivia1));

            return (rule.LineOperation == LineColumnRule.LineOperations.Preserve) ? Math.Max(adjustedRuleLines, existingWhitespaceBetween.Lines) : adjustedRuleLines;
        }

        private int GetIndentation()
        {
            var lastText = this.OriginalString.GetLastLineText();
            var initialColumn = (lastText == this.OriginalString) ? this.InitialLineColumn.Column : 0;

            var index = lastText.GetFirstNonWhitespaceIndexInString();
            if (index < 0)
            {
                return this.Spaces;
            }

            var position = lastText.ConvertTabToSpace(this.OptionSet.GetOption(FormattingOptions.TabSize, this.Language), initialColumn, index);
            var tokenPosition = lastText.ConvertTabToSpace(this.OptionSet.GetOption(FormattingOptions.TabSize, this.Language), initialColumn, lastText.Length);

            return this.Spaces - (tokenPosition - position);
        }

        /// <summary>
        /// return 0 or 1 based on line column of the trivia1's end point
        /// this is based on our structured trivia's implementation detail that some structured trivia can have
        /// one new line at the end of the trivia
        /// </summary>
        private int GetTrailingLinesAtEndOfTrivia1(LineColumn lineColumnAfterTrivia1)
        {
            return (lineColumnAfterTrivia1.Column == 0 && lineColumnAfterTrivia1.Line > 0) ? 1 : 0;
        }

        private void AddExtraLines(int linesBetweenTokens, List<SyntaxTrivia> changes)
        {
            if (linesBetweenTokens < this.LineBreaks)
            {
                var lineBreaks = new List<SyntaxTrivia>();
                AddWhitespaceTrivia(
                    LineColumn.Default,
                    new LineColumnDelta(lines: this.LineBreaks - linesBetweenTokens, spaces: 0),
                    lineBreaks);

                changes.InsertRange(GetInsertionIndex(changes), lineBreaks);
            }
        }

        private int GetInsertionIndex(List<SyntaxTrivia> changes)
        {
            // first line is blank or there is no changes. 
            // just insert at the head
            if (_firstLineBlank ||
                changes.Count == 0)
            {
                return 0;
            }

            // try to find end of line
            for (int i = changes.Count - 1; i >= 0; i--)
            {
                // insert right after existing end of line trivia
                if (IsEndOfLine(changes[i]))
                {
                    return i + 1;
                }
            }

            // can't find any line, put blank line right after any trivia that has lines in them
            for (int i = changes.Count - 1; i >= 0; i--)
            {
                if (changes[i].ToFullString().ContainsLineBreak())
                {
                    return i + 1;
                }
            }

            // well, give up and insert at the top
            return 0;
        }

        private void AddExtraLines(int linesBetweenTokens, List<TextChange> changes)
        {
            if (linesBetweenTokens >= this.LineBreaks)
            {
                return;
            }

            if (changes.Count == 0)
            {
                AddWhitespaceTextChange(
                    LineColumn.Default, new LineColumnDelta(lines: this.LineBreaks - linesBetweenTokens, spaces: 0),
                    GetInsertionSpan(changes), changes);
                return;
            }

            if (TryGetMatchingChangeIndex(changes, out var index))
            {
                // already change exist at same position that contains only whitespace
                var delta = GetLineColumnDelta(0, changes[index].NewText);

                changes[index] = GetWhitespaceTextChange(
                    LineColumn.Default,
                    new LineColumnDelta(lines: this.LineBreaks + delta.Lines - linesBetweenTokens, spaces: delta.Spaces),
                    changes[index].Span);
                return;
            }
            else
            {
                var change = GetWhitespaceTextChange(
                    LineColumn.Default,
                    new LineColumnDelta(lines: this.LineBreaks - linesBetweenTokens, spaces: 0),
                    GetInsertionSpan(changes));

                changes.Insert(0, change);
                return;
            }
        }

        private bool TryGetMatchingChangeIndex(List<TextChange> changes, out int index)
        {
            index = -1;
            var insertionPoint = GetInsertionSpan(changes);

            for (int i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                if (change.Span.Contains(insertionPoint) && IsNullOrWhitespace(change.NewText))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private TextSpan GetInsertionSpan(List<TextChange> changes)
        {
            // first line is blank or there is no changes. 
            // just insert at the head
            if (_firstLineBlank ||
                changes.Count == 0)
            {
                return new TextSpan(this.StartPosition, 0);
            }

            // try to find end of line
            for (int i = this.OriginalString.Length - 1; i >= 0; i--)
            {
                if (this.OriginalString[i] == '\n')
                {
                    return new TextSpan(Math.Min(this.StartPosition + i + 1, this.EndPosition), 0);
                }
            }

            // well, give up and insert at the top
            Debug.Assert(!_firstLineBlank);
            return new TextSpan(this.EndPosition, 0);
        }

        private void AddWhitespaceTrivia(
            LineColumn lineColumn,
            LineColumnDelta delta,
            List<SyntaxTrivia> changes)
        {
            AddWhitespaceTrivia(lineColumn, delta, default, changes);
        }

        private void AddWhitespaceTrivia(
            LineColumn lineColumn,
            LineColumnDelta delta,
            TextSpan notUsed,
            List<SyntaxTrivia> changes)
        {
            if (delta.Lines == 0 && delta.Spaces == 0)
            {
                // remove trivia
                return;
            }

            for (int i = 0; i < delta.Lines; i++)
            {
                changes.Add(CreateEndOfLine());
            }

            if (delta.Spaces == 0)
            {
                return;
            }

            var useTabs = this.OptionSet.GetOption(FormattingOptions.UseTabs, this.Language);
            var tabSize = this.OptionSet.GetOption(FormattingOptions.TabSize, this.Language);

            // space indicates indentation
            if (delta.Lines > 0 || lineColumn.Column == 0)
            {
                changes.Add(CreateWhitespace(delta.Spaces.CreateIndentationString(useTabs, tabSize)));
                return;
            }

            // space indicates space between two noisy trivia or tokens
            changes.Add(CreateWhitespace(GetSpaces(delta.Spaces)));
        }

        private string GetWhitespaceString(LineColumn lineColumn, LineColumnDelta delta)
        {
            var sb = StringBuilderPool.Allocate();

            var newLine = this.OptionSet.GetOption(FormattingOptions.NewLine, this.Language);
            for (int i = 0; i < delta.Lines; i++)
            {
                sb.Append(newLine);
            }

            if (delta.Spaces == 0)
            {
                return StringBuilderPool.ReturnAndFree(sb);
            }

            var useTabs = this.OptionSet.GetOption(FormattingOptions.UseTabs, this.Language);
            var tabSize = this.OptionSet.GetOption(FormattingOptions.TabSize, this.Language);

            // space indicates indentation
            if (delta.Lines > 0 || lineColumn.Column == 0)
            {
                sb.AppendIndentationString(delta.Spaces, useTabs, tabSize);
                return StringBuilderPool.ReturnAndFree(sb);
            }

            // space indicates space between two noisy trivia or tokens
            sb.Append(' ', repeatCount: delta.Spaces);
            return StringBuilderPool.ReturnAndFree(sb);
        }

        private TextChange GetWhitespaceTextChange(LineColumn lineColumn, LineColumnDelta delta, TextSpan span)
        {
            return new TextChange(span, GetWhitespaceString(lineColumn, delta));
        }

        private void AddWhitespaceTextChange(LineColumn lineColumn, LineColumnDelta delta, TextSpan span, List<TextChange> changes)
        {
            var newText = GetWhitespaceString(lineColumn, delta);
            changes.Add(new TextChange(span, newText));
        }

        private TextSpan GetTextSpan(SyntaxTrivia trivia1, SyntaxTrivia trivia2)
        {
            if (trivia1.RawKind == 0)
            {
                return TextSpan.FromBounds(this.StartPosition, trivia2.FullSpan.Start);
            }

            if (trivia2.RawKind == 0)
            {
                return TextSpan.FromBounds(trivia1.FullSpan.End, this.EndPosition);
            }

            return TextSpan.FromBounds(trivia1.FullSpan.End, trivia2.FullSpan.Start);
        }

        private bool IsWhitespaceOrEndOfLine(SyntaxTrivia trivia)
        {
            return IsWhitespace(trivia) || IsEndOfLine(trivia);
        }

        private LineColumnDelta GetLineColumnOfWhitespace(
            LineColumn lineColumn,
            SyntaxTrivia previousTrivia,
            SyntaxTrivia trivia1,
            LineColumnDelta whitespaceBetween,
            SyntaxTrivia trivia2)
        {
            Debug.Assert(IsWhitespaceOrEndOfLine(trivia2));

            // treat elastic as new line as long as its previous trivia is not elastic or
            // it has line break right before it
            if (trivia2.IsElastic())
            {
                // eat up consecutive elastic trivia or next line
                if (trivia1.IsElastic() || IsEndOfLine(trivia1))
                {
                    return LineColumnDelta.Default;
                }

                // if there was already new lines, ignore elastic
                var lineColumnAfterPreviousTrivia = GetLineColumn(lineColumn, previousTrivia);

                var newLineFromPreviousOperation = (whitespaceBetween.Lines > 0) ||
                                                   (lineColumnAfterPreviousTrivia.Line > 0 && lineColumnAfterPreviousTrivia.Column == 0);
                if (newLineFromPreviousOperation && whitespaceBetween.WhitespaceOnly)
                {
                    return LineColumnDelta.Default;
                }

                return new LineColumnDelta(lines: 1, spaces: 0, whitespaceOnly: true, forceUpdate: true);
            }

            if (IsEndOfLine(trivia2))
            {
                return new LineColumnDelta(lines: 1, spaces: 0, whitespaceOnly: true, forceUpdate: false);
            }

            var text = trivia2.ToFullString();
            return new LineColumnDelta(
                lines: 0,
                spaces: text.ConvertTabToSpace(this.OptionSet.GetOption(FormattingOptions.TabSize, this.Language), lineColumn.With(whitespaceBetween).Column, text.Length),
                whitespaceOnly: true,
                forceUpdate: false);
        }

        private LineColumn GetInitialLineColumn()
        {
            var tokenText = this.Token1.ToString();
            var initialColumn = this.Token1.RawKind == 0 ? 0 : this.TokenStream.GetCurrentColumn(this.Token1);
            var delta = GetLineColumnDelta(initialColumn, tokenText);

            return new LineColumn(line: 0, column: initialColumn + delta.Spaces, whitespaceOnly: delta.WhitespaceOnly);
        }

        protected LineColumn GetLineColumn(LineColumn lineColumn, SyntaxTrivia trivia)
        {
            var text = trivia.ToFullString();

            return lineColumn.With(GetLineColumnDelta(lineColumn.Column, text));
        }

        protected LineColumnDelta GetLineColumnDelta(LineColumn lineColumn, SyntaxTrivia trivia)
        {
            var text = trivia.ToFullString();

            return GetLineColumnDelta(lineColumn.Column, text);
        }

        protected LineColumnDelta GetLineColumnDelta(int initialColumn, string text)
        {
            var lineText = text.GetLastLineText();

            if (text != lineText)
            {
                return new LineColumnDelta(
                    lines: text.GetNumberOfLineBreaks(),
                    spaces: lineText.GetColumnFromLineOffset(lineText.Length, this.OptionSet.GetOption(FormattingOptions.TabSize, this.Language)),
                    whitespaceOnly: IsNullOrWhitespace(lineText));
            }

            return new LineColumnDelta(
                lines: 0,
                spaces: text.ConvertTabToSpace(this.OptionSet.GetOption(FormattingOptions.TabSize, this.Language), initialColumn, text.Length),
                whitespaceOnly: IsNullOrWhitespace(lineText));
        }

        protected int GetExistingIndentation(SyntaxTrivia trivia)
        {
            var offset = trivia.FullSpan.Start - this.StartPosition;
            var originalText = this.OriginalString.Substring(0, offset);
            var delta = GetLineColumnDelta(this.InitialLineColumn.Column, originalText);

            return this.InitialLineColumn.With(delta).Column;
        }

        private static string GetSpaces(int space)
        {
            if (space >= 0 && space < 20)
            {
                return s_spaceCache[space];
            }

            return new string(' ', space);
        }
    }
}
