// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer
        : AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<SyntaxKind, ParenthesizedExpressionSyntax>
    {
        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override SyntaxKind GetSyntaxNodeKind()
            => SyntaxKind.ParenthesizedExpression;

        protected override bool CanRemoveParentheses(
            ParenthesizedExpressionSyntax parenthesizedExpression, SemanticModel semanticModel,
            out PrecedenceKind precedence, out bool clarifiesPrecedence)
        {
            return CanRemoveParenthesesHelper(
                parenthesizedExpression, semanticModel,
                out precedence, out clarifiesPrecedence);
        }

        public static bool CanRemoveParenthesesHelper(
            ParenthesizedExpressionSyntax parenthesizedExpression, SemanticModel semanticModel,
            out PrecedenceKind parentPrecedenceKind, out bool clarifiesPrecedence)
        {
            var result = parenthesizedExpression.CanRemoveParentheses(semanticModel);
            if (!result)
            {
                parentPrecedenceKind = default;
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
                    parentPrecedenceKind = PrecedenceKind.Other;
                    clarifiesPrecedence = false;
                    return innerIsSimple;

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
                    parentPrecedenceKind = PrecedenceKind.Other;
                    clarifiesPrecedence = false;
                    return true;
            }

            // We're parented by something binary-like. 
            parentPrecedenceKind = CSharpPrecedenceService.Instance.GetPrecedenceKind(parentExpression);

            // Precedence is clarified any time we have expression with different precedence
            // (and the inner expression is not a primary expression).  in other words, this
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
    }
}
