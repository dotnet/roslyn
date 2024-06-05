// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            var pattern = ParsePattern(GetPrecedence(SyntaxKind.IsPatternExpression), afterIs: true);
            return pattern switch
            {
                ConstantPatternSyntax cp when ConvertExpressionToType(cp.Expression, out NameSyntax? type) => type,
                TypePatternSyntax tp => tp.Type,
                DiscardPatternSyntax dp => _syntaxFactory.IdentifierName(ConvertToIdentifier(dp.UnderscoreToken)),
                var p => p,
            };
        }

        private bool ConvertExpressionToType(ExpressionSyntax expression, [NotNullWhen(true)] out NameSyntax? type)
        {
            switch (expression)
            {
                case SimpleNameSyntax s:
                    type = s;
                    return true;
                case MemberAccessExpressionSyntax { Expression: var expr, OperatorToken: { Kind: SyntaxKind.DotToken } dotToken, Name: var simpleName }
                        when ConvertExpressionToType(expr, out var leftType):
                    type = _syntaxFactory.QualifiedName(leftType, dotToken, simpleName);
                    return true;
                case AliasQualifiedNameSyntax a:
                    type = a;
                    return true;
                default:
                    type = null;
                    return false;
            };
        }

        private PatternSyntax ParsePattern(Precedence precedence, bool afterIs = false, bool whenIsKeyword = false)
        {
            return ParseDisjunctivePattern(precedence, afterIs, whenIsKeyword);
        }

        private PatternSyntax ParseDisjunctivePattern(Precedence precedence, bool afterIs, bool whenIsKeyword)
        {
            PatternSyntax result = ParseConjunctivePattern(precedence, afterIs, whenIsKeyword);
            while (this.CurrentToken.ContextualKind == SyntaxKind.OrKeyword)
            {
                result = _syntaxFactory.BinaryPattern(
                    SyntaxKind.OrPattern,
                    result,
                    ConvertToKeyword(this.EatToken()),
                    ParseConjunctivePattern(precedence, afterIs, whenIsKeyword));
            }

            return result;
        }

        /// <summary>
        /// Given tk, the type of the current token, does this look like the type of a pattern?
        /// </summary>
        private bool LooksLikeTypeOfPattern()
        {
            var tk = CurrentToken.Kind;
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

        private PatternSyntax ParseConjunctivePattern(Precedence precedence, bool afterIs, bool whenIsKeyword)
        {
            PatternSyntax result = ParseNegatedPattern(precedence, afterIs, whenIsKeyword);
            while (this.CurrentToken.ContextualKind == SyntaxKind.AndKeyword)
            {
                result = _syntaxFactory.BinaryPattern(
                    SyntaxKind.AndPattern,
                    result,
                    ConvertToKeyword(this.EatToken()),
                    ParseNegatedPattern(precedence, afterIs, whenIsKeyword));
            }

            return result;
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

        private PatternSyntax ParseNegatedPattern(Precedence precedence, bool afterIs, bool whenIsKeyword)
        {
            if (this.CurrentToken.ContextualKind == SyntaxKind.NotKeyword)
            {
                return _syntaxFactory.UnaryPattern(
                    ConvertToKeyword(this.EatToken()),
                    ParseNegatedPattern(precedence, afterIs, whenIsKeyword));
            }
            else
            {
                return ParsePrimaryPattern(precedence, afterIs, whenIsKeyword);
            }
        }

        private PatternSyntax ParsePrimaryPattern(Precedence precedence, bool afterIs, bool whenIsKeyword)
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
                    return _syntaxFactory.ConstantPattern(this.ParseIdentifierName(null, ErrorCode.ERR_MissingPattern));
            }

            if (CurrentToken.ContextualKind == SyntaxKind.UnderscoreToken)
            {
                return _syntaxFactory.DiscardPattern(this.EatContextualToken(SyntaxKind.UnderscoreToken));
            }

            switch (CurrentToken.Kind)
            {
                case SyntaxKind.OpenBracketToken:
                    return this.ParseListPattern(whenIsKeyword);
                case SyntaxKind.DotDotToken:
                    return _syntaxFactory.SlicePattern(EatToken(),
                        IsPossibleSubpatternElement() ? ParsePattern(precedence, afterIs: false, whenIsKeyword) : null);
                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.GreaterThanEqualsToken:
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.ExclamationEqualsToken:
                    // this is a relational pattern.
                    Debug.Assert(precedence < Precedence.Shift);
                    return _syntaxFactory.RelationalPattern(
                        this.EatToken(),
                        this.ParseSubExpression(Precedence.Relational));
            }

            using var resetPoint = this.GetDisposableResetPoint(resetOnDispose: false);

            TypeSyntax? type = null;
            if (LooksLikeTypeOfPattern())
            {
                type = this.ParseType(afterIs ? ParseTypeMode.AfterIs : ParseTypeMode.DefinitePattern);
                if (type.IsMissing || !CanTokenFollowTypeInPattern(precedence))
                {
                    // either it is not shaped like a type, or it is a constant expression.
                    resetPoint.Reset();
                    type = null;
                }
            }

            var pattern = ParsePatternContinued(type, precedence, whenIsKeyword);
            if (pattern != null)
                return pattern;

            resetPoint.Reset();
            var value = this.ParseSubExpression(precedence);
            return _syntaxFactory.ConstantPattern(value);
        }

        /// <summary>
        /// Is the current token something that could follow a type in a pattern?
        /// </summary>
        bool CanTokenFollowTypeInPattern(Precedence precedence)
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.CloseBraceToken:   // for efficiency, test some tokens that can follow a type pattern
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CommaToken:
                case SyntaxKind.SemicolonToken:
                    return true;
                case SyntaxKind.DotToken:
                    // int.MaxValue is an expression, not a type.
                    return false;
                case SyntaxKind.MinusGreaterThanToken:
                case SyntaxKind.ExclamationToken:
                    // parse as an expression for error recovery
                    return false;
                case var kind:
                    // If we find what looks like a continuation of an expression, it is not a type.
                    return !SyntaxFacts.IsBinaryExpressionOperatorToken(kind) ||
                           GetPrecedence(SyntaxFacts.GetBinaryExpression(kind)) <= precedence;
            }
        }

        private PatternSyntax? ParsePatternContinued(TypeSyntax? type, Precedence precedence, bool whenIsKeyword)
        {
            if (type?.Kind == SyntaxKind.IdentifierName)
            {
                var typeIdentifier = (IdentifierNameSyntax)type;
                var typeIdentifierToken = typeIdentifier.Identifier;
                if (typeIdentifierToken.ContextualKind == SyntaxKind.VarKeyword &&
                    (this.CurrentToken.Kind == SyntaxKind.OpenParenToken || this.IsValidPatternDesignation(whenIsKeyword)))
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
                var openParenToken = this.EatToken(SyntaxKind.OpenParenToken);
                var subPatterns = this.ParseCommaSeparatedSyntaxList(
                    ref openParenToken,
                    SyntaxKind.CloseParenToken,
                    static @this => @this.IsPossibleSubpatternElement(),
                    static @this => @this.ParseSubpatternElement(),
                    SkipBadPatternListTokens,
                    allowTrailingSeparator: false,
                    requireOneElement: false,
                    allowSemicolonAsSeparator: false);
                var closeParenToken = this.EatToken(SyntaxKind.CloseParenToken);

                parsePropertyPatternClause(out PropertyPatternClauseSyntax? propertyPatternClause0);
                var designation0 = TryParseSimpleDesignation(whenIsKeyword);

                if (type == null &&
                    propertyPatternClause0 == null &&
                    designation0 == null &&
                    subPatterns.Count == 1 &&
                    subPatterns.SeparatorCount == 0)
                {
                    var firstSubPattern = subPatterns[0];
                    RoslynDebug.AssertNotNull(firstSubPattern);

                    if (firstSubPattern.ExpressionColon == null)
                    {
                        var subpattern = firstSubPattern.Pattern;
                        switch (subpattern)
                        {
                            case ConstantPatternSyntax cp:
                                // There is an ambiguity between a positional pattern `(` pattern `)`
                                // and a constant expression pattern that happens to be parenthesized.
                                // Per 2017-11-20 LDM we treat such syntax as a parenthesized expression always.
                                ExpressionSyntax expression = _syntaxFactory.ParenthesizedExpression(openParenToken, cp.Expression, closeParenToken);
                                expression = ParseExpressionContinued(expression, precedence);
                                return _syntaxFactory.ConstantPattern(expression);
                            default:
                                return _syntaxFactory.ParenthesizedPattern(openParenToken, subpattern, closeParenToken);
                        }
                    }
                }

                var positionalPatternClause = _syntaxFactory.PositionalPatternClause(openParenToken, subPatterns, closeParenToken);
                var result = _syntaxFactory.RecursivePattern(type, positionalPatternClause, propertyPatternClause0, designation0);
                return result;
            }

            if (parsePropertyPatternClause(out PropertyPatternClauseSyntax? propertyPatternClause))
            {
                return _syntaxFactory.RecursivePattern(
                    type, positionalPatternClause: null, propertyPatternClause,
                    TryParseSimpleDesignation(whenIsKeyword));
            }

            if (type != null)
            {
                var designation = TryParseSimpleDesignation(whenIsKeyword);
                if (designation != null)
                    return _syntaxFactory.DeclarationPattern(type, designation);

                // We normally prefer an expression rather than a type in a pattern.
                return ConvertTypeToExpression(type, out var expression)
                    ? _syntaxFactory.ConstantPattern(ParseExpressionContinued(expression, precedence))
                    : _syntaxFactory.TypePattern(type);
            }

            // let the caller fall back to parsing an expression
            return null;

            bool parsePropertyPatternClause([NotNullWhen(true)] out PropertyPatternClauseSyntax? propertyPatternClauseResult)
            {
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    propertyPatternClauseResult = ParsePropertyPatternClause();
                    return true;
                }

                propertyPatternClauseResult = null;
                return false;
            }

            bool looksLikeCast()
            {
                using var _ = this.GetDisposableResetPoint(resetOnDispose: true);
                return this.ScanCast(forPattern: true);
            }
        }

        private VariableDesignationSyntax? TryParseSimpleDesignation(bool whenIsKeyword)
        {
            return this.IsTrueIdentifier() && this.IsValidPatternDesignation(whenIsKeyword)
                ? ParseSimpleDesignation()
                : null;
        }

        private bool IsValidPatternDesignation(bool whenIsKeyword)
        {
            if (CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                switch (CurrentToken.ContextualKind)
                {
                    case SyntaxKind.WhenKeyword:
                        return !whenIsKeyword;
                    case SyntaxKind.AndKeyword:
                    case SyntaxKind.OrKeyword:
                        var tk = PeekToken(1).Kind;
                        switch (tk)
                        {
                            case SyntaxKind.CloseBraceToken:
                            case SyntaxKind.CloseBracketToken:
                            case SyntaxKind.CloseParenToken:
                            case SyntaxKind.CommaToken:
                            case SyntaxKind.SemicolonToken:
                            case SyntaxKind.QuestionToken:
                            case SyntaxKind.ColonToken:
                                return true;
                            case SyntaxKind.LessThanEqualsToken:
                            case SyntaxKind.LessThanToken:
                            case SyntaxKind.GreaterThanEqualsToken:
                            case SyntaxKind.GreaterThanToken:
                            case SyntaxKind.IdentifierToken:
                            case SyntaxKind.OpenBraceToken:
                            case SyntaxKind.OpenParenToken:
                            case SyntaxKind.OpenBracketToken:
                                // these all can start a pattern
                                return false;
                            default:
                                {
                                    if (SyntaxFacts.IsBinaryExpression(tk)) return true; // `e is int and && true` is valid C# 7.0 code with `and` being a designator

                                    // If the following token could start an expression, it may be a constant pattern after a combinator.
                                    using var _ = this.GetDisposableResetPoint(resetOnDispose: true);
                                    this.EatToken();
                                    return !CanStartExpression();
                                }
                        }
                    case SyntaxKind.UnderscoreToken: // discard is a valid pattern designation
                    default:
                        return true;
                }
            }

            return false;
        }

        private CSharpSyntaxNode ParseExpressionOrPatternForSwitchStatement()
        {
            var savedState = _termState;
            _termState |= TerminatorState.IsExpressionOrPatternInCaseLabelOfSwitchStatement;
            var pattern = ParsePattern(Precedence.Conditional, whenIsKeyword: true);
            _termState = savedState;
            return ConvertPatternToExpressionIfPossible(pattern);
        }

        private CSharpSyntaxNode ConvertPatternToExpressionIfPossible(PatternSyntax pattern, bool permitTypeArguments = false)
        {
            return pattern switch
            {
                ConstantPatternSyntax cp => cp.Expression,
                TypePatternSyntax tp when ConvertTypeToExpression(tp.Type, out ExpressionSyntax? expr, permitTypeArguments) => expr,
                DiscardPatternSyntax dp => _syntaxFactory.IdentifierName(ConvertToIdentifier(dp.UnderscoreToken)),
                var p => p,
            };
        }

        private bool ConvertTypeToExpression(TypeSyntax type, [NotNullWhen(true)] out ExpressionSyntax? expr, bool permitTypeArguments = false)
        {
            switch (type)
            {
                case GenericNameSyntax g:
                    expr = g;
                    return permitTypeArguments;
                case SimpleNameSyntax s:
                    expr = s;
                    return true;
                case QualifiedNameSyntax { Left: var left, dotToken: var dotToken, Right: var right }
                            when (permitTypeArguments || right is not GenericNameSyntax):
                    var newLeft = ConvertTypeToExpression(left, out var leftExpr, permitTypeArguments: true) ? leftExpr : left;
                    expr = _syntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, newLeft, dotToken, right);
                    return true;
                default:
                    expr = null;
                    return false;
            }
        }

        private bool LooksLikeTupleArrayType()
        {
            if (this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
                return false;

            using var _ = GetDisposableResetPoint(resetOnDispose: true);
            return ScanType(forPattern: true) != ScanTypeFlags.NotType;
        }

        private PropertyPatternClauseSyntax ParsePropertyPatternClause()
        {
            var openBraceToken = this.EatToken(SyntaxKind.OpenBraceToken);
            var subPatterns = this.ParseCommaSeparatedSyntaxList(
                ref openBraceToken,
                SyntaxKind.CloseBraceToken,
                static @this => @this.IsPossibleSubpatternElement(),
                static @this => @this.ParseSubpatternElement(),
                SkipBadPatternListTokens,
                allowTrailingSeparator: true,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            return _syntaxFactory.PropertyPatternClause(
                openBraceToken,
                subPatterns,
                this.EatToken(SyntaxKind.CloseBraceToken));
        }

        private SubpatternSyntax ParseSubpatternElement()
        {
            BaseExpressionColonSyntax? exprColon = null;

            PatternSyntax pattern = ParsePattern(Precedence.Conditional);
            // If there is a colon but it's not preceded by a valid expression, leave it out to parse it as a missing comma, preserving C# 9.0 behavior.
            if (this.CurrentToken.Kind == SyntaxKind.ColonToken && ConvertPatternToExpressionIfPossible(pattern, permitTypeArguments: true) is ExpressionSyntax expr)
            {
                var colon = EatToken();
                exprColon = expr is IdentifierNameSyntax identifierName
                    ? _syntaxFactory.NameColon(identifierName, colon)
                    : _syntaxFactory.ExpressionColon(expr, colon);

                pattern = ParsePattern(Precedence.Conditional);
            }

            return _syntaxFactory.Subpattern(exprColon, pattern);
        }

        /// <summary>
        /// Check the next token to see if it is valid as the first token of a subpattern element.
        /// Used to assist in error recovery for subpattern lists (e.g. determining which tokens to skip)
        /// to ensure we make forward progress during recovery.
        /// </summary>
        private bool IsPossibleSubpatternElement()
        {
            return this.CanStartExpression() ||
                this.CurrentToken.Kind is
                    SyntaxKind.OpenBraceToken or
                    SyntaxKind.OpenBracketToken or
                    SyntaxKind.LessThanToken or
                    SyntaxKind.LessThanEqualsToken or
                    SyntaxKind.GreaterThanToken or
                    SyntaxKind.GreaterThanEqualsToken;
        }

        private static PostSkipAction SkipBadPatternListTokens<T>(
            LanguageParser @this, ref SyntaxToken open, SeparatedSyntaxListBuilder<T> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            where T : CSharpSyntaxNode
        {
            if (@this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.CloseBraceToken or SyntaxKind.CloseBracketToken or SyntaxKind.SemicolonToken)
                return PostSkipAction.Abort;

            // `:` is usually treated as incorrect separation token. This helps for error recovery in basic typing scenarios like `{ Prop:$$ Prop1: { ... } }`.
            // However, such behavior isn't much desirable when parsing pattern of a case label in a switch statement. For instance, consider the following example: `case { Prop: { }: case ...`.
            // Normally we would skip second `:` and `case` keyword after it as bad tokens and continue parsing pattern, which produces a lot of noise errors.
            // In order to avoid that and produce single error of missing `}` we exit on unexpected `:` in such cases.
            if (@this._termState.HasFlag(TerminatorState.IsExpressionOrPatternInCaseLabelOfSwitchStatement) && @this.CurrentToken.Kind is SyntaxKind.ColonToken)
                return PostSkipAction.Abort;

            // This is pretty much the same as above, but for switch expressions and `=>` and `:` tokens.
            // The reason why we cannot use single flag for both cases is because we want `=>` to be the "exit" token only for switch expressions.
            // Consider the following example: `case (() => 0):`. Normally `=>` is treated as bad separator, so we parse this basically the same as `case ((), 1):`, which is syntactically valid.
            // However, if we treated `=>` as "exit" token, parsing wouldn't consume full case label properly and would produce a lot of noise errors.
            // We can afford `:` to be the exit token for switch expressions because error recovery is already good enough and treats `:` as bad `=>`,
            // meaning that switch expression arm `{ : 1` can be recovered to `{ } => 1` where the closing `}` is missing and instead of `=>` we have `:`.
            if (@this._termState.HasFlag(TerminatorState.IsPatternInSwitchExpressionArm) && @this.CurrentToken.Kind is SyntaxKind.EqualsGreaterThanToken or SyntaxKind.ColonToken)
                return PostSkipAction.Abort;

            return @this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleSubpatternElement(),
                static (p, closeKind) => p.CurrentToken.Kind == closeKind || p.CurrentToken.Kind == SyntaxKind.SemicolonToken,
                expectedKind, closeKind);
        }

        private SwitchExpressionSyntax ParseSwitchExpression(ExpressionSyntax governingExpression, SyntaxToken switchKeyword)
        {
            // For better error recovery when an expression is typed on a line before a switch statement,
            // the caller checks if the switch keyword is followed by an open curly brace. Only if it is
            // would we attempt to parse it as a switch expression here.
            return _syntaxFactory.SwitchExpression(
                governingExpression,
                switchKeyword,
                this.EatToken(SyntaxKind.OpenBraceToken),
                parseSwitchExpressionArms(),
                this.EatToken(SyntaxKind.CloseBraceToken));

            SeparatedSyntaxList<SwitchExpressionArmSyntax> parseSwitchExpressionArms()
            {
                var arms = _pool.AllocateSeparated<SwitchExpressionArmSyntax>();

                while (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
                {
                    // Help out in the case where a user is converting a switch statement to a switch expression. Note:
                    // `default(...)` and `default` will also be consumed as a legal syntactic patterns (though the
                    // latter will fail during binding).  So if the user has `default:` we will recover fine as we
                    // handle the errant colon below.
                    var errantCase = this.CurrentToken.Kind == SyntaxKind.CaseKeyword
                        ? AddError(this.EatToken(), ErrorCode.ERR_BadCaseInSwitchArm)
                        : null;

                    var savedState = _termState;
                    _termState |= TerminatorState.IsPatternInSwitchExpressionArm;
                    var pattern = ParsePattern(Precedence.Coalescing, whenIsKeyword: true);
                    _termState = savedState;

                    // We use a precedence that excludes lambdas, assignments, and a conditional which could have a
                    // lambda on the right, because we need the parser to leave the EqualsGreaterThanToken to be
                    // consumed by the switch arm. The strange side-effect of that is that the conditional expression is
                    // not permitted as a constant expression here; it would have to be parenthesized.

                    var switchExpressionCase = _syntaxFactory.SwitchExpressionArm(
                        pattern,
                        ParseWhenClause(Precedence.Coalescing),
                        // Help out in the case where a user is converting a switch statement to a switch expression.
                        // Consume the `:` as a `=>` and report an error.
                        this.CurrentToken.Kind == SyntaxKind.ColonToken
                            ? this.EatTokenAsKind(SyntaxKind.EqualsGreaterThanToken)
                            : this.EatToken(SyntaxKind.EqualsGreaterThanToken),
                        ParseExpressionCore());

                    // If we're not making progress, abort
                    if (errantCase is null && switchExpressionCase.FullWidth == 0 && this.CurrentToken.Kind != SyntaxKind.CommaToken)
                        break;

                    if (errantCase != null)
                        switchExpressionCase = AddLeadingSkippedSyntax(switchExpressionCase, errantCase);

                    arms.Add(switchExpressionCase);
                    if (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
                    {
                        var commaToken = this.CurrentToken.Kind == SyntaxKind.SemicolonToken
                            ? this.EatTokenAsKind(SyntaxKind.CommaToken)
                            : this.EatToken(SyntaxKind.CommaToken);
                        arms.AddSeparator(commaToken);
                    }
                }

                return _pool.ToListAndFree(arms);
            }
        }

        private ListPatternSyntax ParseListPattern(bool whenIsKeyword)
        {
            var openBracket = this.EatToken(SyntaxKind.OpenBracketToken);
            var list = this.ParseCommaSeparatedSyntaxList(
                ref openBracket,
                SyntaxKind.CloseBracketToken,
                static @this => @this.IsPossibleSubpatternElement(),
                static @this => @this.ParsePattern(Precedence.Conditional),
                SkipBadPatternListTokens,
                allowTrailingSeparator: true,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            return _syntaxFactory.ListPattern(
                openBracket,
                list,
                this.EatToken(SyntaxKind.CloseBracketToken),
                TryParseSimpleDesignation(whenIsKeyword));
        }
    }
}
