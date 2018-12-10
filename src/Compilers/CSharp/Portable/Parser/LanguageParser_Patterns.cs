// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class LanguageParser : SyntaxParser
    {
        /// <summary>
        /// Parses the type, or pattern, right-hand operand of an is expression.
        /// Priority is the TypeSyntax. It may return a TypeSyntax which turns out in binding to
        /// be a constant pattern such as enum 'Days.Sunday'. We handle such cases in the binder of the is operator.
        /// </summary>
        private CSharpSyntaxNode ParseTypeOrPatternForIsOperator()
        {
            var tk = this.CurrentToken.Kind;
            Precedence precedence = GetPrecedence(SyntaxKind.IsPatternExpression);

            switch (tk)
            {
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
            if (SyntaxFacts.IsPredefinedType(tk) ||
                (tk == SyntaxKind.IdentifierToken &&
                  (this.CurrentToken.ContextualKind != SyntaxKind.NameOfKeyword || this.PeekToken(1).Kind != SyntaxKind.OpenParenToken)))
            {
                var resetPoint = this.GetResetPoint();
                try
                {
                    TypeSyntax type = this.ParseType(ParseTypeMode.AfterIs);

                    if (!type.IsMissing && this.IsTrueIdentifier())
                    {
                        var designation = ParseSimpleDesignation();
                        return _syntaxFactory.DeclarationPattern(type, designation);
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

            // We parse a shift-expression ONLY (nothing looser) - i.e. not a relational expression
            // So x is y < z should be parsed as (x is y) < z
            // But x is y << z should be parsed as x is (y << z)
            Debug.Assert(Precedence.Shift == precedence + 1);

            // In places where a pattern is supported, we do not support tuple types
            // due to both syntactic and semantic ambiguities between tuple types and positional patterns.
            // But it still might be a pattern such as (operand is 3) or (operand is nameof(x))
            return _syntaxFactory.ConstantPattern(this.ParseSubExpressionCore(precedence));
        }

        //
        // Parse an expression where a declaration expression would be permitted. This is suitable for use after
        // the `out` keyword in an argument list, or in the elements of a tuple literal (because they may
        // be on the left-hand-side of a deconstruction). The first element of a tuple is handled slightly
        // differently, as we check for the comma before concluding that the identifier should cause a
        // disambiguation. For example, for the input `(A < B , C > D)`, we treat this as a tuple with
        // two elements, because if we considered the `A<B,C>` to be a type, it wouldn't be a tuple at
        // all. Since we don't have such a thing as a one-element tuple (even for deconstruction), the
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
            if (!this.CurrentToken.IsVar())
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
                    var result = this.IsTrueIdentifier();
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

        // Priority is the ExpressionSyntax. It might return ExpressionSyntax which might be a constant pattern such as 'case 3:' 
        // All constant expressions are converted to the constant pattern in the switch binder if it is a match statement.
        // It is used for parsing patterns in the switch cases. It never returns constant pattern!
        private CSharpSyntaxNode ParseExpressionOrPatternForCase()
        {
            var tk = this.CurrentToken.Kind;
            CSharpSyntaxNode node = null;

            // If it is a nameof, skip the 'if' and parse as an expression. 
            if ((SyntaxFacts.IsPredefinedType(tk) || tk == SyntaxKind.IdentifierToken) &&
                  this.CurrentToken.ContextualKind != SyntaxKind.NameOfKeyword)
            {
                var resetPoint = this.GetResetPoint();
                try
                {
                    TypeSyntax type = this.ParseType(ParseTypeMode.AfterCase);
                    if (!type.IsMissing)
                    {
                        // X.Y.Z id
                        if (this.IsTrueIdentifier() && this.CurrentToken.ContextualKind != SyntaxKind.WhenKeyword)
                        {
                            var designation = ParseSimpleDesignation();
                            node = _syntaxFactory.DeclarationPattern(type, designation);
                        }
                    }
                    if (node == null)
                    {
                        // it is an expression for typical switch case. 
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
                // In places where a pattern is supported, we do not support tuple types
                // due to both syntactic and semantic ambiguities between tuple types and positional patterns.

                // But it still might be a pattern such as (operand is 3) or (operand is nameof(x))
                node = this.ParseSubExpression(Precedence.Expression);
            }

            return node;
        }
    }
}
