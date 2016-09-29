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
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.CommaToken:
                    // HACK: for error recovery, we prefer a (missing) type.
                    return this.ParseType(ParseTypeMode.Pattern);
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
                    TypeSyntax type = this.ParseType(ParseTypeMode.Pattern);

                    tk = this.CurrentToken.ContextualKind;
                    if (!type.IsMissing)
                    {
                        if (this.IsTrueIdentifier())
                        {
                            var identifier = ParseIdentifierToken();
                            node = _syntaxFactory.DeclarationPattern(type, identifier);
                        }
                    }

                    if (node == null)
                    {
                        Debug.Assert(Precedence.Shift == Precedence.Relational + 1);
                        if ((IsExpectedBinaryOperator(tk) && GetPrecedence(SyntaxFacts.GetBinaryExpression(tk)) > Precedence.Relational) ||
                            tk == SyntaxKind.DotToken) // member selection is not formally a binary operator but has higher precedence than relational
                        {
                            this.Reset(ref resetPoint);
                            // We parse a shift-expression ONLY (nothing looser) - i.e. not a relational expression
                            // So x is y < z should be parsed as (x is y) < z
                            // But x is y << z should be parsed as x is (y << z)
                            node = _syntaxFactory.ConstantPattern(this.ParseSubExpression(Precedence.Shift));
                        }
                        // it is a typical "is Type" operator
                        else
                        {
                            // Note that we don't bother checking for primary expressions such as X[e], X(e), X++, and X--
                            // as those are never semantically valid constant expressions for a pattern
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
                // In places where a pattern is supported, we do not support tuple types
                // due to both syntactic and semantic ambiguities between tuple types and positional patterns.

                // But it still might be a pattern such as (operand is 3) or (operand is nameof(x))
                node = _syntaxFactory.ConstantPattern(this.ParseExpressionCore());
            }
            return node;
        }

        // This method is used when we always want a pattern as a result.
        // For instance, it is used in parsing recursivepattern and propertypattern.
        // SubPatterns in these (recursivepattern, propertypattern) must be a type of Pattern.
        private PatternSyntax ParsePattern()
        {
            var node = this.ParseExpressionOrPattern(whenIsKeyword: false);
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
        private CSharpSyntaxNode ParseExpressionOrPattern(bool whenIsKeyword)
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
                    TypeSyntax type = this.ParseType(ParseTypeMode.Pattern);
                    if (!type.IsMissing)
                    {
                        // X.Y.Z id
                        if (this.IsTrueIdentifier() && (!whenIsKeyword || this.CurrentToken.ContextualKind != SyntaxKind.WhenKeyword))
                        {
                            var identifier = ParseIdentifierToken();
                            node = _syntaxFactory.DeclarationPattern(type, identifier);
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
