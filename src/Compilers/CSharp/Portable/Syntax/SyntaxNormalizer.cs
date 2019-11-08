// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxNormalizer : CSharpSyntaxRewriter
    {
        private readonly TextSpan _consideredSpan;
        private readonly int _initialDepth;
        private readonly string _indentWhitespace;
        private readonly bool _useElasticTrivia;
        private readonly SyntaxTrivia _eolTrivia;

        private bool _isInStructuredTrivia;

        private SyntaxToken _previousToken;

        private bool _afterLineBreak;
        private bool _afterIndentation;

        // CONSIDER: if we become concerned about space, we shouldn't actually need any 
        // of the values between indentations[0] and indentations[initialDepth] (exclusive).
        private ArrayBuilder<SyntaxTrivia> _indentations;

        private SyntaxNormalizer(TextSpan consideredSpan, int initialDepth, string indentWhitespace, string eolWhitespace, bool useElasticTrivia)
            : base(visitIntoStructuredTrivia: true)
        {
            _consideredSpan = consideredSpan;
            _initialDepth = initialDepth;
            _indentWhitespace = indentWhitespace;
            _useElasticTrivia = useElasticTrivia;
            _eolTrivia = useElasticTrivia ? SyntaxFactory.ElasticEndOfLine(eolWhitespace) : SyntaxFactory.EndOfLine(eolWhitespace);
            _afterLineBreak = true;
        }

        internal static TNode Normalize<TNode>(TNode node, string indentWhitespace, string eolWhitespace, bool useElasticTrivia = false)
            where TNode : SyntaxNode
        {
            var normalizer = new SyntaxNormalizer(node.FullSpan, GetDeclarationDepth(node), indentWhitespace, eolWhitespace, useElasticTrivia);
            var result = (TNode)normalizer.Visit(node);
            normalizer.Free();
            return result;
        }

        internal static SyntaxToken Normalize(SyntaxToken token, string indentWhitespace, string eolWhitespace, bool useElasticTrivia = false)
        {
            var normalizer = new SyntaxNormalizer(token.FullSpan, GetDeclarationDepth(token), indentWhitespace, eolWhitespace, useElasticTrivia);
            var result = normalizer.VisitToken(token);
            normalizer.Free();
            return result;
        }

        internal static SyntaxTriviaList Normalize(SyntaxTriviaList trivia, string indentWhitespace, string eolWhitespace, bool useElasticTrivia = false)
        {
            var normalizer = new SyntaxNormalizer(trivia.FullSpan, GetDeclarationDepth(trivia.Token), indentWhitespace, eolWhitespace, useElasticTrivia);
            var result = normalizer.RewriteTrivia(
                trivia,
                GetDeclarationDepth((SyntaxToken)trivia.ElementAt(0).Token),
                isTrailing: false,
                indentAfterLineBreak: false,
                mustHaveSeparator: false,
                lineBreaksAfter: 0);
            normalizer.Free();
            return result;
        }

        private void Free()
        {
            if (_indentations != null)
            {
                _indentations.Free();
            }
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.None || (token is { IsMissing: true, FullWidth: 0 }))
            {
                return token;
            }

            try
            {
                var tk = token;

                var depth = GetDeclarationDepth(token);

                tk = tk.WithLeadingTrivia(RewriteTrivia(
                    token.LeadingTrivia,
                    depth,
                    isTrailing: false,
                    indentAfterLineBreak: NeedsIndentAfterLineBreak(token),
                    mustHaveSeparator: false,
                    lineBreaksAfter: 0));

                var nextToken = this.GetNextRelevantToken(token);

                _afterLineBreak = IsLineBreak(token);
                _afterIndentation = false;

                var lineBreaksAfter = LineBreaksAfter(token, nextToken);
                var needsSeparatorAfter = NeedsSeparator(token, nextToken);
                tk = tk.WithTrailingTrivia(RewriteTrivia(
                    token.TrailingTrivia,
                    depth,
                    isTrailing: true,
                    indentAfterLineBreak: false,
                    mustHaveSeparator: needsSeparatorAfter,
                    lineBreaksAfter: lineBreaksAfter));

                return tk;
            }
            finally
            {
                // to help debugging
                _previousToken = token;
            }
        }

        private SyntaxToken GetNextRelevantToken(SyntaxToken token)
        {
            // get next token, skipping zero width tokens except for end-of-directive tokens
            var nextToken = token.GetNextToken(
                t => SyntaxToken.NonZeroWidth(t) || t.Kind() == SyntaxKind.EndOfDirectiveToken,
                t => t.Kind() == SyntaxKind.SkippedTokensTrivia);

            if (_consideredSpan.Contains(nextToken.FullSpan))
            {
                return nextToken;
            }
            else
            {
                return default(SyntaxToken);
            }
        }

        private SyntaxTrivia GetIndentation(int count)
        {
            count = Math.Max(count - _initialDepth, 0);

            int capacity = count + 1;
            if (_indentations == null)
            {
                _indentations = ArrayBuilder<SyntaxTrivia>.GetInstance(capacity);
            }
            else
            {
                _indentations.EnsureCapacity(capacity);
            }

            // grow indentation collection if necessary
            for (int i = _indentations.Count; i <= count; i++)
            {
                string text = i == 0
                    ? ""
                    : _indentations[i - 1].ToString() + _indentWhitespace;
                _indentations.Add(_useElasticTrivia ? SyntaxFactory.ElasticWhitespace(text) : SyntaxFactory.Whitespace(text));
            }

            return _indentations[count];
        }

        private static bool NeedsIndentAfterLineBreak(SyntaxToken token)
        {
            return !token.IsKind(SyntaxKind.EndOfFileToken);
        }

        private int LineBreaksAfter(SyntaxToken currentToken, SyntaxToken nextToken)
        {
            if (currentToken.IsKind(SyntaxKind.EndOfDirectiveToken))
            {
                return 1;
            }

            if (nextToken.Kind() == SyntaxKind.None)
            {
                return 0;
            }

            // none of the following tests currently have meaning for structured trivia
            if (_isInStructuredTrivia)
            {
                return 0;
            }

            switch (currentToken.Kind())
            {
                case SyntaxKind.None:
                    return 0;

                case SyntaxKind.OpenBraceToken:
                    return LineBreaksAfterOpenBrace(currentToken, nextToken);

                case SyntaxKind.FinallyKeyword:
                    return 1;

                case SyntaxKind.CloseBraceToken:
                    return LineBreaksAfterCloseBrace(currentToken, nextToken);

                case SyntaxKind.CloseParenToken:
                    // Note: the `where` case handles constraints on method declarations
                    //  and also `where` clauses (consistently with other LINQ cases below)
                    return (((currentToken.Parent is StatementSyntax) && nextToken.Parent != currentToken.Parent)
                        || nextToken.Kind() == SyntaxKind.OpenBraceToken
                        || nextToken.Kind() == SyntaxKind.WhereKeyword) ? 1 : 0;

                case SyntaxKind.CloseBracketToken:
                    if (currentToken.Parent is AttributeListSyntax && !(currentToken.Parent.Parent is ParameterSyntax))
                    {
                        return 1;
                    }
                    break;

                case SyntaxKind.SemicolonToken:
                    return LineBreaksAfterSemicolon(currentToken, nextToken);

                case SyntaxKind.CommaToken:
                    return currentToken.Parent is EnumDeclarationSyntax ? 1 : 0;
                case SyntaxKind.ElseKeyword:
                    return nextToken.Kind() != SyntaxKind.IfKeyword ? 1 : 0;
                case SyntaxKind.ColonToken:
                    if (currentToken.Parent is LabeledStatementSyntax || currentToken.Parent is SwitchLabelSyntax)
                    {
                        return 1;
                    }
                    break;
            }

            if ((nextToken.IsKind(SyntaxKind.FromKeyword) && nextToken.Parent.IsKind(SyntaxKind.FromClause)) ||
                (nextToken.IsKind(SyntaxKind.LetKeyword) && nextToken.Parent.IsKind(SyntaxKind.LetClause)) ||
                (nextToken.IsKind(SyntaxKind.WhereKeyword) && nextToken.Parent.IsKind(SyntaxKind.WhereClause)) ||
                (nextToken.IsKind(SyntaxKind.JoinKeyword) && nextToken.Parent.IsKind(SyntaxKind.JoinClause)) ||
                (nextToken.IsKind(SyntaxKind.JoinKeyword) && nextToken.Parent.Kind() == SyntaxKind.JoinIntoClause) ||
                (nextToken.Kind() == SyntaxKind.OrderByKeyword && nextToken.Parent.Kind() == SyntaxKind.OrderByClause) ||
                (nextToken.Kind() == SyntaxKind.SelectKeyword && nextToken.Parent.Kind() == SyntaxKind.SelectClause) ||
                (nextToken.Kind() == SyntaxKind.GroupKeyword && nextToken.Parent.Kind() == SyntaxKind.GroupClause))
            {
                return 1;
            }

            switch (nextToken.Kind())
            {
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.CloseBraceToken:
                    return (nextToken.Parent.IsKind(SyntaxKind.Interpolation) || nextToken.Parent is InitializerExpressionSyntax) ? 0 : 1;
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.FinallyKeyword:
                    return 1;
                case SyntaxKind.OpenBracketToken:
                    return (nextToken.Parent is AttributeListSyntax && !(nextToken.Parent.Parent is ParameterSyntax)) ? 1 : 0;
                case SyntaxKind.WhereKeyword:
                    return currentToken.Parent is TypeParameterListSyntax ? 1 : 0;
            }

            return 0;
        }

        private static int LineBreaksAfterOpenBrace(SyntaxToken currentToken, SyntaxToken nextToken)
        {
            if (currentToken.Parent is InitializerExpressionSyntax ||
                currentToken.Parent.IsKind(SyntaxKind.Interpolation))
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        private static int LineBreaksAfterCloseBrace(SyntaxToken currentToken, SyntaxToken nextToken)
        {
            if (currentToken.Parent is InitializerExpressionSyntax ||
                currentToken.Parent.IsKind(SyntaxKind.Interpolation))
            {
                return 0;
            }

            var kind = nextToken.Kind();
            switch (kind)
            {
                case SyntaxKind.EndOfFileToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.CatchKeyword:
                case SyntaxKind.FinallyKeyword:
                case SyntaxKind.ElseKeyword:
                    return 1;
                default:
                    if (kind == SyntaxKind.WhileKeyword &&
                        nextToken.Parent.IsKind(SyntaxKind.DoStatement))
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }
            }
        }

        private static int LineBreaksAfterSemicolon(SyntaxToken currentToken, SyntaxToken nextToken)
        {
            if (currentToken.Parent.Kind() == SyntaxKind.ForStatement)
            {
                return 0;
            }
            else if (nextToken.Kind() == SyntaxKind.CloseBraceToken)
            {
                return 1;
            }
            else if (currentToken.Parent.Kind() == SyntaxKind.UsingDirective)
            {
                return nextToken.Parent.Kind() == SyntaxKind.UsingDirective ? 1 : 2;
            }
            else if (currentToken.Parent.Kind() == SyntaxKind.ExternAliasDirective)
            {
                return nextToken.Parent.Kind() == SyntaxKind.ExternAliasDirective ? 1 : 2;
            }
            else
            {
                return 1;
            }
        }

        private static bool NeedsSeparator(SyntaxToken token, SyntaxToken next)
        {
            if (token.Parent == null || next.Parent == null)
            {
                return false;
            }

            if (IsXmlTextToken(token.Kind()) || IsXmlTextToken(next.Kind()))
            {
                return false;
            }

            if (next.Kind() == SyntaxKind.EndOfDirectiveToken)
            {
                // In a directive, there's often no token between the directive keyword and 
                // the end-of-directive, so we may need a separator.
                return IsKeyword(token.Kind()) && next.LeadingWidth > 0;
            }

            if ((token.Parent is AssignmentExpressionSyntax && AssignmentTokenNeedsSeparator(token.Kind())) ||
                (next.Parent is AssignmentExpressionSyntax && AssignmentTokenNeedsSeparator(next.Kind())) ||
                (token.Parent is BinaryExpressionSyntax && BinaryTokenNeedsSeparator(token.Kind())) ||
                (next.Parent is BinaryExpressionSyntax && BinaryTokenNeedsSeparator(next.Kind())))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.GreaterThanToken) && token.Parent.IsKind(SyntaxKind.TypeArgumentList))
            {
                if (!SyntaxFacts.IsPunctuation(next.Kind()))
                {
                    return true;
                }
            }

            if (token.IsKind(SyntaxKind.CommaToken) &&
                !next.IsKind(SyntaxKind.CommaToken) &&
                !token.Parent.IsKind(SyntaxKind.EnumDeclaration))
            {
                return true;
            }

            if (token.Kind() == SyntaxKind.SemicolonToken
                && !(next.Kind() == SyntaxKind.SemicolonToken || next.Kind() == SyntaxKind.CloseParenToken))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.QuestionToken)
                && (token.Parent.IsKind(SyntaxKind.ConditionalExpression) || token.Parent is TypeSyntax)
                && !token.Parent.Parent.IsKind(SyntaxKind.TypeArgumentList))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.ColonToken))
            {
                return !token.Parent.IsKind(SyntaxKind.InterpolationFormatClause);
            }

            if (next.IsKind(SyntaxKind.ColonToken))
            {
                if (next.Parent.IsKind(SyntaxKind.BaseList) ||
                    next.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
                {
                    return true;
                }
            }

            if (token.IsKind(SyntaxKind.CloseBracketToken) && IsWord(next.Kind()))
            {
                return true;
            }

            // We don't want to add extra space after cast, we want space only after tuple
            if (token.IsKind(SyntaxKind.CloseParenToken) && IsWord(next.Kind()) && token.Parent.IsKind(SyntaxKind.TupleType) == true)
            {
                return true;
            }

            if ((next.IsKind(SyntaxKind.QuestionToken) || next.IsKind(SyntaxKind.ColonToken))
                && (next.Parent.IsKind(SyntaxKind.ConditionalExpression)))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.EqualsToken) || next.IsKind(SyntaxKind.EqualsToken))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.EqualsGreaterThanToken) || next.IsKind(SyntaxKind.EqualsGreaterThanToken))
            {
                return true;
            }

            // Can happen in directives (e.g. #line 1 "file")
            if (SyntaxFacts.IsLiteral(token.Kind()) && SyntaxFacts.IsLiteral(next.Kind()))
            {
                return true;
            }

            if (IsKeyword(token.Kind()))
            {
                if (!next.IsKind(SyntaxKind.ColonToken) &&
                    !next.IsKind(SyntaxKind.DotToken) &&
                    !next.IsKind(SyntaxKind.QuestionToken) &&
                    !next.IsKind(SyntaxKind.SemicolonToken) &&
                    !next.IsKind(SyntaxKind.OpenBracketToken) &&
                    (!next.IsKind(SyntaxKind.OpenParenToken) || KeywordNeedsSeparatorBeforeOpenParen(token.Kind()) || next.Parent.IsKind(SyntaxKind.TupleType)) &&
                    !next.IsKind(SyntaxKind.CloseParenToken) &&
                    !next.IsKind(SyntaxKind.CloseBraceToken) &&
                    !next.IsKind(SyntaxKind.ColonColonToken) &&
                    !next.IsKind(SyntaxKind.GreaterThanToken) &&
                    !next.IsKind(SyntaxKind.CommaToken))
                {
                    return true;
                }
            }

            if (IsWord(token.Kind()) && IsWord(next.Kind()))
            {
                return true;
            }
            else if (token.Width > 1 && next.Width > 1)
            {
                var tokenLastChar = token.Text.Last();
                var nextFirstChar = next.Text.First();
                if (tokenLastChar == nextFirstChar && TokenCharacterCanBeDoubled(tokenLastChar))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool KeywordNeedsSeparatorBeforeOpenParen(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.TypeOfKeyword:
                case SyntaxKind.DefaultKeyword:
                case SyntaxKind.NewKeyword:
                case SyntaxKind.BaseKeyword:
                case SyntaxKind.ThisKeyword:
                case SyntaxKind.CheckedKeyword:
                case SyntaxKind.UncheckedKeyword:
                case SyntaxKind.SizeOfKeyword:
                case SyntaxKind.ArgListKeyword:
                    return false;
                default:
                    return true;
            }
        }

        private static bool IsXmlTextToken(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.XmlTextLiteralNewLineToken:
                case SyntaxKind.XmlTextLiteralToken:
                    return true;
                default:
                    return false;
            }
        }

        private static bool BinaryTokenNeedsSeparator(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.DotToken:
                case SyntaxKind.MinusGreaterThanToken:
                    return false;
                default:
                    return SyntaxFacts.GetBinaryExpression(kind) != SyntaxKind.None;
            }
        }

        private static bool AssignmentTokenNeedsSeparator(SyntaxKind kind)
        {
            return SyntaxFacts.GetAssignmentExpression(kind) != SyntaxKind.None;
        }

        private SyntaxTriviaList RewriteTrivia(
            SyntaxTriviaList triviaList,
            int depth,
            bool isTrailing,
            bool indentAfterLineBreak,
            bool mustHaveSeparator,
            int lineBreaksAfter)
        {
            ArrayBuilder<SyntaxTrivia> currentTriviaList = ArrayBuilder<SyntaxTrivia>.GetInstance(triviaList.Count);
            try
            {
                foreach (var trivia in triviaList)
                {
                    if (trivia.IsKind(SyntaxKind.WhitespaceTrivia) ||
                        trivia.IsKind(SyntaxKind.EndOfLineTrivia) ||
                        trivia.FullWidth == 0)
                    {
                        continue;
                    }

                    var needsSeparator =
                        (currentTriviaList.Count > 0 && NeedsSeparatorBetween(currentTriviaList.Last())) ||
                            (currentTriviaList.Count == 0 && isTrailing);

                    var needsLineBreak = NeedsLineBreakBefore(trivia, isTrailing)
                        || (currentTriviaList.Count > 0 && NeedsLineBreakBetween(currentTriviaList.Last(), trivia, isTrailing));

                    if (needsLineBreak && !_afterLineBreak)
                    {
                        currentTriviaList.Add(GetEndOfLine());
                        _afterLineBreak = true;
                        _afterIndentation = false;
                    }

                    if (_afterLineBreak)
                    {
                        if (!_afterIndentation && NeedsIndentAfterLineBreak(trivia))
                        {
                            currentTriviaList.Add(this.GetIndentation(GetDeclarationDepth(trivia)));
                            _afterIndentation = true;
                        }
                    }
                    else if (needsSeparator)
                    {
                        currentTriviaList.Add(GetSpace());
                        _afterLineBreak = false;
                        _afterIndentation = false;
                    }

                    if (trivia.HasStructure)
                    {
                        var tr = this.VisitStructuredTrivia(trivia);
                        currentTriviaList.Add(tr);
                    }
                    else if (trivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia))
                    {
                        // recreate exterior to remove any leading whitespace
                        currentTriviaList.Add(s_trimmedDocCommentExterior);
                    }
                    else
                    {
                        currentTriviaList.Add(trivia);
                    }

                    if (NeedsLineBreakAfter(trivia, isTrailing)
                        && (currentTriviaList.Count == 0 || !EndsInLineBreak(currentTriviaList.Last())))
                    {
                        currentTriviaList.Add(GetEndOfLine());
                        _afterLineBreak = true;
                        _afterIndentation = false;
                    }
                }

                if (lineBreaksAfter > 0)
                {
                    if (currentTriviaList.Count > 0
                        && EndsInLineBreak(currentTriviaList.Last()))
                    {
                        lineBreaksAfter--;
                    }

                    for (int i = 0; i < lineBreaksAfter; i++)
                    {
                        currentTriviaList.Add(GetEndOfLine());
                        _afterLineBreak = true;
                        _afterIndentation = false;
                    }
                }
                else if (indentAfterLineBreak && _afterLineBreak && !_afterIndentation)
                {
                    currentTriviaList.Add(this.GetIndentation(depth));
                    _afterIndentation = true;
                }
                else if (mustHaveSeparator)
                {
                    currentTriviaList.Add(GetSpace());
                    _afterLineBreak = false;
                    _afterIndentation = false;
                }

                if (currentTriviaList.Count == 0)
                {
                    return default(SyntaxTriviaList);
                }
                else if (currentTriviaList.Count == 1)
                {
                    return SyntaxFactory.TriviaList(currentTriviaList.First());
                }
                else
                {
                    return SyntaxFactory.TriviaList(currentTriviaList);
                }
            }
            finally
            {
                currentTriviaList.Free();
            }
        }

        private static SyntaxTrivia s_trimmedDocCommentExterior = SyntaxFactory.DocumentationCommentExterior("///");

        private SyntaxTrivia GetSpace()
        {
            return _useElasticTrivia ? SyntaxFactory.ElasticSpace : SyntaxFactory.Space;
        }

        private SyntaxTrivia GetEndOfLine()
        {
            return _eolTrivia;
        }

        private SyntaxTrivia VisitStructuredTrivia(SyntaxTrivia trivia)
        {
            bool oldIsInStructuredTrivia = _isInStructuredTrivia;
            _isInStructuredTrivia = true;

            SyntaxToken oldPreviousToken = _previousToken;
            _previousToken = default(SyntaxToken);

            SyntaxTrivia result = VisitTrivia(trivia);

            _isInStructuredTrivia = oldIsInStructuredTrivia;
            _previousToken = oldPreviousToken;

            return result;
        }

        private static bool NeedsSeparatorBetween(SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.None:
                case SyntaxKind.WhitespaceTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                    return false;
                default:
                    return !SyntaxFacts.IsPreprocessorDirective(trivia.Kind());
            }
        }

        private static bool NeedsLineBreakBetween(SyntaxTrivia trivia, SyntaxTrivia next, bool isTrailingTrivia)
        {
            return NeedsLineBreakAfter(trivia, isTrailingTrivia)
                || NeedsLineBreakBefore(next, isTrailingTrivia);
        }

        private static bool NeedsLineBreakBefore(SyntaxTrivia trivia, bool isTrailingTrivia)
        {
            var kind = trivia.Kind();
            switch (kind)
            {
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                    return !isTrailingTrivia;
                default:
                    return SyntaxFacts.IsPreprocessorDirective(kind);
            }
        }

        private static bool NeedsLineBreakAfter(SyntaxTrivia trivia, bool isTrailingTrivia)
        {
            var kind = trivia.Kind();
            switch (kind)
            {
                case SyntaxKind.SingleLineCommentTrivia:
                    return true;
                case SyntaxKind.MultiLineCommentTrivia:
                    return !isTrailingTrivia;
                default:
                    return SyntaxFacts.IsPreprocessorDirective(kind);
            }
        }

        private static bool NeedsIndentAfterLineBreak(SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsLineBreak(SyntaxToken token)
        {
            return token.Kind() == SyntaxKind.XmlTextLiteralNewLineToken;
        }

        private static bool EndsInLineBreak(SyntaxTrivia trivia)
        {
            if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
            {
                return true;
            }

            if (trivia.Kind() == SyntaxKind.PreprocessingMessageTrivia || trivia.Kind() == SyntaxKind.DisabledTextTrivia)
            {
                var text = trivia.ToFullString();
                return text.Length > 0 && SyntaxFacts.IsNewLine(text.Last());
            }

            if (trivia.HasStructure)
            {
                var node = trivia.GetStructure();
                var trailing = node.GetTrailingTrivia();
                if (trailing.Count > 0)
                {
                    return EndsInLineBreak(trailing.Last());
                }
                else
                {
                    return IsLineBreak(node.GetLastToken());
                }
            }

            return false;
        }

        private static bool IsWord(SyntaxKind kind)
        {
            return kind == SyntaxKind.IdentifierToken || IsKeyword(kind);
        }

        private static bool IsKeyword(SyntaxKind kind)
        {
            return SyntaxFacts.IsKeywordKind(kind) || SyntaxFacts.IsPreprocessorKeyword(kind);
        }

        private static bool TokenCharacterCanBeDoubled(char c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '<':
                case ':':
                case '?':
                case '=':
                case '"':
                    return true;
                default:
                    return false;
            }
        }

        private static int GetDeclarationDepth(SyntaxToken token)
        {
            return GetDeclarationDepth(token.Parent);
        }

        private static int GetDeclarationDepth(SyntaxTrivia trivia)
        {
            if (SyntaxFacts.IsPreprocessorDirective(trivia.Kind()))
            {
                return 0;
            }

            return GetDeclarationDepth((SyntaxToken)trivia.Token);
        }

        private static int GetDeclarationDepth(SyntaxNode node)
        {
            if (node != null)
            {
                if (node.IsStructuredTrivia)
                {
                    var tr = ((StructuredTriviaSyntax)node).ParentTrivia;
                    return GetDeclarationDepth(tr);
                }
                else if (node.Parent != null)
                {
                    if (node.Parent.IsKind(SyntaxKind.CompilationUnit))
                    {
                        return 0;
                    }

                    int parentDepth = GetDeclarationDepth(node.Parent);

                    if (node.Parent.IsKind(SyntaxKind.GlobalStatement))
                    {
                        return parentDepth;
                    }

                    if (node.IsKind(SyntaxKind.IfStatement) && node.Parent.IsKind(SyntaxKind.ElseClause))
                    {
                        return parentDepth;
                    }

                    if (node.Parent is BlockSyntax ||
                        (node is StatementSyntax && !(node is BlockSyntax)))
                    {
                        // all nested statements are indented one level
                        return parentDepth + 1;
                    }

                    if (node is MemberDeclarationSyntax ||
                        node is AccessorDeclarationSyntax ||
                        node is TypeParameterConstraintClauseSyntax ||
                        node is SwitchSectionSyntax ||
                        node is UsingDirectiveSyntax ||
                        node is ExternAliasDirectiveSyntax ||
                        node is QueryExpressionSyntax ||
                        node is QueryContinuationSyntax)
                    {
                        return parentDepth + 1;
                    }

                    return parentDepth;
                }
            }

            return 0;
        }
    }
}
