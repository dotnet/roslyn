// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Precedence;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Precedence;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnnecessaryExpressionParenthesesDiagnosticAnalyzer
    : AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<SyntaxKind, ParenthesizedExpressionSyntax>
{
    protected override SyntaxKind GetSyntaxKind()
        => SyntaxKind.ParenthesizedExpression;

    protected override ISyntaxFacts GetSyntaxFacts()
        => CSharpSyntaxFacts.Instance;

    protected override bool CanRemoveParentheses(
        ParenthesizedExpressionSyntax parenthesizedExpression,
        SemanticModel semanticModel, CancellationToken cancellationToken,
        out PrecedenceKind precedence, out bool clarifiesPrecedence, out bool innerExpressionHasPrimaryPrecedence)
    {
        return CanRemoveParenthesesHelper(
            parenthesizedExpression, semanticModel, cancellationToken,
            out precedence, out clarifiesPrecedence, out innerExpressionHasPrimaryPrecedence);
    }

    public static bool CanRemoveParenthesesHelper(
        ParenthesizedExpressionSyntax parenthesizedExpression, SemanticModel semanticModel, CancellationToken cancellationToken,
        out PrecedenceKind parentPrecedenceKind, out bool clarifiesPrecedence, out bool innerExpressionHasPrimaryPrecedence)
    {
        var result = parenthesizedExpression.CanRemoveParentheses(semanticModel, cancellationToken);
        if (!result)
        {
            parentPrecedenceKind = default;
            clarifiesPrecedence = false;
            innerExpressionHasPrimaryPrecedence = false;
            return false;
        }

        var inner = parenthesizedExpression.Expression;
        var innerPrecedence = inner.GetOperatorPrecedence();
        var innerIsSimple = innerPrecedence is OperatorPrecedence.Primary or
                            OperatorPrecedence.None;
        innerExpressionHasPrimaryPrecedence = innerIsSimple;

        ExpressionSyntax parentExpression;
        switch (parenthesizedExpression.Parent)
        {
            case ConditionalExpressionSyntax:
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

            case ConstantPatternSyntax { Parent: IsPatternExpressionSyntax isPatternExpression }:
                // on the right side of an 'x is const_pattern' expression
                parentExpression = isPatternExpression;
                break;

            default:
                parentPrecedenceKind = PrecedenceKind.Other;
                clarifiesPrecedence = false;
                return true;
        }

        // We're parented by something binary-like. 
        parentPrecedenceKind = CSharpExpressionPrecedenceService.Instance.GetPrecedenceKind(parentExpression);

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
