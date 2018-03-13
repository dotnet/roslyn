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
            if (result)
            {
                switch (parenthesizedExpression.Parent)
                {
                    case ConditionalExpressionSyntax _:
                        break;

                    case BinaryExpressionSyntax _:
                    case IsPatternExpressionSyntax _:
                        var parentExpression = (ExpressionSyntax)parenthesizedExpression.Parent;
                        precedenceKind = GetPrecedenceKind(parentExpression, semanticModel);

                        clarifiesPrecedence = parentExpression.GetOperatorPrecedence() != parenthesizedExpression.Expression.GetOperatorPrecedence();
                        return true;

                    default:
                        precedenceKind = PrecedenceKind.Other;
                        clarifiesPrecedence = false;
                        return true;
                }

            }

            precedenceKind = default;
            clarifiesPrecedence = false;
            return false;
        }

        public static PrecedenceKind GetPrecedenceKind(ExpressionSyntax parentExpression, SemanticModel semanticModel)
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
