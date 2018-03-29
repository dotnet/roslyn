// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer
        : AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<SyntaxKind, ParenthesizedExpressionSyntax>
    {
        protected override SyntaxKind GetSyntaxNodeKind()
            => SyntaxKind.ParenthesizedExpression;

        protected override bool CanRemoveParentheses(
            ParenthesizedExpressionSyntax parenthesizedExpression, SemanticModel semanticModel,
            out PrecedenceKind precedenceKind, out bool clarifiesPrecedence)
        {
            return CanRemoveParenthesesHelper(parenthesizedExpression, semanticModel, out precedenceKind, out clarifiesPrecedence);
        }

        public static bool CanRemoveParenthesesHelper(
            ParenthesizedExpressionSyntax parenthesizedExpression, SemanticModel semanticModel,
            out PrecedenceKind precedenceKind, out bool clarifiesPrecedence)
        {
            var result = parenthesizedExpression.CanRemoveParentheses(semanticModel);
            if (!result)
            {
                precedenceKind = default;
                clarifiesPrecedence = false;
                return false;
            }

            var inner = parenthesizedExpression.Expression;
            var innerKind = inner.Kind();
            var innerPrecedence = inner.GetOperatorPrecedence();
            var innerIsSimple = innerPrecedence == OperatorPrecedence.Primary ||
                                innerPrecedence == OperatorPrecedence.None;

            ExpressionSyntax parentExpression;

            switch (parenthesizedExpression.Parent)
            {
                case ConditionalExpressionSyntax _:
                    // If our parent is a conditional, then only remove parens if the inner
                    // expression is a primary. i.e. it's ok to remove any of the following:
                    //
                    //      (a()) ? (b.length) : (c[0])
                    //
                    // But we shouldn't remove parens for anything more complex like:
                    //
                    //      ++a ? b + c : d << e
                    //
                    precedenceKind = PrecedenceKind.Other;
                    clarifiesPrecedence = false;
                    return innerIsSimple;

                case CastExpressionSyntax _:
                    // if our parent is a cast, then we may have something like: (int)(-x)
                    // While the latter parens aren't necessary, they can help clarify precedence
                    // as '(int)-x' looks somewhat like a binary expression.
                    precedenceKind = PrecedenceKind.Cast;
                    clarifiesPrecedence = IsAmbiguousInnerExpressionKindForCast(innerKind);
                    return true;

                case BinaryExpressionSyntax binaryExpression:
                    parentExpression = binaryExpression;
                    break;

                case IsPatternExpressionSyntax isPatternExpression:
                    // on the left side of an 'x is pat' expression
                    parentExpression = isPatternExpression;
                    break;

                case ConstantPatternSyntax constantPattern when constantPattern.Parent is IsPatternExpressionSyntax isPatternExpression:
                    // on the right side of an 'x is const_pattern' expression
                    parentExpression = isPatternExpression;
                    break;

                default:
                    precedenceKind = PrecedenceKind.Other;
                    clarifiesPrecedence = false;
                    return true;
            }

            // We're parented by something binary-like. 
            precedenceKind = GetPrecedenceKind(parentExpression);

            // Precedence is clarified any time we have expression with different precedence
            // (and the inner expressoin is not a primary expression).  in other words, this
            // is helps clarify precedence:
            //
            //      a + (b * c)
            //
            // However, this does not:
            //
            //      a + (b.Length)
            clarifiesPrecedence = !innerIsSimple &&
                                  parentExpression.GetOperatorPrecedence() != innerPrecedence;
            return true;
        }

        public static bool IsAmbiguousInnerExpressionKindForCast(SyntaxKind kind)
            => kind == SyntaxKind.UnaryMinusExpression ||
               kind == SyntaxKind.UnaryPlusExpression ||
               kind == SyntaxKind.AddressOfExpression ||
               kind == SyntaxKind.PointerIndirectionExpression;

        public static PrecedenceKind GetPrecedenceKind(ExpressionSyntax parentExpression)
        {
            var precedence = parentExpression.GetOperatorPrecedence();
            switch (precedence)
            {
                case OperatorPrecedence.NullCoalescing: return PrecedenceKind.Coalesce;
                case OperatorPrecedence.ConditionalOr:
                case OperatorPrecedence.ConditionalAnd: return PrecedenceKind.Logical;
                case OperatorPrecedence.LogicalOr:
                case OperatorPrecedence.LogicalXor:
                case OperatorPrecedence.LogicalAnd: return PrecedenceKind.Bitwise;
                case OperatorPrecedence.Equality: return PrecedenceKind.Equality;
                case OperatorPrecedence.RelationalAndTypeTesting: return PrecedenceKind.Relational;
                case OperatorPrecedence.Shift: return PrecedenceKind.Shift;
                case OperatorPrecedence.Additive:
                case OperatorPrecedence.Multiplicative: return PrecedenceKind.Arithmetic;
            }

            throw ExceptionUtilities.UnexpectedValue(precedence);
        }
    }
}
