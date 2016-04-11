// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class LanguageParser : SyntaxParser
    {
        // Priority is the TypeSyntax. It might return TypeSyntax which might be a constant pattern such as enum 'Days.Sunday' 
        // We handle such cases in the binder of is operator.
        // It is used for parsing patterns in the is operators.
        private CSharpSyntaxNode ParseTypeOrPattern()
        {
            var tk = this.CurrentToken.Kind;
            CSharpSyntaxNode node = null;

            switch (tk)
            {
                case SyntaxKind.AsteriskToken:
                    var asteriskToken = this.EatToken();
                    return _syntaxFactory.WildcardPattern(asteriskToken);
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.CommaToken:
                    // HACK: for error recovery, we prefer a (missing) type.
                    return this.ParseTypeCore(parentIsParameter: false, isOrAs: true, expectSizes: false, isArrayCreation: false);
                default:
                    // attempt to disambiguate.
                    break;
            }

            // If it is a nameof, skip the 'if' and parse as a constant pattern.
            if (SyntaxFacts.IsPredefinedType(tk) ||
                (tk == SyntaxKind.IdentifierToken && this.CurrentToken.ContextualKind != SyntaxKind.NameOfKeyword))
            {
                var resetPoint = this.GetResetPoint();
                try
                {
                    TypeSyntax type = this.ParseTypeCore(parentIsParameter: false, isOrAs: true, expectSizes: false, isArrayCreation: false);

                    tk = this.CurrentToken.ContextualKind;
                    if (!type.IsMissing)
                    {
                        // X.Y.Z ( ... ) : PositionalPattern
                        if (tk == SyntaxKind.OpenParenToken)
                        {
                            node = _syntaxFactory.PositionalPattern(type, this.ParseSubPositionalPatternList());
                            node = CheckFeatureAvailability(node, MessageID.IDS_FeaturePatternMatching2);
                        }
                        // X.Y.Z { ... } : PropertyPattern
                        else if (tk == SyntaxKind.OpenBraceToken)
                        {
                            node = ParsePropertyPatternBody(type);
                        }
                        // X.Y.Z id
                        else if (this.IsTrueIdentifier())
                        {
                            var identifier = ParseIdentifierToken();
                            node = _syntaxFactory.DeclarationPattern(type, identifier);
                        }
                    }
                    if (node == null)
                    {
                        Debug.Assert(Precedence.Shift == Precedence.Relational + 1);
                        if (IsExpectedBinaryOperator(tk) && GetPrecedence(SyntaxFacts.GetBinaryExpression(tk)) > Precedence.Relational ||
                            tk == SyntaxKind.DotToken)
                        {
                            this.Reset(ref resetPoint);
                            // We parse a shift-expression ONLY (nothing looser) - i.e. not a relational expression
                            // So x is y < z should be parsed as (x is y) < z
                            // But x is y << z should be parsed as x is (y << z)
                            node = _syntaxFactory.ConstantPattern(this.ParseSubExpression(Precedence.Shift));
                        }
                        // it is a typical is operator! 
                        else
                        {
                            // Note that we don't bother checking for primary expressions such as X[e], X(e), X++, and X--
                            // as those are never semantically valid constant expressions for a pattern, and X(e) is
                            // syntactically a recursive pattern that we checked for earlier.
                            node = type;
                        }
                    }
                }
                finally
                {
                    this.Release(ref resetPoint);
                }
            }
            else
            {
                // it still might be a pattern such as (operand is 3) or (operand is nameof(x))
                node = _syntaxFactory.ConstantPattern(this.ParseExpressionCore());
            }
            return node;
        }

        private PropertyPatternSyntax ParsePropertyPatternBody(TypeSyntax type)
        {
            var open = this.EatToken(SyntaxKind.OpenBraceToken);
            var list = this.ParseSubPropertyPatternList(ref open);
            var close = this.EatToken(SyntaxKind.CloseBraceToken);
            return _syntaxFactory.PropertyPattern(type, open, list, close);
        }

        private SubPositionalPatternListSyntax ParseSubPositionalPatternList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.SubPositionalPatternList)
            {
                return (SubPositionalPatternListSyntax)this.EatNode();
            }

            SyntaxToken openToken, closeToken;
            SeparatedSyntaxList<SubPositionalPatternSyntax> subPatterns;
            ParseSubPositionalPatternList(out openToken, out subPatterns, out closeToken, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken);

            return _syntaxFactory.SubPositionalPatternList(openToken, subPatterns, closeToken);
        }

        private void ParseSubPositionalPatternList(
            out SyntaxToken openToken,
            out SeparatedSyntaxList<SubPositionalPatternSyntax> subPatterns,
            out SyntaxToken closeToken,
            SyntaxKind openKind,
            SyntaxKind closeKind)
        {
            var open = this.EatToken(openKind);
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfArgumentList;

            SeparatedSyntaxListBuilder<SubPositionalPatternSyntax> list = default(SeparatedSyntaxListBuilder<SubPositionalPatternSyntax>);
            try
            {
                if (this.CurrentToken.Kind != closeKind && this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
                {
                    tryAgain:
                    if (list.IsNull)
                    {
                        list = _pool.AllocateSeparated<SubPositionalPatternSyntax>();
                    }

                    if (this.IsPossibleArgumentExpression() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        // first argument
                        list.Add(this.ParseSubPositionalPattern());

                        // additional arguments
                        while (true)
                        {
                            if (this.CurrentToken.Kind == closeKind || this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                            {
                                break;
                            }
                            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleArgumentExpression())
                            {
                                list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                                list.Add(this.ParseSubPositionalPattern());
                                continue;
                            }
                            else if (this.SkipBadSubPatternListTokens(ref open, list, SyntaxKind.CommaToken, closeKind) == PostSkipAction.Abort)
                            {
                                break;
                            }
                        }
                    }
                    else if (this.SkipBadSubPatternListTokens(ref open, list, SyntaxKind.IdentifierToken, closeKind) == PostSkipAction.Continue)
                    {
                        goto tryAgain;
                    }
                }

                _termState = saveTerm;

                openToken = open;
                closeToken = this.EatToken(closeKind);
                subPatterns = list.ToList();
            }
            finally
            {
                if (!list.IsNull)
                {
                    _pool.Free(list);
                }
            }
        }

        private SubPositionalPatternSyntax ParseSubPositionalPattern()
        {
            NameColonSyntax nameColon = null;
            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken && this.PeekToken(1).Kind == SyntaxKind.ColonToken)
            {
                var name = this.ParseIdentifierName();
                var colon = this.EatToken(SyntaxKind.ColonToken);
                nameColon = _syntaxFactory.NameColon(name, colon);
            }

            PatternSyntax pattern = this.CurrentToken.Kind == SyntaxKind.CommaToken ?
                                            this.AddError(_syntaxFactory.ConstantPattern(this.CreateMissingIdentifierName()), ErrorCode.ERR_MissingArgument) :
                                            ParsePattern();

            return _syntaxFactory.SubPositionalPattern(nameColon, pattern);
        }

        // This method is used when we always want a pattern as a result.
        // For instance, it is used in parsing recursivepattern and propertypattern.
        // SubPatterns in these (recursivepattern, propertypattern) must be a type of Pattern.
        private PatternSyntax ParsePattern()
        {
            var node = this.ParseExpressionOrPattern();
            if (node is PatternSyntax)
            {
                return (PatternSyntax)node;
            }

            Debug.Assert(node is ExpressionSyntax);
            return _syntaxFactory.ConstantPattern((ExpressionSyntax)node);
        }

        // Priority is the ExpressionSyntax. It might return ExpressionSyntax which might be a constant pattern such as 'case 3:' 
        // All constant expressions are converted to the constant pattern in the switch binder if it is a match statement.
        // It is used for parsing patterns in the switch cases. It never returns constant pattern!
        private CSharpSyntaxNode ParseExpressionOrPattern()
        {
            var tk = this.CurrentToken.Kind;
            CSharpSyntaxNode node = null;

            if (tk == SyntaxKind.AsteriskToken)
            {
                var asteriskToken = this.EatToken();
                return _syntaxFactory.WildcardPattern(asteriskToken);
            }

            // If it is a nameof, skip the 'if' and parse as an expression. 
            if ((SyntaxFacts.IsPredefinedType(tk) || tk == SyntaxKind.IdentifierToken) &&
                  this.CurrentToken.ContextualKind != SyntaxKind.NameOfKeyword)
            {
                var resetPoint = this.GetResetPoint();
                try
                {
                    TypeSyntax type = this.ParseTypeCore(parentIsParameter: false, isOrAs: true, expectSizes: false, isArrayCreation: false);
                    tk = this.CurrentToken.Kind;
                    if (!type.IsMissing)
                    {
                        // X.Y.Z ( ... ) : PositionalPattern
                        if (tk == SyntaxKind.OpenParenToken)
                        {
                            node = _syntaxFactory.PositionalPattern(type, this.ParseSubPositionalPatternList());
                            node = CheckFeatureAvailability(node, MessageID.IDS_FeaturePatternMatching2);
                        }
                        // X.Y.Z { ... } : PropertyPattern
                        else if (tk == SyntaxKind.OpenBraceToken)
                        {
                            node = ParsePropertyPatternBody(type);
                        }
                        // X.Y.Z id
                        else if (this.IsTrueIdentifier())
                        {
                            var identifier = ParseIdentifierToken();
                            node = _syntaxFactory.DeclarationPattern(type, identifier);
                        }
                    }
                    if (node == null)
                    {
                        // it is an expression for typical switch case. 
                        // This can be transformed to the constant pattern in the SwitchBinder if there is a CaseMatchLabel in the sections.
                        this.Reset(ref resetPoint);
                        node = this.ParseSubExpression(Precedence.Expression);
                    }
                }
                finally
                {
                    this.Release(ref resetPoint);
                }
            }
            else
            {
                node = this.ParseSubExpression(Precedence.Expression);
            }
            return node;
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseSubPropertyPatternList(ref SyntaxToken openBrace)
        {
            var subPatterns = _pool.AllocateSeparated<ExpressionSyntax>();
            this.ParseSubPropertyPatternList(ref openBrace, subPatterns);
            var result = subPatterns.ToList();
            _pool.Free(subPatterns);
            return result;
        }

        private void ParseSubPropertyPatternList(ref SyntaxToken openBrace, SeparatedSyntaxListBuilder<ExpressionSyntax> list)
        {
            if (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
            {
                tryAgain:
                if (IsPossibleSubPropertyPattern() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    // first argument
                    list.Add(ParseSubPropertyPattern());

                    // additional arguments
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || IsPossibleSubPropertyPattern())
                        {
                            list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));

                            // check for exit case after legal trailing comma
                            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                            {
                                break;
                            }

                            list.Add(ParseSubPropertyPattern());
                            continue;
                        }
                        else if (this.SkipBadSubPatternListTokens(ref openBrace, list, SyntaxKind.CommaToken, SyntaxKind.CloseBraceToken) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }
                }
                else if (this.SkipBadSubPatternListTokens(ref openBrace, list, SyntaxKind.IdentifierToken, SyntaxKind.CloseBraceToken) == PostSkipAction.Continue)
                {
                    goto tryAgain;
                }
            }
        }

        private bool IsPossibleSubPropertyPattern()
        {
            return (this.CurrentToken.Kind == SyntaxKind.IdentifierToken) && (this.PeekToken(1).Kind == SyntaxKind.IsKeyword);
        }

        private IsPatternExpressionSyntax ParseSubPropertyPattern()
        {
            var name = this.EatToken(SyntaxKind.IdentifierToken);
            var identifier = _syntaxFactory.IdentifierName(name);
            var isKeyword = this.EatToken(SyntaxKind.IsKeyword);

            PatternSyntax pattern = this.CurrentToken.Kind == SyntaxKind.CommaToken ?
                                                        this.AddError(_syntaxFactory.ConstantPattern(this.CreateMissingIdentifierName()), ErrorCode.ERR_MissingArgument) :
                                                        ParsePattern();
            return _syntaxFactory.IsPatternExpression(identifier, isKeyword, pattern);
        }

        private PostSkipAction SkipBadSubPatternListTokens<TNode>(ref SyntaxToken open, SeparatedSyntaxListBuilder<TNode> list, SyntaxKind expected, SyntaxKind closeKind) where TNode : CSharpSyntaxNode
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossiblePattern(),
                p => p.CurrentToken.Kind == closeKind || p.CurrentToken.Kind == SyntaxKind.SemicolonToken || p.IsTerminator(),
                expected);
        }

        // It should not be accurate. This method is just used in the SkipBadSubPatternListTokens.
        // It is very general way of checking whether there is a possible pattern.
        private bool IsPossiblePattern()
        {
            // TODO(@gafter): this doesn't accept many forms that should be accepted, such as (1+1).
            var tk = this.CurrentToken.Kind;
            switch (tk)
            {
                case SyntaxKind.AsteriskToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.NullKeyword:
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.ArgListKeyword:
                    return true;
                case SyntaxKind.IdentifierToken:
                    var next = this.PeekToken(1).Kind;
                    if (next == SyntaxKind.DotToken || next == SyntaxKind.OpenBraceToken ||
                       next == SyntaxKind.OpenParenToken)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        private ExpressionSyntax ParseMatchExpression(ExpressionSyntax leftOperand, SyntaxToken opToken)
        {
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var sections = _pool.Allocate<MatchSectionSyntax>();
            while (this.CurrentToken.Kind == SyntaxKind.CaseKeyword)
            {
                var mcase = this.ParseMatchSection();
                sections.Add(mcase);
            }
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var result = _syntaxFactory.MatchExpression(leftOperand, opToken, openParen, sections, closeParen);
            _pool.Free(sections);
            return result;
        }

        private MatchSectionSyntax ParseMatchSection()
        {
            var caseKeyword = this.EatToken(SyntaxKind.CaseKeyword);
            var pattern = ParsePattern();
            var whenClause = ParseWhenClauseOpt();
            var colon = this.EatToken(SyntaxKind.ColonToken);
            var expression = ParseExpressionCore();
            return _syntaxFactory.MatchSection(caseKeyword, pattern, whenClause, colon, expression);
        }
    }
}
