// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

    internal partial class LanguageParser : SyntaxParser
    {
        /// <summary>
        /// Parses the type, or pattern, right-hand operand of an is expression.
        /// Priority is the TypeSyntax. It may return a TypeSyntax which turns out in binding to
        /// be a constant pattern such as enum 'Days.Sunday'. We handle such cases in the binder of the is operator.
        /// Note that the syntax `_` will be parsed as a type.
        /// </summary>
        private CSharpSyntaxNode ParseTypeOrPatternForIsOperator()
        {
            return CheckRecursivePatternFeature(ParseTypeOrPatternForIsOperatorCore());
        }

        private CSharpSyntaxNode CheckRecursivePatternFeature(CSharpSyntaxNode node)
        {
            switch (node.Kind)
            {
                case SyntaxKind.RecursivePattern:
                case SyntaxKind.DiscardPattern:
                case SyntaxKind.VarPattern when ((VarPatternSyntax)node).Designation.Kind == SyntaxKind.ParenthesizedVariableDesignation:
                    return this.CheckFeatureAvailability(node, MessageID.IDS_FeatureRecursivePatterns);
                default:
                    return node;
            }
        }

        private CSharpSyntaxNode ParseTypeOrPatternForIsOperatorCore()
        {
            var tk = this.CurrentToken.Kind;
            Precedence precedence = GetPrecedence(SyntaxKind.IsPatternExpression);

            // We will parse a shift-expression ONLY (nothing looser) - i.e. not a relational expression
            // So x is y < z should be parsed as (x is y) < z
            // But x is y << z should be parsed as x is (y << z)
            Debug.Assert(Precedence.Shift == precedence + 1);

            // For totally broken syntax, parse a type for error recovery purposes
            switch (tk)
            {
                case SyntaxKind.IdentifierToken when this.CurrentToken.ContextualKind == SyntaxKind.UnderscoreToken:
                // We permit a type named `_` on the right-hand-side of an is operator, but not inside of a pattern.
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.CommaToken:
                    // HACK: for error recovery, we prefer a (missing) type.
                    return this.ParseType(ParseTypeMode.AfterIs);
                default:
                    // attempt to disambiguate.
                    break;
            }

            // If it starts with 'nameof(', skip the 'if' and parse as a constant pattern.
            if (LooksLikeTypeOfPattern(tk))
            {
                var resetPoint = this.GetResetPoint();
                try
                {
                    TypeSyntax type = this.ParseType(ParseTypeMode.AfterIs);

                    if (!type.IsMissing)
                    {
                        PatternSyntax p = ParsePatternContinued(type, precedence, whenIsKeyword: false);
                        if (p != null)
                        {
                            return p;
                        }
                    }

                    tk = this.CurrentToken.ContextualKind;
                    if ((!IsExpectedBinaryOperator(tk) || GetPrecedence(SyntaxFacts.GetBinaryExpression(tk)) <= precedence) &&
                        // member selection is not formally a binary operator but has higher precedence than relational
                        tk != SyntaxKind.DotToken)
                    {
                        // it is a typical "is Type" operator.
                        // Note that we don't bother checking for primary expressions such as X[e], X(e), X++, and X--
                        // as those are never semantically valid constant expressions for a pattern
                        return type;
                    }

                    this.Reset(ref resetPoint);
                }
                finally
                {
                    this.Release(ref resetPoint);
                }
            }

            // check to see if it looks like a recursive pattern.
            if (tk == SyntaxKind.OpenParenToken || tk == SyntaxKind.OpenBraceToken)
            {
                var resetPoint = this.GetResetPoint();
                try
                {
                    PatternSyntax p = ParsePatternContinued(type: null, precedence, whenIsKeyword: false);
                    if (p != null)
                    {
                        return p;
                    }

                    // this can occur when we encounter a misplaced lambda expression.
                    this.Reset(ref resetPoint);
                }
                finally
                {
                    this.Release(ref resetPoint);
                }
            }

            // In places where a pattern is supported, we do not support tuple types
            // due to both syntactic and semantic ambiguities between tuple types and positional patterns.
            // But it still might be a pattern such as (operand is 3) or (operand is nameof(x))
            return _syntaxFactory.ConstantPattern(this.ParseSubExpressionCore(precedence));
        }

        /// <summary>
        /// Given tk, the type of the current token, does this look like the type of a pattern?
        /// </summary>
        private bool LooksLikeTypeOfPattern(SyntaxKind tk)
        {
            if (SyntaxFacts.IsPredefinedType(tk))
            {
                return true;
            }

            if (tk == SyntaxKind.IdentifierToken && this.CurrentToken.ContextualKind != SyntaxKind.UnderscoreToken &&
                (this.CurrentToken.ContextualKind != SyntaxKind.NameOfKeyword || this.PeekToken(1).Kind != SyntaxKind.OpenParenToken))
            {
                return true;
            }

            if (LooksLikeTupleArrayType())
            {
                return true;
            }

            // We'll parse the function pointer, but issue an error in semantic analysis
            if (IsFunctionPointerStart())
            {
                return true;
            }

            return false;
        }

        //
        // Parse an expression where a declaration expression would be permitted. This is suitable for use after
        // the `out` keyword in an argument list, or in the elements of a tuple literal (because they may
        // be on the left-hand-side of a positional subpattern). The first element of a tuple is handled slightly
        // differently, as we check for the comma before concluding that the identifier should cause a
        // disambiguation. For example, for the input `(A < B , C > D)`, we treat this as a tuple with
        // two elements, because if we considered the `A<B,C>` to be a type, it wouldn't be a tuple at
        // all. Since we don't have such a thing as a one-element tuple (even for positional subpattern), the
        // absence of the comma after the `D` means we don't treat the `D` as contributing to the
        // disambiguation of the expression/type. More formally, ...
        //
        // If a sequence of tokens can be parsed(in context) as a* simple-name* (§7.6.3), *member-access* (§7.6.5),
        // or* pointer-member-access* (§18.5.2) ending with a* type-argument-list* (§4.4.1), the token immediately
        // following the closing `>` token is examined, to see if it is
        // - One of `(  )  ]  }  :  ;  ,  .  ?  ==  !=  |  ^  &&  ||  &  [`; or
        // - One of the relational operators `<  >  <=  >=  is as`; or
        // - A contextual query keyword appearing inside a query expression; or
        // - In certain contexts, we treat *identifier* as a disambiguating token.Those contexts are where the
        //   sequence of tokens being disambiguated is immediately preceded by one of the keywords `is`, `case`
        //   or `out`, or arises while parsing the first element of a tuple literal(in which case the tokens are
        //   preceded by `(` or `:` and the identifier is followed by a `,`) or a subsequent element of a tuple literal.
        //
        // If the following token is among this list, or an identifier in such a context, then the *type-argument-list* is
        // retained as part of the *simple-name*, *member-access* or  *pointer-member-access* and any other possible parse
        // of the sequence of tokens is discarded.Otherwise, the *type-argument-list* is not considered to be part of the
        // *simple-name*, *member-access* or *pointer-member-access*, even if there is no other possible parse of the
        // sequence of tokens.Note that these rules are not applied when parsing a *type-argument-list* in a *namespace-or-type-name* (§3.8).
        //
        // See also ScanTypeArgumentList where these disambiguation rules are encoded.
        //
        private ExpressionSyntax ParseExpressionOrDeclaration(ParseTypeMode mode, MessageID feature, bool permitTupleDesignation)
        {
            return IsPossibleDeclarationExpression(mode, permitTupleDesignation)
                ? this.ParseDeclarationExpression(mode, feature)
                : this.ParseSubExpression(Precedence.Expression);
        }

        private bool IsPossibleDeclarationExpression(ParseTypeMode mode, bool permitTupleDesignation)
        {
            if (this.IsInAsync && this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword)
            {
                // can't be a declaration expression.
                return false;
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                bool typeIsVar = IsVarType();
                SyntaxToken lastTokenOfType;
                if (ScanType(mode, out lastTokenOfType) == ScanTypeFlags.NotType)
                {
                    return false;
                }

                // check for a designation
                if (!ScanDesignation(permitTupleDesignation && (typeIsVar || IsPredefinedType(lastTokenOfType.Kind))))
                {
                    return false;
                }

                switch (mode)
                {
                    case ParseTypeMode.FirstElementOfPossibleTupleLiteral:
                        return this.CurrentToken.Kind == SyntaxKind.CommaToken;
                    case ParseTypeMode.AfterTupleComma:
                        return this.CurrentToken.Kind == SyntaxKind.CommaToken || this.CurrentToken.Kind == SyntaxKind.CloseParenToken;
                    default:
                        // The other case where we disambiguate between a declaration and expression is before the `in` of a foreach loop.
                        // There we err on the side of accepting a declaration.
                        return true;
                }
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }
        }

        /// <summary>
        /// Is the following set of tokens, interpreted as a type, the type <c>var</c>?
        /// </summary>
        private bool IsVarType()
        {
            if (!this.CurrentToken.IsIdentifierVar())
            {
                return false;
            }

            switch (this.PeekToken(1).Kind)
            {
                case SyntaxKind.DotToken:
                case SyntaxKind.ColonColonToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.AsteriskToken:
                case SyntaxKind.QuestionToken:
                case SyntaxKind.LessThanToken:
                    return false;
                default:
                    return true;
            }
        }

        private bool ScanDesignation(bool permitTuple)
        {
            switch (this.CurrentToken.Kind)
            {
                default:
                    return false;
                case SyntaxKind.IdentifierToken:
                    bool result = this.IsTrueIdentifier();
                    this.EatToken();
                    return result;
                case SyntaxKind.OpenParenToken:
                    if (!permitTuple)
                    {
                        return false;
                    }

                    bool sawComma = false;
                    while (true)
                    {
                        this.EatToken(); // consume the `(` or `,`
                        if (!ScanDesignation(permitTuple: true))
                        {
                            return false;
                        }
                        switch (this.CurrentToken.Kind)
                        {
                            case SyntaxKind.CloseParenToken:
                                this.EatToken();
                                return sawComma;
                            case SyntaxKind.CommaToken:
                                sawComma = true;
                                continue;
                            default:
                                return false;
                        }
                    }
            }
        }

        /// <summary>
        /// Here is the grammar being parsed:
        /// ``` antlr
        /// pattern
        /// 	: declaration_pattern
        /// 	| constant_pattern
        /// 	| positional_pattern
        /// 	| property_pattern
        /// 	| discard_pattern
        /// 	;
        /// declaration_pattern
        /// 	: type identifier
        /// 	;
        /// constant_pattern
        /// 	: expression
        /// 	;
        /// positional_pattern
        /// 	: type? '(' subpatterns? ')' property_subpattern? identifier?
        /// 	;
        /// subpatterns
        /// 	: subpattern
        /// 	| subpattern ',' subpatterns
        /// 	;
        /// subpattern
        /// 	: pattern
        /// 	| identifier ':' pattern
        /// 	;
        /// property_subpattern
        /// 	: '{' subpatterns? '}'
        /// 	;
        /// property_pattern
        /// 	: property_subpattern identifier?
        /// 	;
        /// discard_pattern
        /// 	: '_'
        /// 	;
        /// ```
        ///
        /// Priority is the ExpressionSyntax. It might return ExpressionSyntax which might be a constant pattern such as 'case 3:' 
        /// All constant expressions are converted to the constant pattern in the switch binder if it is a match statement.
        /// It is used for parsing patterns in the switch cases. It never returns constant pattern, because for a `case` we
        /// need to use a pre-pattern-matching syntax node for a constant case.
        /// </summary>
        /// <param name="whenIsKeyword">prevents the use of "when" for the identifier</param>
        /// <returns></returns>
        private CSharpSyntaxNode ParseExpressionOrPattern(bool whenIsKeyword, bool forSwitchCase, Precedence precedence)
        {
            // handle common error recovery situations during typing
            var tk = this.CurrentToken.Kind;
            switch (tk)
            {
                case SyntaxKind.CommaToken:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.EqualsGreaterThanToken:
                    return this.ParseIdentifierName(ErrorCode.ERR_MissingPattern);
            }

            if (CurrentToken.ContextualKind == SyntaxKind.UnderscoreToken && !forSwitchCase)
            {
                // In a switch case, we parse `_` as an expression.
                return _syntaxFactory.DiscardPattern(this.EatContextualToken(SyntaxKind.UnderscoreToken));
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                TypeSyntax type = null;
                if (LooksLikeTypeOfPattern(tk))
                {
                    type = this.ParseType(ParseTypeMode.DefinitePattern);
                    if (type.IsMissing || !CanTokenFollowTypeInPattern())
                    {
                        // either it is not shaped like a type, or it is a constant expression.
                        this.Reset(ref resetPoint);
                        type = null;
                    }
                }

                PatternSyntax p = ParsePatternContinued(type, precedence, whenIsKeyword);
                if (p != null)
                {
                    return (whenIsKeyword && p is ConstantPatternSyntax c) ? c.expression : (CSharpSyntaxNode)p;
                }

                this.Reset(ref resetPoint);
                return this.ParseSubExpression(precedence);
            }
            finally
            {
                this.Release(ref resetPoint);
            }
        }

        private bool LooksLikeTupleArrayType()
        {
            if (this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
            {
                return false;
            }

            ResetPoint resetPoint = GetResetPoint();
            try
            {
                return ScanType(forPattern: true) != ScanTypeFlags.NotType;
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }
        }

        /// <summary>
        /// Is the current token something that could follow a type in a pattern?
        /// </summary>
        private bool CanTokenFollowTypeInPattern()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.IdentifierToken:
                    return true;
                default:
                    return false;
            }
        }

        private PatternSyntax ParsePatternContinued(TypeSyntax type, Precedence precedence, bool whenIsKeyword)
        {
            if (type?.Kind == SyntaxKind.IdentifierName)
            {
                var typeIdentifier = (IdentifierNameSyntax)type;
                var typeIdentifierToken = typeIdentifier.Identifier;
                if (typeIdentifierToken.ContextualKind == SyntaxKind.VarKeyword &&
                    CanTokenFollowTypeInPattern() && (!whenIsKeyword || this.CurrentToken.ContextualKind != SyntaxKind.WhenKeyword))
                {
                    // we have a "var" pattern; "var" is not permitted to be a stand-in for a type (or a constant) in a pattern.
                    var varToken = ConvertToKeyword(typeIdentifierToken);
                    var varDesignation = ParseDesignation(forPattern: true);
                    return _syntaxFactory.VarPattern(varToken, varDesignation);
                }
            }

            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken && (type != null || !looksLikeCast()))
            {
                // It is possible this is a parenthesized (constant) expression.
                // We normalize later.
                ParseSubpatternList(
                    openToken: out SyntaxToken openParenToken,
                    subPatterns: out SeparatedSyntaxList<SubpatternSyntax> subPatterns,
                    closeToken: out SyntaxToken closeParenToken,
                    openKind: SyntaxKind.OpenParenToken,
                    closeKind: SyntaxKind.CloseParenToken);

                parsePropertyPatternClause(out PropertyPatternClauseSyntax propertyPatternClause0);
                parseDesignation(out VariableDesignationSyntax designation0);

                if (type == null &&
                    propertyPatternClause0 == null &&
                    designation0 == null &&
                    subPatterns.Count == 1 &&
                    subPatterns[0].NameColon == null &&
                    subPatterns.SeparatorCount == 0)
                {
                    if (subPatterns[0].Pattern is ConstantPatternSyntax cp)
                    {
                        // There is an ambiguity between a positional pattern `(` pattern `)`
                        // and a constant expression pattern that happens to be parenthesized.
                        // Per 2017-11-20 LDM we treat such syntax as a parenthesized expression always.
                        ExpressionSyntax expression = _syntaxFactory.ParenthesizedExpression(openParenToken, cp.Expression, closeParenToken);
                        expression = ParseExpressionContinued(expression, precedence);
                        return _syntaxFactory.ConstantPattern(expression);
                    }
                }

                var positionalPatternClause = _syntaxFactory.PositionalPatternClause(openParenToken, subPatterns, closeParenToken);
                var result = _syntaxFactory.RecursivePattern(type, positionalPatternClause, propertyPatternClause0, designation0);

                bool singleElementPattern =
                    type == null &&
                    subPatterns.Count == 1 &&
                    propertyPatternClause0 == null &&
                    designation0 == null &&
                    subPatterns[0].NameColon == null;
                // A single-element parenthesized pattern requires some other syntax to disambiguate it from a merely parenthesized pattern,
                // thus leaving open the possibility that we can use parentheses for grouping patterns in the future, e.g. if we introduce `or` and
                // `and` patterns.
                return singleElementPattern ? this.AddError(result, ErrorCode.ERR_SingleElementPositionalPatternRequiresDisambiguation) : result;
            }

            if (parsePropertyPatternClause(out PropertyPatternClauseSyntax propertyPatternClause))
            {
                parseDesignation(out VariableDesignationSyntax designation0);
                return _syntaxFactory.RecursivePattern(type, positionalPatternClause: null, propertyPatternClause, designation0);
            }

            if (type != null && parseDesignation(out VariableDesignationSyntax designation))
            {
                return _syntaxFactory.DeclarationPattern(type, designation);
            }

            // let the caller fall back to its default (expression or type)
            return null;

            bool parsePropertyPatternClause(out PropertyPatternClauseSyntax propertyPatternClauseResult)
            {
                propertyPatternClauseResult = null;
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    propertyPatternClauseResult = ParsePropertyPatternClause();
                    return true;
                }

                return false;
            }

            bool parseDesignation(out VariableDesignationSyntax designationResult)
            {
                designationResult = null;
                if (this.IsTrueIdentifier() && (!whenIsKeyword || this.CurrentToken.ContextualKind != SyntaxKind.WhenKeyword))
                {
                    designationResult = ParseSimpleDesignation();
                    return true;
                }

                return false;
            }

            bool looksLikeCast()
            {
                var resetPoint = this.GetResetPoint();
                bool result = this.ScanCast(forPattern: true);
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
                return result;
            }
        }

        private PatternSyntax ParsePattern(Precedence precedence, bool whenIsKeyword = false)
        {
            var node = ParseExpressionOrPattern(whenIsKeyword: whenIsKeyword, forSwitchCase: false, precedence: precedence);
            switch (node)
            {
                case PatternSyntax pattern:
                    return pattern;
                case ExpressionSyntax expression:
                    return _syntaxFactory.ConstantPattern(expression);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }
        }

        private PropertyPatternClauseSyntax ParsePropertyPatternClause()
        {
            ParseSubpatternList(
                openToken: out SyntaxToken openBraceToken,
                subPatterns: out SeparatedSyntaxList<SubpatternSyntax> subPatterns,
                closeToken: out SyntaxToken closeBraceToken,
                openKind: SyntaxKind.OpenBraceToken,
                closeKind: SyntaxKind.CloseBraceToken);
            return _syntaxFactory.PropertyPatternClause(openBraceToken, subPatterns, closeBraceToken);
        }

        private void ParseSubpatternList(
            out SyntaxToken openToken,
            out SeparatedSyntaxList<SubpatternSyntax> subPatterns,
            out SyntaxToken closeToken,
            SyntaxKind openKind,
            SyntaxKind closeKind)
        {
            Debug.Assert(openKind == SyntaxKind.OpenParenToken || openKind == SyntaxKind.OpenBraceToken);
            Debug.Assert(closeKind == SyntaxKind.CloseParenToken || closeKind == SyntaxKind.CloseBraceToken);
            Debug.Assert((openKind == SyntaxKind.OpenParenToken) == (closeKind == SyntaxKind.CloseParenToken));
            Debug.Assert(openKind == this.CurrentToken.Kind);

            openToken = this.EatToken(openKind);
            var list = _pool.AllocateSeparated<SubpatternSyntax>();
            try
            {
tryAgain:

                if (this.IsPossibleSubpatternElement() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    // first pattern
                    list.Add(this.ParseSubpatternElement());

                    // additional patterns
                    int lastTokenPosition = -1;
                    while (IsMakingProgress(ref lastTokenPosition))
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken ||
                            this.CurrentToken.Kind == SyntaxKind.CloseBraceToken ||
                            this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleSubpatternElement())
                        {
                            list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                            {
                                break;
                            }
                            list.Add(this.ParseSubpatternElement());
                            continue;
                        }
                        else if (this.SkipBadPatternListTokens(ref openToken, list, SyntaxKind.CommaToken, closeKind) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }
                }
                else if (this.SkipBadPatternListTokens(ref openToken, list, SyntaxKind.IdentifierToken, closeKind) == PostSkipAction.Continue)
                {
                    goto tryAgain;
                }

                closeToken = this.EatToken(closeKind);
                subPatterns = list.ToList();
            }
            finally
            {
                _pool.Free(list);
            }
        }

        private SubpatternSyntax ParseSubpatternElement()
        {
            NameColonSyntax nameColon = null;
            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken && this.PeekToken(1).Kind == SyntaxKind.ColonToken)
            {
                var name = this.ParseIdentifierName();
                var colon = this.EatToken(SyntaxKind.ColonToken);
                nameColon = _syntaxFactory.NameColon(name, colon);
            }

            var pattern = ParsePattern(Precedence.Conditional);
            return this._syntaxFactory.Subpattern(nameColon, pattern);
        }

        /// <summary>
        /// Check the next token to see if it is valid as the first token of a subpattern element.
        /// Used to assist in error recovery for subpattern lists (e.g. determining which tokens to skip)
        /// to ensure we make forward progress during recovery.
        /// </summary>
        private bool IsPossibleSubpatternElement()
        {
            return this.IsPossibleExpression(allowBinaryExpressions: false, allowAssignmentExpressions: false) ||
                this.CurrentToken.Kind == SyntaxKind.OpenBraceToken;
        }

        private PostSkipAction SkipBadPatternListTokens(
            ref SyntaxToken open,
            SeparatedSyntaxListBuilder<SubpatternSyntax> list,
            SyntaxKind expected,
            SyntaxKind closeKind)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleSubpatternElement(),
                p => p.CurrentToken.Kind == closeKind || p.CurrentToken.Kind == SyntaxKind.SemicolonToken || p.IsTerminator(),
                expected);
        }

        private ExpressionSyntax ParseSwitchExpression(ExpressionSyntax governingExpression, SyntaxToken switchKeyword)
        {
            // For better error recovery when an expression is typed on a line before a switch statement,
            // the caller checks if the switch keyword is followed by an open curly brace. Only if it is
            // would we attempt to parse it as a switch expression here.
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            var arms = this.ParseSwitchExpressionArms();
            var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
            var result = _syntaxFactory.SwitchExpression(governingExpression, switchKeyword, openBrace, arms, closeBrace);
            result = this.CheckFeatureAvailability(result, MessageID.IDS_FeatureRecursivePatterns);
            return result;
        }

        private SeparatedSyntaxList<SwitchExpressionArmSyntax> ParseSwitchExpressionArms()
        {
            var arms = _pool.AllocateSeparated<SwitchExpressionArmSyntax>();

            while (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
            {
                // We use a precedence that excludes lambdas, assignments, and a conditional which could have a
                // lambda on the right, because we need the parser to leave the EqualsGreaterThanToken
                // to be consumed by the switch arm. The strange side-effect of that is that the conditional
                // expression is not permitted as a constant expression here; it would have to be parenthesized.
                var pattern = ParsePattern(Precedence.Coalescing, whenIsKeyword: true);
                var whenClause = ParseWhenClause(Precedence.Coalescing);
                var arrow = this.EatToken(SyntaxKind.EqualsGreaterThanToken);
                var expression = ParseExpressionCore();
                var switchExpressionCase = _syntaxFactory.SwitchExpressionArm(pattern, whenClause, arrow, expression);

                // If we're not making progress, abort
                if (switchExpressionCase.Width == 0 && this.CurrentToken.Kind != SyntaxKind.CommaToken)
                    break;

                arms.Add(switchExpressionCase);
                if (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
                {
                    var commaToken = this.CurrentToken.Kind == SyntaxKind.SemicolonToken
                        ? this.EatTokenAsKind(SyntaxKind.CommaToken)
                        : this.EatToken(SyntaxKind.CommaToken);
                    arms.AddSeparator(commaToken);
                }
            }

            SeparatedSyntaxList<SwitchExpressionArmSyntax> result = arms;
            _pool.Free(arms);
            return result;
        }
    }
}
