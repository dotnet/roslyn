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
            var pattern = ParsePattern(GetPrecedence(SyntaxKind.IsPatternExpression), afterIs: true);
            return pattern switch
            {
                ConstantPatternSyntax cp when ConvertExpressionToType(cp.Expression, out NameSyntax type) => type,
                TypePatternSyntax tp => tp.Type,
                DiscardPatternSyntax dp => _syntaxFactory.IdentifierName(ConvertToIdentifier(dp.UnderscoreToken)),
                var p => p,
            };
        }

        private bool ConvertExpressionToType(ExpressionSyntax expression, out NameSyntax type)
        {
            type = null;
            switch (expression)
            {
                case SimpleNameSyntax s:
                    type = s;
                    return true;
                case MemberAccessExpressionSyntax { Expression: var expr, OperatorToken: { Kind: SyntaxKind.DotToken } dotToken, Name: var simpleName }
                        when ConvertExpressionToType(expr, out var leftType):
                    type = _syntaxFactory.QualifiedName(leftType, dotToken, simpleName);
                    return true;
                default:
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
                var orToken = ConvertToKeyword(this.EatToken());
                var right = ParseConjunctivePattern(precedence, afterIs, whenIsKeyword);
                result = _syntaxFactory.BinaryPattern(result, orToken, right);
                result = CheckFeatureAvailability(result, MessageID.IDS_FeatureOrPattern);
            }

            return result;
        }

        /// <summary>
        /// Given tk, the type of the current token, does this look like the type of a pattern?
        /// </summary>
        private bool LooksLikeTypeOfPattern(SyntaxKind tk)
        {
            return SyntaxFacts.IsPredefinedType(tk) ||
                (tk == SyntaxKind.IdentifierToken && this.CurrentToken.ContextualKind != SyntaxKind.UnderscoreToken &&
                  (this.CurrentToken.ContextualKind != SyntaxKind.NameOfKeyword || this.PeekToken(1).Kind != SyntaxKind.OpenParenToken)) ||
                LooksLikeTupleArrayType();
        }

        private PatternSyntax ParseConjunctivePattern(Precedence precedence, bool afterIs, bool whenIsKeyword)
        {
            PatternSyntax result = ParseNegatedPattern(precedence, afterIs, whenIsKeyword);
            while (this.CurrentToken.ContextualKind == SyntaxKind.AndKeyword)
            {
                var orToken = ConvertToKeyword(this.EatToken());
                var right = ParseConjunctivePattern(precedence, afterIs, whenIsKeyword);
                result = _syntaxFactory.BinaryPattern(result, orToken, right);
                result = CheckFeatureAvailability(result, MessageID.IDS_FeatureAndPattern);
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
                var notToken = ConvertToKeyword(this.EatToken());
                var pattern = ParseNegatedPattern(precedence, afterIs, whenIsKeyword);
                var result = _syntaxFactory.UnaryPattern(notToken, pattern);
                return CheckFeatureAvailability(result, MessageID.IDS_FeatureNotPattern);
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
                    return _syntaxFactory.ConstantPattern(this.ParseIdentifierName(ErrorCode.ERR_MissingPattern));
            }

            if (CurrentToken.ContextualKind == SyntaxKind.UnderscoreToken)
            {
                return _syntaxFactory.DiscardPattern(this.EatContextualToken(SyntaxKind.UnderscoreToken));
            }

            switch (CurrentToken.Kind)
            {
                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.GreaterThanEqualsToken:
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.ExclamationEqualsToken:
                    // this is a relational pattern.
                    var relationalToken = this.EatToken();
                    Debug.Assert(precedence < Precedence.Shift);
                    // PROTOTYPE(ngafter): is this always a subexpression? (to ensure ParseWithStackGuard).
                    var expression = this.ParseSubExpression(Precedence.Relational);
                    var result = _syntaxFactory.RelationalPattern(relationalToken, expression);
                    return CheckFeatureAvailability(result, MessageID.IDS_FeatureRelationalPattern);
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                TypeSyntax type = null;
                if (LooksLikeTypeOfPattern(tk))
                {
                    type = this.ParseType(afterIs ? ParseTypeMode.AfterIs : ParseTypeMode.DefinitePattern);
                    if (type.IsMissing || !CanTokenFollowTypeInPattern(precedence))
                    {
                        // either it is not shaped like a type, or it is a constant expression.
                        this.Reset(ref resetPoint);
                        type = null;
                    }
                }

                PatternSyntax p = ParsePatternContinued(type, precedence, whenIsKeyword);
                if (p != null)
                    return p;

                this.Reset(ref resetPoint);
                var value = this.ParseSubExpression(precedence);
                return _syntaxFactory.ConstantPattern(value);
            }
            finally
            {
                this.Release(ref resetPoint);
            }
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
                    return true;
                case SyntaxKind.DotToken:
                    // int.MaxValue is an expression, not a type.
                    return false;
                case var kind:
                    // If we find what looks like a continuation of an expression, it is not a type.
                    return !SyntaxFacts.IsBinaryExpressionOperatorToken(kind) ||
                           GetPrecedence(SyntaxFacts.GetBinaryExpression(kind)) <= precedence;
            }
        }

        private PatternSyntax ParsePatternContinued(TypeSyntax type, Precedence precedence, bool whenIsKeyword)
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
                    subPatterns[0].NameColon == null
                    )
                {
                    var subpattern = subPatterns[0].Pattern;
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
                            var parenthesizedPattern = _syntaxFactory.ParenthesizedPattern(openParenToken, subPatterns[0].Pattern, closeParenToken);
                            return CheckFeatureAvailability(parenthesizedPattern, MessageID.IDS_FeatureParenthesizedPattern);
                    }
                }

                var positionalPatternClause = _syntaxFactory.PositionalPatternClause(openParenToken, subPatterns, closeParenToken);
                var result = _syntaxFactory.RecursivePattern(type, positionalPatternClause, propertyPatternClause0, designation0);
                return result;
            }

            if (parsePropertyPatternClause(out PropertyPatternClauseSyntax propertyPatternClause))
            {
                parseDesignation(out VariableDesignationSyntax designation0);
                return _syntaxFactory.RecursivePattern(type, positionalPatternClause: null, propertyPatternClause, designation0);
            }

            if (type != null)
            {
                if (parseDesignation(out VariableDesignationSyntax designation))
                {
                    return _syntaxFactory.DeclarationPattern(type, designation);
                }
                else
                {
                    // We normally prefer an expression rather than a type in a pattern.
                    if (ConvertTypeToExpression(type, out var expression))
                    {
                        expression = ParseExpressionContinued(expression, precedence);
                        return _syntaxFactory.ConstantPattern(expression);
                    }

                    var typePattern = _syntaxFactory.TypePattern(type);
                    return CheckFeatureAvailability(typePattern, MessageID.IDS_FeatureTypePattern);
                }
            }

            // let the caller fall back to parsing an expression
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
                if (this.IsTrueIdentifier() && this.IsValidPatternDesignation(whenIsKeyword))
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

        private bool IsValidPatternDesignation(bool whenIsKeyword)
        {
            if (CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                switch (CurrentToken.ContextualKind)
                {
                    // PROTOTYPE(ngafter): document breaking change that we now disallow these identifiers for a pattern designation.
                    case SyntaxKind.WhenKeyword:
                        return !whenIsKeyword;
                    case SyntaxKind.AndKeyword:
                    case SyntaxKind.OrKeyword:
                        return false;
                    case SyntaxKind.UnderscoreToken: // discard is a valid pattern designation
                    default:
                        return true;
                }
            }

            return false;
        }

        private CSharpSyntaxNode ParseExpressionOrPatternForSwitchStatement()
        {
            return CheckRecursivePatternFeature(ParseExpressionOrPatternForSwitchStatementCore());
        }

        private CSharpSyntaxNode ParseExpressionOrPatternForSwitchStatementCore()
        {
            var pattern = ParsePattern(Precedence.Conditional, whenIsKeyword: true);
            return pattern switch
            {
                ConstantPatternSyntax cp => cp.Expression,
                TypePatternSyntax tp when ConvertTypeToExpression(tp.Type, out ExpressionSyntax expr) => expr,
                DiscardPatternSyntax dp => _syntaxFactory.IdentifierName(ConvertToIdentifier(dp.UnderscoreToken)),
                var p => p,
            };
        }

        private bool ConvertTypeToExpression(TypeSyntax type, out ExpressionSyntax expr, bool permitTypeArguments = false)
        {
            expr = null;
            switch (type)
            {
                case SimpleNameSyntax s:
                    expr = s;
                    return true;
                case QualifiedNameSyntax { Left: var left, dotToken: var dotToken, Right: var right }
                            when (permitTypeArguments || !(right is GenericNameSyntax)) && ConvertTypeToExpression(left, out var leftExpr, permitTypeArguments: true):
                    expr = _syntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, leftExpr, dotToken, right);
                    return true;
                default:
                    return false;
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
